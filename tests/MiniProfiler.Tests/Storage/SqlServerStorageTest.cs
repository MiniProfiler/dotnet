using System.Data.SqlServerCe;
using System.Linq;

using Dapper;
using StackExchange.Profiling.Data;
using StackExchange.Profiling.Storage;
using Xunit;

namespace StackExchange.Profiling.Tests.Storage
{
    /// <summary>
    /// The SQL server storage test.
    /// </summary>
    public class SqlServerStorageTest : BaseTest
    {
        private SqlCeConnection _conn;
        
        public SqlServerStorageTest()
        {
            var sqlToExecute = SqlServerStorage.TableCreationScript.Replace("nvarchar(max)", "ntext").Split(';').Where(s => !string.IsNullOrWhiteSpace(s));
            var connStr = CreateSqlCeDatabase<SqlServerStorageTest>(sqlToExecute: sqlToExecute);

            MiniProfiler.Settings.Storage = new SqlCeStorage(connStr);
            _conn = GetOpenSqlCeConnection<SqlServerStorageTest>();
        }
        
        //[TestFixtureTearDown]
        //public void TestFixtureTearDown()
        //{
        //    MiniProfiler.Settings.Storage = null;
        //}
        
        [Fact]
        public void NoChildTimings()
        {
            var mp = GetProfiler();
            AssertMiniProfilerExists(mp);
            AssertTimingsExist(mp, 1);

            var mp2 = MiniProfiler.Settings.Storage.Load(mp.Id);
            AssertProfilersAreEqual(mp, mp2);
        }
        
        [Fact]
        public void WithChildTimings()
        {
            var mp = GetProfiler(childDepth: 5);
            AssertMiniProfilerExists(mp);
            AssertTimingsExist(mp, 6);

            var mp2 = MiniProfiler.Settings.Storage.Load(mp.Id);
            AssertProfilersAreEqual(mp, mp2);
        }
        
        private ProfiledDbConnection GetProfiledConnection()
        {
            return new ProfiledDbConnection(GetOpenSqlCeConnection<SqlServerStorageTest>(), MiniProfiler.Current);
        }
        
        private void AssertMiniProfilerExists(MiniProfiler miniProfiler)
        {
            var count = _conn.Query<int>("select count(*) from MiniProfilers where Id = @Id", new { miniProfiler.Id }).Single();
            Assert.Equal(count, 1);
        }
        
        private void AssertTimingsExist(MiniProfiler profiler, int expected)
        {
            var count = _conn.Query<int>("select count(*) from MiniProfilerTimings where MiniProfilerId = @Id", new { profiler.Id }).Single();
            Assert.Equal(count, expected);
        }
    }
}
