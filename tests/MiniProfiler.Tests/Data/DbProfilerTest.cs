using System.Data.Common;

using Dapper;
using NUnit.Framework;
using StackExchange.Profiling.Data;

namespace StackExchange.Profiling.Tests.Data
{
    /// <summary>
    /// The profiler test.
    /// </summary>
    [TestFixture]
    public class DbProfilerTest : BaseTest
    {
        public void TestFixtureSetUp()
        {
            CreateSqlCeDatabase<DbProfilerTest>(sqlToExecute: new[] { "create table TestTable (Id int null)" });
        }
        
        [Test]
        public void NonQuery()
        {
            using (var conn = GetConnection())
            {
                var profiler = conn.CountingProfiler;

                conn.Execute("insert into TestTable values (1)");
                Assert.That(profiler.ExecuteStartCount == 1);
                Assert.That(profiler.ExecuteFinishCount == 1);
                Assert.That(profiler.CompleteStatementMeasured);

                conn.Execute("delete from TestTable where Id = 1");
                Assert.That(profiler.ExecuteStartCount == 2);
                Assert.That(profiler.ExecuteFinishCount == 2);
                Assert.That(profiler.CompleteStatementMeasured);
            }
        }
        
        [Test]
        public void Scalar()
        {
            using (var conn = GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                var profiler = conn.CountingProfiler;

                cmd.CommandText = "select 1";
                cmd.ExecuteScalar();

                Assert.That(profiler.ExecuteStartCount == 1);
                Assert.That(profiler.ExecuteFinishCount == 1);
                Assert.That(profiler.CompleteStatementMeasured);
            }
        }
        
        [Test]
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

                Assert.That(profiler.ExecuteStartCount == 1);
                Assert.That(profiler.ExecuteFinishCount == 1);
                Assert.That(profiler.ReaderFinishCount == 1);
                Assert.That(profiler.CompleteStatementMeasured);
            }
        }
        
        [Test]
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

                Assert.That(profiler.ErrorCount == 1);
                Assert.That(profiler.ExecuteStartCount == 1);
                Assert.That(profiler.ExecuteFinishCount == 1);
                Assert.That(profiler.ErrorSql == BadSql);

                try
                {
                    conn.Query<int>(BadSql);
                }
                catch (DbException) { /* yep */ }

                Assert.That(profiler.ErrorCount == 2);
                Assert.That(profiler.ExecuteStartCount == 2);
                Assert.That(profiler.ExecuteFinishCount == 2);
                Assert.That(profiler.ErrorSql == BadSql);

                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = BadSql;
                        cmd.ExecuteScalar();
                    }
                }
                catch (DbException) { /* yep */ }

                Assert.That(profiler.ExecuteStartCount == 3);
                Assert.That(profiler.ExecuteFinishCount == 3);
                Assert.That(profiler.ErrorCount == 3);
                Assert.That(profiler.ErrorSql == BadSql);
            }
        }
        
        /// <summary>
        /// The get connection.
        /// </summary>
        /// <returns>the counting connection</returns>
        private CountingConnection GetConnection()
        {
            var connection = GetOpenSqlCeConnection<DbProfilerTest>();
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
