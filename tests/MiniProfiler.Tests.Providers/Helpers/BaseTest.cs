using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Profiling.Tests
{
    public abstract class BaseTest
    {
        public const string NonParallel = nameof(NonParallel);

        protected MiniProfilerBaseOptions Options = new MiniProfilerBaseOptions();

        protected ITestOutputHelper Output { get; }

        protected BaseTest(ITestOutputHelper output)
        {
            Output = output;
        }
    }

    [CollectionDefinition(BaseTest.NonParallel, DisableParallelization = true)]
    public class NonParallelDefinition
    {
    }
}
