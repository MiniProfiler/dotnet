using BenchmarkDotNet.Running;
using System.Linq;
using System.Reflection;

namespace Benchmarks
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var benchmarkTypes = Assembly.GetEntryAssembly()
                .DefinedTypes
                .Where(t => t.Name.EndsWith("Benchmarks"))
                .ToArray();

            var switcher = new BenchmarkSwitcher(benchmarkTypes);
            switcher.Run(args);
        }
    }
}
