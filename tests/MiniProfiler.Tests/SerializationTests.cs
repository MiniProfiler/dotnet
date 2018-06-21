using StackExchange.Profiling.Internal;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Profiling.Tests
{
    public class SerializationTests : BaseTest
    {
        public SerializationTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void ParentMapping()
        {
            var mp = new MiniProfiler("Test", Options);
            using (mp.Step("Main"))
            {
                using (mp.Step("Sub Step 1"))
                {
                    using (mp.CustomTiming("cat", "Command 1")) {}
                    using (mp.CustomTiming("cat", "Command 2")) {}
                    using (mp.CustomTiming("cat", "Command 3")) {}
                }
                using (mp.Step("Sub Step 2"))
                {
                    using (mp.CustomTiming("cat", "Command 4")) {}
                    using (mp.CustomTiming("cat", "Command 5")) {}
                    using (mp.CustomTiming("cat", "Command 6")) {}
                }
            }
            mp.Stop();
            var json = mp.ToJson();

            var deserialized = MiniProfiler.FromJson(json);
            var root = deserialized.Root;
            foreach (var t in root.Children)
            {
                Assert.Equal(root, t.ParentTiming);
                Assert.True(root == t.ParentTiming);

                foreach (var tc in t.Children)
                {
                    Assert.Equal(t, tc.ParentTiming);
                    Assert.True(t == tc.ParentTiming);
                }
            }
        }
    }
}
