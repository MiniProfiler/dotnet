using System.Data;
using System.Data.Common;
using System.Data.SqlServerCe;
using System.Diagnostics;

using NUnit.Framework;

using Dapper;
using StackExchange.Profiling.Data;

namespace StackExchange.Profiling.Tests.Data
{
    /// <summary>
    /// The profiler test.
    /// </summary>
    [TestFixture]
    public class DbProfilerTest : BaseTest
    {
        /// <summary>
        /// The test fixture set up.
        /// </summary>
        public void TestFixtureSetUp()
        {
            BaseTest.CreateSqlCeDatabase<DbProfilerTest>(sqlToExecute: new[] { "create table TestTable (Id int null)" });
        }

        /// <summary>
        /// The non query.
        /// </summary>
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

        /// <summary>
        /// test scalar.
        /// </summary>
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

        /// <summary>
        /// The data reader.
        /// </summary>
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

        /// <summary>
        /// The errors.
        /// </summary>
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
                catch (DbException)
                {
                }

                var profiler = conn.CountingProfiler;

                Assert.That(profiler.ErrorCount == 1);
                Assert.That(profiler.ExecuteStartCount == 1);
                Assert.That(profiler.ExecuteFinishCount == 1);
                Assert.That(profiler.ErrorSql == BadSql);

                try
                {
                    conn.Query<int>(BadSql);
                }
                catch (DbException)
                {
                }

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
                catch (DbException)
                {
                }

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

        /// <summary>
        /// The counting connection.
        /// </summary>
        public class CountingConnection : ProfiledDbConnection
        {
            /// <summary>
            /// Initialises a new instance of the <see cref="CountingConnection"/> class. 
            /// </summary>
            /// <param name="connection">
            /// The connection.
            /// </param>
            /// <param name="profiler">
            /// The profiler.
            /// </param>
            public CountingConnection(DbConnection connection, IDbProfiler profiler)
                : base(connection, profiler)
            {
                CountingProfiler = (CountingDbProfiler)profiler;
            }

            /// <summary>
            /// Gets or sets the counting profiler.
            /// </summary>
            public CountingDbProfiler CountingProfiler { get; set; }
        }
    }
}
