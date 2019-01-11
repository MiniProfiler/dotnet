using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace Benchmarks
{
    public static class Configs
    {
        public class Full : ManualConfig
        {
            public Full()
            {
                Add(new MemoryDiagnoser());
            }
        }

        public class Memory : ManualConfig
        {
            public Memory() => Add(new MemoryDiagnoser());
        }
    }
}
