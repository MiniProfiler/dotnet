using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;

namespace Benchmarks
{
    public static class Configs
    {
        public class Full : ManualConfig
        {
            public Full()
            {
                Add(new MemoryDiagnoser());
                //Add(new BenchmarkDotNet.Diagnostics.Windows.InliningDiagnoser());
            }
        }

        public class Memory : ManualConfig
        {
            public Memory() => Add(new MemoryDiagnoser());
        }

        public class MemoryFast : ManualConfig
        {
            public MemoryFast()
            {
                Add(new MemoryDiagnoser());
                Add(Job.Dry
                    .With(Platform.X64)
                    .With(Jit.RyuJit)
                    .With(Runtime.Core)
                    .WithTargetCount(5)
                    .WithInvocationCount(2048)
                    .WithIterationTime(TimeInterval.Second*5)
                    .WithId("Core"));
                Add(Job.Dry
                    .With(Platform.X64)
                    .With(Jit.RyuJit)
                    .With(Runtime.Clr)
                    .WithTargetCount(5)
                    .WithInvocationCount(2048)
                    .WithIterationTime(TimeInterval.Second*5)
                    .WithId("Clr"));
            }
        }
    }
}
