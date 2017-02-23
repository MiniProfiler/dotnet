using System.IO;

using Dapper;
using StackExchange.Profiling;
using StackExchange.Profiling.SqlFormatters;
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
                MiniProfiler.Start();
                Increment(); // 1 ms
                MiniProfiler.Stop();

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
                MiniProfiler.Settings.SqlFormatter = new SqlServerFormatter();
                MiniProfiler.Start();
                var mp1 = MiniProfiler.Current;

                Increment(); // 1 ms

                using (mp1.Step("Child one"))
                {
                    Increment();

                    using (mp1.CustomTiming("http", "GET http://google.com"))
                    {
                        Increment();
                    }

                    using (var conn = Utils.GetSqliteConnection())
                    {
                        conn.Query<long>("select 1");
                        conn.Query<long>("select @one", new { one = (byte)1 });
                    }
                }

                MiniProfiler.Stop();

                var ms = new MemoryStream();
                ProtoBuf.Serializer.Serialize(ms, mp1);

                ms.Position = 0;
                var mp2 = ProtoBuf.Serializer.Deserialize<MiniProfiler>(ms);
                AssertProfilersAreEqual(mp1, mp2);
            }
        }
    }
}
