using System.Data.SqlServerCe;
using System.Linq;

using Dapper;
using NUnit.Framework;
using StackExchange.Profiling.Data;
using StackExchange.Profiling.Storage;

namespace StackExchange.Profiling.Tests.Storage
{
    /// <summary>
    /// The SQL server storage test.
    /// </summary>
    [TestFixture]
    public class SqlServerStorageTest : BaseTest
    {
        private SqlCeConnection _conn;
        
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            var sqlToExecute = SqlServerStorage.TableCreationScript.Replace("nvarchar(max)", "ntext").Split(';').Where(s => !string.IsNullOrWhiteSpace(s));
            var connStr = CreateSqlCeDatabase<SqlServerStorageTest>(sqlToExecute: sqlToExecute);

            MiniProfiler.Settings.Storage = new SqlCeStorage(connStr);
            _conn = BaseTest.GetOpenSqlCeConnection<SqlServerStorageTest>();
        }
        
        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            MiniProfiler.Settings.Storage = null;
        }
        
        [Test]
        public void NoChildTimings()
        {
            var mp = GetProfiler();
            AssertMiniProfilerExists(mp);
            AssertTimingsExist(mp, 1);

            var mp2 = MiniProfiler.Settings.Storage.Load(mp.Id);
            AssertProfilersAreEqual(mp, mp2);
        }
        
        [Test]
        public void WithChildTimings()
        {
            var mp = GetProfiler(childDepth: 5);
            AssertMiniProfilerExists(mp);
            AssertTimingsExist(mp, 6);

            var mp2 = MiniProfiler.Settings.Storage.Load(mp.Id);
            AssertProfilersAreEqual(mp, mp2);
        }
        
        // TODO: Revisit
        //[Test]
        //public void WithSqlTimings()
        //{
        //    MiniProfiler mp;

        //    using (BaseTest.GetRequest())
        //    using (var conn = GetProfiledConnection())
        //    {
        //        mp = MiniProfiler.Current;

        //        // one sql in the root timing
        //        conn.Query("select 1");

        //        using (mp.Step("Child step"))
        //        {
        //            conn.Query("select 2 where 1 = @one", new { one = 1 });
        //        }
        //    }

        //    Assert.IsFalse(mp.HasDuplicateSqlTimings);

        //    AssertSqlTimingsExist(mp.Root, 1);
        //    var t = mp.Root.Children.Single();
        //    AssertSqlTimingsExist(t, 1);
        //    AssertSqlParametersExist(t.SqlTimings.Single(), 1);

        //    var mp2 = MiniProfiler.Settings.Storage.Load(mp.Id);
        //    AssertProfilersAreEqual(mp, mp2);
        //}

            
        // TODO: Revisit
        //[Test]
        //public void WithDuplicateSqlTimings()
        //{
        //    MiniProfiler mp;

        //    using (BaseTest.GetRequest())
        //    using (var conn = GetProfiledConnection())
        //    {
        //        mp = MiniProfiler.Current;

        //        // one sql in the root timing
        //        conn.Query("select 1");

        //        using (mp.Step("Child step"))
        //        {
        //            conn.Query("select 1");
        //        }
        //    }

        //    Assert.IsTrue(mp.HasDuplicateSqlTimings);

        //    AssertSqlTimingsExist(mp.Root, 1);
        //    AssertSqlTimingsExist(mp.Root.Children.Single(), 1);

        //    var mp2 = MiniProfiler.Settings.Storage.Load(mp.Id);
        //    AssertProfilersAreEqual(mp, mp2);
        //}
        
        private ProfiledDbConnection GetProfiledConnection()
        {
            return new ProfiledDbConnection(GetOpenSqlCeConnection<SqlServerStorageTest>(), MiniProfiler.Current);
        }
        
        private void AssertMiniProfilerExists(MiniProfiler miniProfiler)
        {
            Assert.That(_conn.Query<int>("select count(*) from MiniProfilers where Id = @Id", new { miniProfiler.Id }).Single() == 1);
        }
        
        private void AssertTimingsExist(MiniProfiler profiler, int count)
        {
            Assert.That(_conn.Query<int>("select count(*) from MiniProfilerTimings where MiniProfilerId = @Id", new { profiler.Id }).Single() == count);
        }
        
        private void AssertSqlTimingsExist(Timing timing, int count)
        {
            Assert.That(_conn.Query<int>("select count(*) from MiniProfilerSqlTimings where ParentTimingId = @Id ", new { timing.Id }).Single() == count);
        }

        // TODO: Revisit
        //private void AssertSqlParametersExist(SqlTiming sqlTiming, int count)
        //{
        //    Assert.That(_conn.Query<int>("select count(*) from MiniProfilerSqlTimingParameters where ParentSqlTimingId = @Id ", new { sqlTiming.Id }).Single() == count);
        //}
    }
}
