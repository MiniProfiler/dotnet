using System.Data.Common;

using Dapper;
using StackExchange.Profiling.Data;
using Xunit;

namespace StackExchange.Profiling.Tests.Data
{
    /// <summary>
    /// The profiler test.
    /// </summary>
    [Collection("DbProfiler")]
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
                Assert.Equal(profiler.ExecuteStartCount, 1);
                Assert.Equal(profiler.ExecuteFinishCount, 1);
                Assert.True(profiler.CompleteStatementMeasured);

                conn.Execute("delete from TestTable where Id = 1");
                Assert.Equal(profiler.ExecuteStartCount, 2);
                Assert.Equal(profiler.ExecuteFinishCount, 2);
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

                Assert.Equal(profiler.ExecuteStartCount, 1);
                Assert.Equal(profiler.ExecuteFinishCount, 1);
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

                Assert.Equal(profiler.ExecuteStartCount, 1);
                Assert.Equal(profiler.ExecuteFinishCount, 1);
                Assert.Equal(profiler.ReaderFinishCount, 1);
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

                Assert.Equal(profiler.ErrorCount, 1);
                Assert.Equal(profiler.ExecuteStartCount, 1);
                Assert.Equal(profiler.ExecuteFinishCount, 1);
                Assert.Equal(profiler.ErrorSql, BadSql);

                try
                {
                    conn.Query<int>(BadSql);
                }
                catch (DbException) { /* yep */ }

                Assert.Equal(profiler.ErrorCount, 2);
                Assert.Equal(profiler.ExecuteStartCount, 2);
                Assert.Equal(profiler.ExecuteFinishCount, 2);
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

                Assert.Equal(profiler.ExecuteStartCount, 3);
                Assert.Equal(profiler.ExecuteFinishCount, 3);
                Assert.Equal(profiler.ErrorCount, 3);
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
