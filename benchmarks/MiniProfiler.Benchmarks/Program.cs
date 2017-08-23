using BenchmarkDotNet.Running;
using System.Reflection;

namespace Benchmarks
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            //var creation = new Benchmarks.CreationBenchmarks();
            //while (true)
            //{
            //    creation.StartStopProfiler();
            //}
            BenchmarkSwitcher.FromAssembly(typeof(Program).GetTypeInfo().Assembly).Run(args);
        }
    }
}
