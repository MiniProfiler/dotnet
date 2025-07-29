using System.IO;
using Xunit;

namespace StackExchange.Profiling.Tests
{
    public class ProtobufSerializationTests(ITestOutputHelper output) : BaseTest(output)
    {
        [Fact]
        public void Simple()
        {
            var mp = GetBasicProfiler();
            mp.Increment(); // 1 ms
            mp.Stop();

            var mp1 = MiniProfiler.Current;
            Assert.NotNull(mp1);
            var ms = new MemoryStream();
            ProtoBuf.Serializer.Serialize(ms, mp1);

            ms.Position = 0;
            var mp2 = ProtoBuf.Serializer.Deserialize<MiniProfiler>(ms);
            AssertProfilersAreEqual(mp1, mp2);
        }

        [Fact]
        public void CustomTimings()
        {
            var mp = GetBasicProfiler();

            mp.Increment(); // 1 ms

            using (mp.Step("Child one"))
            {
                mp.Increment();

                using (mp.CustomTiming("http", "GET http://google.com"))
                {
                    mp.Increment();
                }
            }

            mp.Stop();

            var ms = new MemoryStream();
            ProtoBuf.Serializer.Serialize(ms, mp);

            ms.Position = 0;
            var mp2 = ProtoBuf.Serializer.Deserialize<MiniProfiler>(ms);
            AssertProfilersAreEqual(mp, mp2);
        }
    }
}
