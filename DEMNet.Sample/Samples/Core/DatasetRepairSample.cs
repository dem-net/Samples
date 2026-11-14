//
// DatasetRepairSample.cs
//
// Author:
//       Xavier Fischer
//
// Copyright (c) 2019
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using BitMiracle.LibTiff.Classic;
using DEM.Net.Core;
using DEM.Net.Core.Datasets;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SampleApp
{
    /// <summary>
    /// Puts local datasets back in a consistent state after a metadata format or registration change.
    ///
    /// For each dataset found in the local data directory:
    ///  1. every raster file is parsed again and its json manifest rewritten (current FileMetadata version and registration),
    ///  2. raster files that cannot be parsed (truncated downloads, corrupted files...) are moved to a "_quarantine" folder
    ///     and downloaded again from the dataset source when it is a remote one,
    ///  3. manifests whose raster file no longer exists are deleted,
    ///  4. the protobuf index (globalmanifest.bin) is rebuilt and loaded, as the Web API does at startup.
    ///
    /// Stop the Web API (or its app pool) before running it, and start it again afterwards: the API keeps the catalog in memory.
    /// Use dryRun first to see what would be done: files are only parsed, nothing is written, moved or downloaded.
    /// </summary>
    public class DatasetRepairSample
    {
        private const string QUARANTINE_DIR = "_quarantine";
        private const string MANIFEST_DIR = "manifest";
        private readonly ILogger<DatasetRepairSample> _logger;
        private readonly RasterService _rasterService;

        public DatasetRepairSample(ILogger<DatasetRepairSample> logger, RasterService rasterService)
        {
            _logger = logger;
            _rasterService = rasterService;
        }

        /// <summary>
        /// Command line entry point: repair [dataset1,dataset2|all] [--data-dir path] [--dry-run] [--no-download] [--parallel n]
        /// </summary>
        public int RunFromCommandLine(string[] args)
        {
            var datasetArg = args.Skip(1).FirstOrDefault(a => !a.StartsWith("--")) ?? "all";
            string dataDir = GetOption(args, "--data-dir");
            bool dryRun = args.Contains("--dry-run");
            bool download = !args.Contains("--no-download");
            int parallel = int.TryParse(GetOption(args, "--parallel"), out int p) ? p : Math.Max(1, Environment.ProcessorCount / 2);

            if (!string.IsNullOrWhiteSpace(dataDir))
            {
                // SetLocalDirectory appends "DEM.Net": accept both the DEM.Net folder and its parent
                dataDir = Path.GetFullPath(dataDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (string.Equals(Path.GetFileName(dataDir), RasterService.APP_NAME, StringComparison.OrdinalIgnoreCase))
                    dataDir = Path.GetDirectoryName(dataDir);
                _rasterService.SetLocalDirectory(dataDir);
            }

            List<DEMDataSet> datasets;
            if (datasetArg.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                datasets = DEMDataSet.RegisteredDatasets.ToList();
            }
            else
            {
                var names = datasetArg.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var unknown = names.Where(n => !DEMDataSet.RegisteredDatasets.Any(d => d.Name == n)).ToList();
                if (unknown.Any())
                {
                    _logger.LogError($"Unknown dataset(s): {string.Join(", ", unknown)}. Registered datasets: {string.Join(", ", DEMDataSet.RegisteredDatasets.Select(d => d.Name))}");
                    return 1;
                }
                datasets = DEMDataSet.RegisteredDatasets.Where(d => names.Contains(d.Name)).ToList();
            }

            bool allOk = Run(datasets, dryRun, download, parallel);
            return allOk ? 0 : 2;
        }

        /// <summary>
        /// Repairs the given datasets. Returns true when every dataset is consistent at the end.
        /// </summary>
        public bool Run(IEnumerable<DEMDataSet> datasets, bool dryRun = false, bool redownloadBadFiles = true, int maxDegreeOfParallelism = 2)
        {
            _logger.LogInformation($"Local data directory: {_rasterService.LocalDirectory}{(dryRun ? " (dry run: nothing will be written)" : "")}");
            var summary = new List<string>();
            bool allOk = true;

            foreach (var dataset in datasets)
            {
                string localPath = _rasterService.GetLocalDEMPath(dataset);
                if (!Directory.Exists(localPath))
                {
                    _logger.LogInformation($"{dataset.Name}: no local directory, skipped.");
                    continue;
                }

                try
                {
                    var result = RepairDataset(dataset, localPath, dryRun, redownloadBadFiles, maxDegreeOfParallelism);
                    summary.Add(result.ToString());
                    allOk &= result.IsOk;
                }
                catch (Exception ex)
                {
                    allOk = false;
                    _logger.LogError(ex, $"{dataset.Name}: repair failed: {ex.Message}");
                    summary.Add($"{dataset.Name}: FAILED ({ex.Message})");
                }
            }

            _logger.LogInformation("Summary:" + Environment.NewLine + string.Join(Environment.NewLine, summary.Select(s => "  " + s)));
            return allOk;
        }

        private RepairResult RepairDataset(DEMDataSet dataset, string localPath, bool dryRun, bool redownloadBadFiles, int maxDegreeOfParallelism)
        {
            var sw = Stopwatch.StartNew();
            var result = new RepairResult(dataset.Name, dryRun);
            string quarantinePath = Path.Combine(localPath, QUARANTINE_DIR);

            var rasterFiles = Directory.EnumerateFiles(localPath, "*" + dataset.FileFormat.FileExtension, SearchOption.AllDirectories)
                .Where(f => !IsInDirectory(f, quarantinePath) && !IsInManifestDirectory(f))
                .ToList();
            result.Files = rasterFiles.Count;
            _logger.LogInformation($"{dataset.Name}: {rasterFiles.Count} raster files to check ({dataset.FileFormat.Registration} registration).");

            // 1. Parse every raster again and rewrite its manifest
            var badFiles = new ConcurrentBag<(string File, string Error)>();
            int done = 0;
            Parallel.ForEach(rasterFiles, new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism }, file =>
            {
                try
                {
                    CheckRasterData(file, dataset.FileFormat.Type);
                    if (dryRun)
                        _rasterService.ParseMetadata(file, dataset.FileFormat);
                    else
                        _rasterService.GenerateFileMetadata(file, dataset.FileFormat, force: true);
                }
                catch (Exception ex)
                {
                    badFiles.Add((file, $"{ex.GetType().Name}: {ex.Message}"));
                    _logger.LogWarning($"{dataset.Name}: cannot parse {file}: {ex.GetType().Name}: {ex.Message}");
                }

                int count = Interlocked.Increment(ref done);
                if (count % 500 == 0)
                    _logger.LogInformation($"{dataset.Name}: {count}/{rasterFiles.Count} files checked ({count * 1d / rasterFiles.Count:P0})");
            });
            result.BadFiles = badFiles.Select(b => b.File).OrderBy(f => f).ToList();

            // 2. Quarantine bad files (with their manifest), download them again when the source is remote
            if (result.BadFiles.Any() && !dryRun)
            {
                Dictionary<string, DemFileReport> sources = null;
                if (redownloadBadFiles && dataset.DataSource.DataSourceType != DEMDataSourceType.LocalFileSystem)
                {
                    sources = _rasterService.GenerateReport(dataset)
                        .GroupBy(r => Path.GetFullPath(r.LocalName), StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
                }

                foreach (var file in result.BadFiles)
                {
                    string target = Path.Combine(quarantinePath, Path.GetRelativePath(localPath, file));
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    if (File.Exists(target)) File.Delete(target);
                    File.Move(file, target);
                    DeleteIfExists(ManifestFileName(file));
                    _logger.LogInformation($"{dataset.Name}: moved {file} to {target}");

                    if (sources != null && sources.TryGetValue(Path.GetFullPath(file), out DemFileReport report))
                    {
                        try
                        {
                            _logger.LogInformation($"{dataset.Name}: downloading {report.URL} again...");
                            _rasterService.DownloadRasterFile(report, dataset); // also generates the manifest
                            result.Redownloaded.Add(file);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"{dataset.Name}: download of {report.URL} failed again ({ex.Message}). File stays in quarantine.");
                            DeleteIfExists(file);
                            DeleteIfExists(ManifestFileName(file));
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"{dataset.Name}: no download source for {file}, it stays in quarantine (tile is now missing).");
                    }
                }
            }

            // 3. Orphan manifests (raster deleted or moved), they would break the catalog
            // (dry run: bad files are still in place, their manifest is handled with them, not as an orphan)
            var kept = dryRun ? rasterFiles : rasterFiles.Except(result.BadFiles).Concat(result.Redownloaded);
            var rasterSet = new HashSet<string>(kept.Select(Path.GetFullPath), StringComparer.OrdinalIgnoreCase);
            foreach (var manifestDir in Directory.EnumerateDirectories(localPath, MANIFEST_DIR, SearchOption.AllDirectories).Where(d => !IsInDirectory(d, quarantinePath)))
            {
                foreach (var json in Directory.EnumerateFiles(manifestDir, "*.json"))
                {
                    string raster = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(manifestDir), Path.GetFileNameWithoutExtension(json) + dataset.FileFormat.FileExtension));
                    if (!rasterSet.Contains(raster))
                    {
                        result.OrphanManifests++;
                        _logger.LogInformation($"{dataset.Name}: {(dryRun ? "would delete" : "deleting")} orphan manifest {json}");
                        if (!dryRun) File.Delete(json);
                    }
                }
            }

            // 4. Rebuild the protobuf index and load the catalog, like the API does
            if (!dryRun)
            {
                var catalog = _rasterService.LoadManifestMetadata(dataset, force: true, logTimeSpent: true);
                result.CatalogEntries = catalog.Count;
                result.InvalidCatalogEntries = catalog.Count(m => m == null || m.BoundingBox == null
                    || m.Version != FileMetadata.FILEMETADATA_VERSION
                    || m.FileFormat?.Registration != dataset.FileFormat.Registration);
            }

            result.Elapsed = sw.Elapsed;
            _logger.LogInformation(result.ToString());
            return result;
        }

        /// <summary>
        /// Metadata parsing only reads the file header: a truncated download (COG headers are at the start of the file)
        /// still parses fine and fails later when pixels are read. Decode the last tile / strip to make sure data is complete.
        /// </summary>
        private static void CheckRasterData(string file, DEMFileType fileType)
        {
            if (fileType != DEMFileType.GEOTIFF) return;

            Tiff.SetErrorHandler(new SilentTiffErrorHandler());
            using (Tiff tiff = Tiff.Open(file, "r"))
            {
                if (tiff == null) throw new InvalidDataException("not a readable TIFF file");

                int read;
                if (tiff.IsTiled())
                {
                    int last = tiff.NumberOfTiles() - 1;
                    read = tiff.ReadEncodedTile(last, new byte[tiff.TileSize()], 0, -1);
                }
                else
                {
                    int last = tiff.NumberOfStrips() - 1;
                    read = tiff.ReadEncodedStrip(last, new byte[tiff.StripSize()], 0, -1);
                }
                if (read <= 0) throw new InvalidDataException("raster data is truncated or corrupted (last tile/strip cannot be decoded)");
            }
        }

        private class SilentTiffErrorHandler : TiffErrorHandler
        {
            public override void ErrorHandler(Tiff tif, string method, string format, params object[] args) { }
            public override void ErrorHandlerExt(Tiff tif, object clientData, string method, string format, params object[] args) { }
            public override void WarningHandler(Tiff tif, string method, string format, params object[] args) { }
            public override void WarningHandlerExt(Tiff tif, object clientData, string method, string format, params object[] args) { }
        }

        private static string ManifestFileName(string rasterFile)
            => Path.Combine(Path.GetDirectoryName(rasterFile), MANIFEST_DIR, Path.GetFileNameWithoutExtension(rasterFile) + ".json");

        private static bool IsInDirectory(string path, string directory)
            => Path.GetFullPath(path).StartsWith(Path.GetFullPath(directory) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

        private static bool IsInManifestDirectory(string path)
            => string.Equals(Path.GetFileName(Path.GetDirectoryName(path)), MANIFEST_DIR, StringComparison.OrdinalIgnoreCase);

        private static void DeleteIfExists(string file)
        {
            if (File.Exists(file)) File.Delete(file);
        }

        private static string GetOption(string[] args, string name)
        {
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        private class RepairResult
        {
            public RepairResult(string dataset, bool dryRun) { Dataset = dataset; DryRun = dryRun; }
            public string Dataset { get; }
            public bool DryRun { get; }
            public int Files { get; set; }
            public List<string> BadFiles { get; set; } = new List<string>();
            public List<string> Redownloaded { get; } = new List<string>();
            public int OrphanManifests { get; set; }
            public int? CatalogEntries { get; set; }
            public int InvalidCatalogEntries { get; set; }
            public TimeSpan Elapsed { get; set; }

            public bool IsOk => DryRun
                ? BadFiles.Count == 0 && OrphanManifests == 0
                : InvalidCatalogEntries == 0 && BadFiles.Count == Redownloaded.Count;

            public override string ToString()
            {
                var s = $"{Dataset}: {Files} files, {BadFiles.Count} unreadable";
                if (!DryRun) s += $" ({Redownloaded.Count} downloaded again, {BadFiles.Count - Redownloaded.Count} left in {QUARANTINE_DIR})";
                s += $", {OrphanManifests} orphan manifests {(DryRun ? "to delete" : "deleted")}";
                if (CatalogEntries.HasValue) s += $", catalog {CatalogEntries} entries ({InvalidCatalogEntries} invalid)";
                s += $" in {Elapsed:hh\\:mm\\:ss} => {(IsOk ? "OK" : "NEEDS ATTENTION")}";
                if (BadFiles.Any()) s += Environment.NewLine + string.Join(Environment.NewLine, BadFiles.Select(f => "      unreadable: " + f));
                return s;
            }
        }
    }
}
