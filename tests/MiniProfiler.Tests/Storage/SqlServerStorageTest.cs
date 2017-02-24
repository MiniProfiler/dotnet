using System.Data.SqlServerCe;
using System.Threading.Tasks;

using Dapper;
using StackExchange.Profiling;
using Xunit;

namespace Tests.Storage
{
    /// <summary>
    /// The SQL server storage test.
    /// </summary>
    [Collection("SQLServer")]
    public class SqlServerStorageTest : BaseTest, IClassFixture<SqlCeStorageFixture<SqlServerStorageTest>>
    {
        private readonly SqlCeConnection _conn;

        public SqlServerStorageTest(SqlCeStorageFixture<SqlServerStorageTest> fixture)
        {
            _conn = fixture.Conn;
            _testStorage = fixture.Storage;
        }

        [Fact]
        public void NoChildTimings()
        {
            var mp = GetProfiler();
            AssertMiniProfilerExists(mp);
            AssertTimingsExist(mp, 1);

            var mp2 = _testStorage.Load(mp.Id);
            AssertProfilersAreEqual(mp, mp2);
        }

        [Fact]
        public async Task NoChildTimingsAsync()
        {
            var mp = await GetProfilerAsync().ConfigureAwait(false);
            AssertMiniProfilerExists(mp);
            AssertTimingsExist(mp, 1);

            var mp2 = await _testStorage.LoadAsync(mp.Id).ConfigureAwait(false);
            AssertProfilersAreEqual(mp, mp2);
        }

        [Fact]
        public void WithChildTimings()
        {
            var mp = GetProfiler(childDepth: 5);
            AssertMiniProfilerExists(mp);
            AssertTimingsExist(mp, 6);

            var mp2 = _testStorage.Load(mp.Id);
            AssertProfilersAreEqual(mp, mp2);
        }

        [Fact]
        public async Task WithChildTimingsAsync()
        {
            var mp = await GetProfilerAsync(childDepth: 5).ConfigureAwait(false);
            AssertMiniProfilerExists(mp);
            AssertTimingsExist(mp, 6);

            var mp2 = await _testStorage.LoadAsync(mp.Id).ConfigureAwait(false);
            AssertProfilersAreEqual(mp, mp2);
        }

        private void AssertMiniProfilerExists(MiniProfiler miniProfiler)
        {
            var count = _conn.QuerySingle<int>("select count(*) from MiniProfilers where Id = @Id", new { miniProfiler.Id });
            Assert.Equal(1, count);
        }

        private void AssertTimingsExist(MiniProfiler profiler, int expected)
        {
            var count = _conn.QuerySingle<int>("select count(*) from MiniProfilerTimings where MiniProfilerId = @Id", new { profiler.Id });
            Assert.Equal(expected, count);
        }
    }
}
