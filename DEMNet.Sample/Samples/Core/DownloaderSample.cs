//
// DownloaderSample.cs
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

using DEM.Net.Core;
using DEM.Net.Core.Datasets;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SampleApp
{
    /// <summary>
    /// DownloaderSample: Show how to download all data into specified directory 
    /// </summary>
    public class DownloaderSample
    {
        private readonly ILogger<DownloaderSample> _logger;
        private readonly RasterService _rasterService;
        private readonly ElevationService _elevationService;


        public DownloaderSample(ILogger<DownloaderSample> logger
                , RasterService rasterService
                , ElevationService elevationService)
        {
            _logger = logger;
            _rasterService = rasterService;
            _elevationService = elevationService;
        }
        public void Run(DEMDataSet specificDataset = null)
        {
            try
            {
                //this.PrepareIgn1_3_Deduplicate(@"C:\Users\admin\Downloads\IGN1\France\Done");
                _logger.LogInformation($"Downloading all files to {_rasterService.LocalDirectory}");
                Stopwatch sw = new Stopwatch();

                var datasetsQuery = DEMDataSet.RegisteredNonLocalDatasets;
                if (specificDataset != null)
                    datasetsQuery = datasetsQuery.Where(d => d.Name == specificDataset.Name);

                foreach (var dataset in datasetsQuery)
                //Parallel.ForEach(datasetsQuery, dataset =>
                {
                    _logger.LogInformation($"{dataset.Name}:");

                    _rasterService.GenerateDirectoryMetadata(dataset, false);
                    _elevationService.DownloadMissingFiles(dataset);

                }
                //);


            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        public void DeduplicateSwissTopoFiles(string path)
        {
            DeduplicateFiles(path,
                fileKey: file => // common part in all duplicate files, unique to them
                {
                    if (!file.Contains("_"))
                        return null;

                    var right = file.Split('_', 3)[2];
                    return right;
                },
                orderFunc: f => // way of sorting files from names
                {
                    var year = f.Split('_', 3)[1];
                    return int.Parse(year);
                }
                );
        }

        public void DeduplicateFiles(string directory, Func<string, string> fileKey, Func<string, int> orderFunc)
        {
            try
            {
                _logger.LogInformation($"Loading all files in {directory}");
                Stopwatch sw = new Stopwatch();
                // Delete duplicate files (keep newest)

                List<(string fileName, string key)> files = new List<(string fileName, string key)>();
                foreach (var file in Directory.GetFiles(directory,"*.*", SearchOption.AllDirectories))
                {
                    var key = fileKey(file);
                    if (key is not null)
                        files.Add((file, key));
                }

                var results = files.GroupBy(f => f.key).Select(g => new { dep = g.Key, files = g.OrderByDescending(_ => orderFunc(_.fileName)).ToList() }).ToList();

                foreach (var result in results)
                {
                    if (result.files.Count > 1)
                    {
                        foreach (var file in result.files.Skip(1))
                        {
                            //File.Move(file.fileName, Path.Combine(archiveDir, Path.GetFileName(file.fileName)));
                            //_logger.LogInformation($"Archiving file {file.fileName}");
                            File.Delete(file.fileName);
                            _logger.LogInformation($"Deleting file {file.fileName}");

                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        public void PrepareIgn5_1_Deduplicate_DezipManualAfter(string localDir = @"C:\Users\admin\Downloads\IGN5\France")
        {
            try
            {
                _logger.LogInformation($"Loading all files in {localDir}");
                Stopwatch sw = new Stopwatch();
                // Delete duplicate files (keep newest)

                List<(string fileName, string key, DateTime date)> files = new List<(string fileName, string key, DateTime date)>();
                foreach (var file in Directory.GetFiles(localDir, "*.7z"))
                {
                    // 0       1   2   3  4            5    6  
                    // RGEALTI_2-0_5M_ASC_LAMB93-IGN69_D022_2021-03-15
                    var fileParts = file.Split(".")[0].Split("_");
                    var dep = fileParts[5];
                    var dateParts = fileParts[6].Split("-").Select(p => int.Parse(p)).ToArray();
                    var date = new DateTime(dateParts[0], dateParts[1], dateParts[2]);

                    files.Add((file, dep, date));
                }

                var results = files.GroupBy(f => f.key).Select(g => new { dep = g.Key, files = g.OrderByDescending(_ => _.date).ToList() }).ToList();

                string archiveDir = Path.Combine(localDir, "Archive");
                Directory.CreateDirectory(archiveDir);

                foreach (var result in results)
                {
                    if (result.files.Count > 1)
                    {
                        foreach (var file in result.files.Skip(1))
                        {
                            //File.Move(file.fileName, Path.Combine(archiveDir, Path.GetFileName(file.fileName)));
                            //_logger.LogInformation($"Archiving file {file.fileName}");
                            File.Delete(file.fileName);
                            _logger.LogInformation($"Deleting file {file.fileName}");

                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        public void PrepareIgn5_2_AfterDezip_MoveAndCompressAsc(string localDir = @"C:\Users\admin\Downloads\IGN5\France")
        {
            try
            {
                _logger.LogInformation($"Loading all files in {localDir}");
                Stopwatch sw = new Stopwatch();
                // Delete duplicate files (keep newest)

                List<(string fileName, string key, DateTime date)> files = new List<(string fileName, string key, DateTime date)>();
                // Parallel.ForEach(Directory.EnumerateFiles(localDir, "*.asc", SearchOption.AllDirectories), file =>
                foreach (var file in Directory.GetFiles(localDir, "*.asc", SearchOption.AllDirectories))
                {
                    var dep = Path.GetDirectoryName(file).Split("_").Last();
                    var outDepDir = Path.Combine(localDir, dep);
                    var outFileName = Path.Combine(localDir, dep, Path.GetFileName(file) + ".gz");
                    Directory.CreateDirectory(outDepDir);

                    _logger.LogInformation($"Compressing file {file}");

                    using (FileStream originalFileStream = File.OpenRead(file))
                    using (FileStream compressedFileStream = File.Create(outFileName))
                    {
                        using (GZipStream compressionStream = new GZipStream(compressedFileStream,
                           CompressionMode.Compress))
                        {
                            originalFileStream.CopyTo(compressionStream);
                        }
                    }
                    File.Delete(file);
                }
                // );


                // DELETE DIRS
                foreach (var directory in Directory.EnumerateDirectories(localDir, "RGEALTI_2-0_5M_ASC_*"))
                {

                    _logger.LogInformation($"Deleting directory {directory}");
                    Directory.Delete(directory, true);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        // We should keep the newest one (keep track of where it comes from in step 1)
        public void PrepareIgn5_3_Deduplicate(string localDir = @"C:\Users\admin\Downloads\IGN5\France")
        {
            try
            {
                _logger.LogInformation($"Loading all files in {localDir}");
                Stopwatch sw = new Stopwatch();
                // Delete duplicate files (keep newest)

                List<(string fileName, string dep, string key, float sizeKb)> files = new List<(string fileName, string dep, string key, float sizeKb)>();
                foreach (var file in Directory.GetFiles(localDir, "*.asc.gz", SearchOption.AllDirectories))
                {
                    var fileInfo = new FileInfo(file);
                    // 0       1   2    3    4   
                    // RGEALTI_FXX_0705_6940_MNT_LAMB93_IGN69.asc
                    var fileParts = file.Split(".")[0].Split("_");
                    var dep = fileInfo.Directory.Name;
                    var fileKey = fileParts[2] + "_" + fileParts[3];
                    var size = fileInfo.Length / 1024f;

                    files.Add((file, dep, fileKey, size));
                }

                var results = files.GroupBy(f => f.key).Select(g => new { dep = g.Key, files = g.ToList() }).ToList();

                foreach (var result in results)
                {
                    if (result.files.Count > 1)
                    {
                        var firstSize = result.files.First().sizeKb;
                        if (!result.files.All(f => f.sizeKb == firstSize))
                        {
                            _logger.LogWarning("Different file sizes");
                            foreach (var file in result.files.OrderByDescending(f => f.sizeKb).Skip(1))
                            {
                                //File.Move(file.fileName, Path.Combine(archiveDir, Path.GetFileName(file.fileName)));
                                //_logger.LogInformation($"Archiving file {file.fileName}");
                                File.Delete(file.fileName);
                                _logger.LogInformation($"Deleting file {file.fileName}");

                            }
                        }
                        else
                        {
                            foreach (var file in result.files.Skip(1))
                            {
                                //File.Move(file.fileName, Path.Combine(archiveDir, Path.GetFileName(file.fileName)));
                                //_logger.LogInformation($"Archiving file {file.fileName}");
                                File.Delete(file.fileName);
                                _logger.LogInformation($"Deleting file {file.fileName}");

                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        public void PrepareIgn1_1_Deduplicate_DezipManualAfter(string localDir = @"C:\Users\admin\Downloads\IGN1\France")
        {
            try
            {
                _logger.LogInformation($"Loading all files in {localDir}");
                Stopwatch sw = new Stopwatch();
                // Delete duplicate files (keep newest)

                List<(string fileName, string key, DateTime date, bool isZipPart)> files = new List<(string fileName, string key, DateTime date, bool isZipPart)>();
                foreach (var file in Directory.GetFiles(localDir, "*.7z.*"))
                {
                    // 0       1   2   3  4            5    6  
                    // RGEALTI_2-0_5M_ASC_LAMB93-IGN69_D022_2021-03-15
                    var fileseg = file.Split(".");
                    var fileParts = fileseg[0].Split("_");
                    bool isZipPart = fileseg.Length == 3;
                    var dep = fileParts[5];
                    var dateParts = fileParts[6].Split("-").Select(p => int.Parse(p)).ToArray();
                    var date = new DateTime(dateParts[0], dateParts[1], dateParts[2]);

                    files.Add((file, dep, date, isZipPart));
                }

                var results = files.GroupBy(f => f.key)
                    .Select(g => new
                    {
                        dep = g.Key,
                        files = g.GroupBy(_ => Path.GetFileName(_.fileName).Split(".")[0])
                                .Select(g1 => new
                                {
                                    FileName = g1.Key,
                                    files = g1.ToList()
                                }).ToList()
                    }).ToList();

                string archiveDir = Path.Combine(localDir, "Archive");
                Directory.CreateDirectory(archiveDir);

                foreach (var result in results)
                {
                    if (result.files.Count > 1)
                    {
                        foreach (var file in result.files.Skip(1))
                        {
                            //File.Move(file.fileName, Path.Combine(archiveDir, Path.GetFileName(file.fileName)));
                            //_logger.LogInformation($"Archiving file {file.fileName}");
                            //File.Delete(file.fileName);
                            //_logger.LogInformation($"Deleting file {file.fileName}");

                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        public void PrepareIgn1_2_AfterDezip_MoveAndCompressAsc(string localDir = @"C:\Users\admin\Downloads\IGN1\France\Process")
        {
            try
            {
                _logger.LogInformation($"Loading all files in {localDir}");
                Stopwatch sw = new Stopwatch();
                // Delete duplicate files (keep newest)

                List<(string fileName, string key, DateTime date)> files = new List<(string fileName, string key, DateTime date)>();
                var ascFiles = Directory.EnumerateFiles(localDir, "*.asc", SearchOption.AllDirectories).ToList();
                int count = 0;
                Parallel.ForEach(ascFiles, new ParallelOptions { MaxDegreeOfParallelism = 4 }, file =>
                //foreach (var file in ascFiles)
                {
                    var dep = Path.GetDirectoryName(file).Split("_").Reverse().Skip(1).First();
                    var outDepDir = Path.Combine(localDir, dep);
                    var outFileName = Path.Combine(localDir, dep, Path.GetFileName(file) + ".gz");
                    Directory.CreateDirectory(outDepDir);

                    using (FileStream originalFileStream = File.OpenRead(file))
                    using (FileStream compressedFileStream = File.Create(outFileName))
                    using (var compressionStream = new GZipStream(compressedFileStream, CompressionMode.Compress))
                    {
                        originalFileStream.CopyTo(compressionStream);
                    }

                    File.Delete(file);
                    Interlocked.Increment(ref count);

                    double progress = count * 1d / ascFiles.Count;

                    if (count % 10 == 0)
                        _logger.LogInformation($"Compressing files... {progress:P1}");
                }
                );


                // DELETE DIRS
                foreach (var directory in Directory.EnumerateDirectories(localDir, "RGEALTI_2-0_1M*"))
                {

                    _logger.LogInformation($"Deleting directory {directory}");
                    Directory.Delete(directory, true);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        // We should keep the newest one (keep track of where it comes from in step 1)
        public void PrepareIgn1_3_Deduplicate(string localDir = @"C:\Users\admin\Downloads\IGN1\France\Done")
        {
            try
            {
                _logger.LogInformation($"Loading all files in {localDir}");
                Stopwatch sw = new Stopwatch();
                // Delete duplicate files (keep newest)

                List<(string fileName, string dep, string key, float sizeKb)> files = new List<(string fileName, string dep, string key, float sizeKb)>();
                foreach (var file in Directory.GetFiles(localDir, "*.asc.gz", SearchOption.AllDirectories))
                {
                    var fileInfo = new FileInfo(file);
                    // 0       1   2    3    4   
                    // RGEALTI_FXX_0705_6940_MNT_LAMB93_IGN69.asc
                    var fileParts = fileInfo.Name.Split('.')[0].Split('_');
                    var dep = fileInfo.Directory.Name;
                    var fileKey = fileParts[2] + "_" + fileParts[3];
                    var size = fileInfo.Length / 1024f;

                    files.Add((file, dep, fileKey, size));
                }

                var results = files.GroupBy(f => f.key).Select(g => new { dep = g.Key, files = g.ToList() }).ToList();

                foreach (var result in results)
                {
                    if (result.files.Count > 1)
                    {
                        var firstSize = result.files.First().sizeKb;
                        if (!result.files.All(f => f.sizeKb == firstSize))
                        {
                            _logger.LogWarning("Different file sizes");
                            foreach (var file in result.files.OrderByDescending(f => f.sizeKb).Skip(1))
                            {
                                //File.Move(file.fileName, Path.Combine(archiveDir, Path.GetFileName(file.fileName)));
                                //_logger.LogInformation($"Archiving file {file.fileName}");
                                File.Delete(file.fileName);
                                _logger.LogInformation($"Deleting file {file.fileName}");

                            }
                        }
                        else
                        {
                            foreach (var file in result.files.Skip(1))
                            {
                                //File.Move(file.fileName, Path.Combine(archiveDir, Path.GetFileName(file.fileName)));
                                //_logger.LogInformation($"Archiving file {file.fileName}");
                                File.Delete(file.fileName);
                                _logger.LogInformation($"Deleting file {file.fileName}");

                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        public void Generate_Ign5_Metadata()
        {
            _rasterService.LoadManifestMetadata(DEMDataSet.RegisteredDatasets.First(d => d.Name == "IGN_5m"), false);
            _rasterService.LoadManifestMetadata(DEMDataSet.RegisteredDatasets.First(d => d.Name == "IGN_1m"), false);
            _rasterService.LoadManifestMetadata(DEMDataSet.RegisteredDatasets.First(d => d.Name == "NASADEM"), false);
            _rasterService.LoadManifestMetadata(DEMDataSet.RegisteredDatasets.First(d => d.Name == "SRTM_GL3"), false);
            //_rasterService.GenerateDirectoryMetadata(DEMDataSet.RegisteredDatasets.First(d => d.Name == "IGN_1m"), true, false);
        }

        public void ExtractCopernicEuDem(string localDir)
        {
            var tarFiles = Directory.GetFileSystemEntries(localDir, "*.tar", SearchOption.AllDirectories);
            int i = 0;
            Parallel.ForEach(tarFiles, new ParallelOptions { MaxDegreeOfParallelism=2 }, (tarFile) =>
            //foreach (var tarFile in tarFiles)
            {
                try
                {
                    Interlocked.Increment(ref i);
                    _logger.LogInformation($"Processing {tarFile} ({i:N0}/{tarFiles.Length:N0}, {i/(float)tarFiles.Length:P1})");
                    string dezipDir = Path.Combine(Path.GetDirectoryName(tarFile), Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(tarFile)));
                    TarArchive.ExtractTar(tarFile, dezipDir, "dem.tif");

                    foreach (var tifFile in Directory.GetFileSystemEntries(dezipDir, "*dem.tif", SearchOption.AllDirectories))
                    {
                        string destination = Path.Combine(Path.GetDirectoryName(tarFile), Path.GetFileName(tifFile));
                        if (!File.Exists(destination))
                            File.Move(tifFile, destination, false);
                    }
                    Directory.Delete(dezipDir, true);
                    File.Delete(tarFile);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
            }
            );
            var csvFiles = Directory.GetFileSystemEntries(localDir, "*.csv", SearchOption.AllDirectories);
            foreach (var csvFile in csvFiles) File.Delete(csvFile);
        }

        public void DeduplicateCopernicEuDem(string localDir)
        {
            var files = Directory.GetFileSystemEntries(localDir, "*dem.tif", SearchOption.AllDirectories)
                .Select(f => new { FileName = Path.GetFileName(f), FullPath = f })
                .GroupBy(f => f.FileName)
                .Select(g => new { FileName = g.Key, Files = g.ToList() })
                .Where(g => g.Files.Count >1)
                .ToList();

            foreach (var file in files)
            {
                foreach (var f in file.Files.Skip(1))
                {
                    File.Delete(f.FullPath);
                }
            }
        }

        public void CompressAscFiles(string localDir)
        {
            try
            {
                _logger.LogInformation($"Loading all files in {localDir}");
                Stopwatch sw = new Stopwatch();
                var ascFiles = Directory.EnumerateFiles(localDir, "*.asc", SearchOption.AllDirectories).ToList();
                int count = 0;
                Parallel.ForEach(ascFiles, new ParallelOptions { MaxDegreeOfParallelism = 4 }, file =>
                //foreach (var file in ascFiles)
                {
                    var outFileName = Path.ChangeExtension(file, ".asc.gz");
                    
                    using (FileStream originalFileStream = File.OpenRead(file))
                    using (FileStream compressedFileStream = File.Create(outFileName))
                    using (var compressionStream = new GZipStream(compressedFileStream, CompressionMode.Compress))
                    {
                        originalFileStream.CopyTo(compressionStream);
                    }

                    File.Delete(file);
                    Interlocked.Increment(ref count);

                    double progress = count * 1d / ascFiles.Count;

                    if (count % 10 == 0)
                        _logger.LogInformation($"Compressing files... {progress:P1}");
                }
                );
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

    }
}

