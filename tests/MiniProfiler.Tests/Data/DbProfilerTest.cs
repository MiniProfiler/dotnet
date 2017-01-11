using System.Data.Common;

using Dapper;
using StackExchange.Profiling.Data;
using Xunit;

namespace StackExchange.Profiling.Tests.Data
{
    /// <summary>
    /// The profiler test.
    /// </summary>
    public class DbProfilerTest : BaseTest
    {
        public DbProfilerTest()
        {
            Utils.CreateSqlCeDatabase<DbProfilerTest>(sqlToExecute: new[] { "create table TestTable (Id int null)" });
        }

        [Fact]
        public void NonQuery()
        {
            using (var conn = GetConnection())
            {
                var profiler = conn.CountingProfiler;

                conn.Execute("insert into TestTable values (1)");
                Assert.Equal(1, profiler.ExecuteStartCount);
                Assert.Equal(1, profiler.ExecuteFinishCount);
                Assert.True(profiler.CompleteStatementMeasured);

                conn.Execute("delete from TestTable where Id = 1");
                Assert.Equal(2, profiler.ExecuteStartCount);
                Assert.Equal(2, profiler.ExecuteFinishCount);
                Assert.True(profiler.CompleteStatementMeasured);
            }
        }

        [Fact]
        public void Scalar()
        {
            using (var conn = GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                var profiler = conn.CountingProfiler;

                cmd.CommandText = "select 1";
                cmd.ExecuteScalar();

                Assert.Equal(1, profiler.ExecuteStartCount);
                Assert.Equal(1, profiler.ExecuteFinishCount);
                Assert.True(profiler.CompleteStatementMeasured);
            }
        }

        [Fact]
        public void DataReader()
        {
            using (var conn = GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                var profiler = conn.CountingProfiler;

                cmd.CommandText = "select 1";

                using (cmd.ExecuteReader())
                {
                }

                Assert.Equal(1, profiler.ExecuteStartCount);
                Assert.Equal(1, profiler.ExecuteFinishCount);
                Assert.Equal(1, profiler.ReaderFinishCount);
                Assert.True(profiler.CompleteStatementMeasured);
            }
        }

        [Fact]
        public void Errors()
        {
            using (var conn = GetConnection())
            {
                const string BadSql = "TROGDOR BURNINATE";

                try
                {
                    conn.Execute(BadSql);
                }
                catch (DbException) { /* yep */ }

                var profiler = conn.CountingProfiler;

                Assert.Equal(1, profiler.ErrorCount);
                Assert.Equal(1, profiler.ExecuteStartCount);
                Assert.Equal(1, profiler.ExecuteFinishCount);
                Assert.Equal(profiler.ErrorSql, BadSql);

                try
                {
                    conn.Query<int>(BadSql);
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
                        cmd.ExecuteScalar();
                    }
                }
                catch (DbException) { /* yep */ }

                Assert.Equal(3, profiler.ExecuteStartCount);
                Assert.Equal(3, profiler.ExecuteFinishCount);
                Assert.Equal(3, profiler.ErrorCount);
                Assert.Equal(profiler.ErrorSql, BadSql);
            }
        }

        /// <summary>
        /// The get connection.
        /// </summary>
        /// <returns>the counting connection</returns>
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
