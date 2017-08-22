using System.IO;

using Dapper;
using StackExchange.Profiling;
using Xunit;

namespace Tests
{
    public class ProtobufSerializationTest : BaseTest
    {
        [Fact]
        public void Simple()
        {
            using (GetRequest("http://localhost/Test.aspx", startAndStopProfiler: false))
            {
                var mp = Options.StartProfiler();
                mp.Increment(); // 1 ms
                mp.Stop();

                var mp1 = MiniProfiler.Current;
                var ms = new MemoryStream();
                ProtoBuf.Serializer.Serialize(ms, mp1);

                ms.Position = 0;
                var mp2 = ProtoBuf.Serializer.Deserialize<MiniProfiler>(ms);
                AssertProfilersAreEqual(mp1, mp2);
            }
        }

        [Fact]
        public void CustomTimings()
        {
            using (GetRequest("http://localhost/Test.aspx", startAndStopProfiler: false))
            {
                var mp = Options.StartProfiler();

                mp.Increment(); // 1 ms

                using (mp.Step("Child one"))
                {
                    mp.Increment();

                    using (mp.CustomTiming("http", "GET http://google.com"))
                    {
                        mp.Increment();
                    }

                    using (var conn = Utils.GetSqliteConnection())
                    {
                        conn.Query<long>("select 1");
                        conn.Query<long>("select @one", new { one = (byte)1 });
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
}
