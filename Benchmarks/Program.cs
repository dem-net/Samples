using BenchmarkDotNet.Running;

namespace Benchmarks
{
    internal class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<VectorListSort>();
        }
    }
}
