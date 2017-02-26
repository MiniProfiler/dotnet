using System.Data.Common;

using Dapper;
using StackExchange.Profiling.Data;
using Xunit;
using System.Threading.Tasks;

namespace Tests.Data
{
    /// <summary>
    /// The profiler test.
    /// </summary>
    public class DbProfilerTestAsync : BaseTest
    {
        public DbProfilerTestAsync()
        {
            Utils.CreateSqlCeDatabase<DbProfilerTest>(sqlToExecute: new[] { "create table TestTable (Id int null)" });
        }

        [Fact]
        public async Task NonQueryAsync()
        {
            using (var conn = GetConnection())
            {
                var profiler = conn.CountingProfiler;

                await conn.ExecuteAsync("insert into TestTable values (1)").ConfigureAwait(false);
                Assert.Equal(1, profiler.ExecuteStartCount);
                Assert.Equal(1, profiler.ExecuteFinishCount);
                Assert.True(profiler.CompleteStatementMeasured);

                await conn.ExecuteAsync("delete from TestTable where Id = 1").ConfigureAwait(false);
                Assert.Equal(2, profiler.ExecuteStartCount);
                Assert.Equal(2, profiler.ExecuteFinishCount);
                Assert.True(profiler.CompleteStatementMeasured);
            }
        }

        [Fact]
        public async Task ScalarAsync()
        {
            using (var conn = GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                var profiler = conn.CountingProfiler;

                cmd.CommandText = "select 1";
                await cmd.ExecuteScalarAsync().ConfigureAwait(false);

                Assert.Equal(1, profiler.ExecuteStartCount);
                Assert.Equal(1, profiler.ExecuteFinishCount);
                Assert.True(profiler.CompleteStatementMeasured);
            }
        }

        [Fact]
        public async Task DataReader()
        {
            using (var conn = GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                var profiler = conn.CountingProfiler;

                cmd.CommandText = "select 1";

                using (await cmd.ExecuteReaderAsync().ConfigureAwait(false))
                {
                }

                Assert.Equal(1, profiler.ExecuteStartCount);
                Assert.Equal(1, profiler.ExecuteFinishCount);
                Assert.Equal(1, profiler.ReaderFinishCount);
                Assert.True(profiler.CompleteStatementMeasured);
            }
        }

        [Fact]
        public async Task Errors()
        {
            using (var conn = GetConnection())
            {
                const string BadSql = "TROGDOR BURNINATE";

                try
                {
                    await conn.ExecuteAsync(BadSql).ConfigureAwait(false);
                }
                catch (DbException) { /* yep */ }

                var profiler = conn.CountingProfiler;

                Assert.Equal(1, profiler.ErrorCount);
                Assert.Equal(1, profiler.ExecuteStartCount);
                Assert.Equal(1, profiler.ExecuteFinishCount);
                Assert.Equal(profiler.ErrorSql, BadSql);

                try
                {
                    await conn.QueryAsync<int>(BadSql).ConfigureAwait(false);
                }
                catch (DbException) { /* yep */ }

                Assert.Equal(2, profiler.ErrorCount);
                Assert.Equal(2, profiler.ExecuteStartCount);
                Assert.Equal(2, profiler.ExecuteFinishCount);
                Assert.Equal(profiler.ErrorSql, BadSql);

                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = BadSql;
                        await cmd.ExecuteScalarAsync().ConfigureAwait(false);
                    }
                }
                catch (DbException) { /* yep */ }

                Assert.Equal(3, profiler.ExecuteStartCount);
                Assert.Equal(3, profiler.ExecuteFinishCount);
                Assert.Equal(3, profiler.ErrorCount);
                Assert.Equal(profiler.ErrorSql, BadSql);
            }
        }

        private CountingConnection GetConnection()
        {
            var connection = Utils.GetOpenSqlCeConnection<DbProfilerTest>();
            return new CountingConnection(connection, new CountingDbProfiler());
        }

        public class CountingConnection : ProfiledDbConnection
        {
            public CountingDbProfiler CountingProfiler { get; set; }

            public CountingConnection(DbConnection connection, IDbProfiler profiler)
                : base(connection, profiler)
            {
                CountingProfiler = (CountingDbProfiler)profiler;
            }
        }
    }
}
