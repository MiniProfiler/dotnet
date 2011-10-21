using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlServerCe;
using System.IO;
using MvcMiniProfiler.Data;
using NUnit.Framework;
using MvcMiniProfiler.Tests.Helpers;
using Dapper;

namespace MvcMiniProfiler.Tests.Data
{
    [TestFixture]
    public class IDbProfilerTest : BaseTest
    {
        class TestConnection : ProfiledDbConnection
        {
            public TestDbProfiler TestProfiler {get; set;}

            public TestConnection(DbConnection connection, IDbProfiler profiler) : base(connection, profiler)
            {
                TestProfiler = (TestDbProfiler)profiler;
            } 
        }
        string connectionString;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            connectionString = CreateSqlCeDatabase<IDbProfilerTest>(sqlToExecute: new[] { "create table TestTable (Id int null)" });
        }

        TestConnection GetConnection()
        {
            var connection = new SqlCeConnection(connectionString);
            connection.Open();
            return new TestConnection(connection, new TestDbProfiler());
        }

        [Test]
        public void ExecuteNonQuery()
        {
            using (var conn = GetConnection())
            {
                var profiler = conn.TestProfiler;

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
        public void ExecuteScalar()
        {
            using (var conn = GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                var profiler = conn.TestProfiler;

                cmd.CommandText = "select 1";
                cmd.ExecuteScalar();

                Assert.That(profiler.ExecuteStartCount == 1);
                Assert.That(profiler.ExecuteFinishCount == 1);
                Assert.That(profiler.CompleteStatementMeasured);
            }
        }


        [Test]
        public void ExecuteDataReader()
        {
            using (var conn = GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                var profiler = conn.TestProfiler;

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
        public void TestErrors()
        { 
            using (var conn = GetConnection())
            {
                string badSql = "TROGDOR BURNINATE";

                try 
                {
                    conn.Execute(badSql);
                }
                catch(DbException) { /**/ }

                var profiler = conn.TestProfiler;

                Assert.That(profiler.ErrorCount == 1);
                Assert.That(profiler.ExecuteStartCount == 1);
                Assert.That(profiler.ExecuteFinishCount == 1);
                Assert.That(profiler.ErrorSql == badSql);

                try
                {
                    conn.Query<int>(badSql);
                }
                catch (DbException) { /**/ }

                Assert.That(profiler.ErrorCount == 2);
                Assert.That(profiler.ExecuteStartCount == 2);
                Assert.That(profiler.ExecuteFinishCount == 2);
                Assert.That(profiler.ErrorSql == badSql);

                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = badSql;
                        cmd.ExecuteScalar();
                    }
                }
                catch (DbException) { /**/ }

                Assert.That(profiler.ExecuteStartCount == 3);
                Assert.That(profiler.ExecuteFinishCount == 3);
                Assert.That(profiler.ErrorCount == 3);
                Assert.That(profiler.ErrorSql == badSql);
            }
        }

        [Test]
        public void TestDataAdapter()
        {
            MiniProfiler mp;
            var factory = EFProfiledDbProviderFactory<SqlCeProviderFactory>.Instance;

            using (GetRequest())
            using (var da = factory.CreateDataAdapter())
            {
                var cmd = factory.CreateCommand();
                cmd.CommandText = "select 1 as A, 2 as B";
                da.SelectCommand = cmd;
                da.SelectCommand.Connection = GetConnection();

                DataSet ds = new DataSet();
                da.Fill(ds);

                Assert.That(((int)ds.Tables[0].Rows[0][0]) == 1);
                mp = MiniProfiler.Current;
            }

            Assert.That(mp.ExecutedReaders == 1);
            Assert.That(mp.ExecutedScalars == 0);
            Assert.That(mp.ExecutedNonQueries == 0);
        }


    }
}
