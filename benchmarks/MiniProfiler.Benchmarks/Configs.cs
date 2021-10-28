using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace Benchmarks
{
    public static class Configs
    {
        public class Full : ManualConfig
        {
            public Full() => AddDiagnoser(MemoryDiagnoser.Default);
        }

        public class Memory : ManualConfig
        {
            public Memory() => AddDiagnoser(MemoryDiagnoser.Default);
        }
    }
}
