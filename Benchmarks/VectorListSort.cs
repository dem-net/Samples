using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class VectorListSort
    {
        private const int N = 1000000;

        private static IEnumerable<Vector4> GetData()
        {
            for (int i = 0; i < N; i++)
            {
                yield return new Vector4(Random.Shared.NextSingle() * 100f - 50f,
                                    Random.Shared.NextSingle() * 100f - 50f,
                                    Random.Shared.NextSingle() * 100f - 50f,
                                    Random.Shared.NextSingle() * 100f - 50f);
            }
        }

        public VectorListSort()
        {
   
        }

        [Benchmark]
        public List<Vector4> InPlaceSort()
        {
            List<Vector4> result = new List<Vector4>(N);
            result.AddRange(GetData());
            result.Sort((u, v) => u.Z.CompareTo(v.Z));
            return result;
        }

        [Benchmark]
        public List<Vector4> LinqSort()
        {
            return GetData().OrderBy(v=>v.Z).ToList();
        }

        [Benchmark]
        public List<Vector4> LinqSortAllocateFirst()
        {
            List<Vector4> result = new List<Vector4>(N);
            result.AddRange(GetData().OrderBy(v => v.Z));
            return result;
        }
    }
}
