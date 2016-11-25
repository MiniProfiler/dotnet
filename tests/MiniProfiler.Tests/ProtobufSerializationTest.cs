using NUnit.Framework;
using System.IO;

using Dapper;
using StackExchange.Profiling.SqlFormatters;

namespace StackExchange.Profiling.Tests
{
    [TestFixture]
    public class ProtobufSerializationTest : BaseTest
    {
        [Test]
        public void Simple()
        {
            using (GetRequest("http://localhost/Test.aspx", startAndStopProfiler: false))
            {
                MiniProfiler.Start();
                IncrementStopwatch(); // 1 ms
                MiniProfiler.Stop();

                var mp1 = MiniProfiler.Current;
                var ms = new MemoryStream();
                ProtoBuf.Serializer.Serialize(ms, mp1);

                ms.Position = 0;
                var mp2 = ProtoBuf.Serializer.Deserialize<MiniProfiler>(ms);
                AssertProfilersAreEqual(mp1, mp2);
            }
        }


        [Test]
        public void CustomTimings()
        {
            using (GetRequest("http://localhost/Test.aspx", startAndStopProfiler: false))
            {
                MiniProfiler.Settings.SqlFormatter = new SqlServerFormatter();
                MiniProfiler.Start();
                var mp1 = MiniProfiler.Current;

                IncrementStopwatch(); // 1 ms

                using (mp1.Step("Child one"))
                {
                    IncrementStopwatch();

                    using (mp1.CustomTiming("http", "GET http://google.com"))
                    {
                        IncrementStopwatch();
                    }

                    using (var conn = GetSqliteConnection())
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
