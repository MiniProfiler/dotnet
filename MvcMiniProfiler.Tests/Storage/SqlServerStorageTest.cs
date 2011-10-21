using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MvcMiniProfiler.Storage;
using NUnit.Framework;
using System.IO;
using System.Data.SqlServerCe;
using MvcMiniProfiler.Helpers.Dapper;
using MvcMiniProfiler.Data;

namespace MvcMiniProfiler.Tests.Storage
{
    [TestFixture]
    public class SqlServerStorageTest : BaseTest
    {
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            var sqlToExecute = SqlServerStorage.TableCreationScript.Replace("nvarchar(max)", "ntext").Split(';').Where(s => !string.IsNullOrWhiteSpace(s));
            var connStr = CreateSqlCeDatabase<SqlServerStorageTest>(sqlToExecute: sqlToExecute);

            MiniProfiler.Settings.Storage = new SqlCeStorage(connStr);
            _conn = GetOpenSqlCeConnection<SqlServerStorageTest>();
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

        [Test]
        public void WithSqlTimings()
        {
            MiniProfiler mp;

            using (GetRequest())
            using (var conn = GetProfiledConnection())
            {
                mp = MiniProfiler.Current;

                // one sql in the root timing
                conn.Query("select 1");

                using (mp.Step("Child step"))
                {
                    conn.Query("select 2 where 1 = @one", new { one = 1 });
                }
            }

            Assert.IsFalse(mp.HasDuplicateSqlTimings);

            AssertSqlTimingsExist(mp.Root, 1);
            var t = mp.Root.Children.Single();
            AssertSqlTimingsExist(t, 1);
            AssertSqlParametersExist(t.SqlTimings.Single(), 1);

            var mp2 = MiniProfiler.Settings.Storage.Load(mp.Id);
            AssertProfilersAreEqual(mp, mp2);
        }

        [Test]
        public void WithDuplicateSqlTimings()
        {
            MiniProfiler mp;

            using (GetRequest())
            using (var conn = GetProfiledConnection())
            {
                mp = MiniProfiler.Current;

                // one sql in the root timing
                conn.Query("select 1");

                using (mp.Step("Child step"))
                {
                    conn.Query("select 1");
                }
            }

            Assert.IsTrue(mp.HasDuplicateSqlTimings);

            AssertSqlTimingsExist(mp.Root, 1);
            AssertSqlTimingsExist(mp.Root.Children.Single(), 1);

            var mp2 = MiniProfiler.Settings.Storage.Load(mp.Id);
            AssertProfilersAreEqual(mp, mp2);
        }

        private SqlCeConnection _conn;

        private ProfiledDbConnection GetProfiledConnection()
        {
            return new ProfiledDbConnection(GetOpenSqlCeConnection<SqlServerStorageTest>(), MiniProfiler.Current);
        }

        private void AssertMiniProfilerExists(MiniProfiler mp)
        {
            Assert.That(_conn.Query<int>("select count(*) from MiniProfilers where Id = @Id", new { mp.Id }).Single() == 1);
        }

        private void AssertTimingsExist(MiniProfiler mp, int count)
        {
            Assert.That(_conn.Query<int>("select count(*) from MiniProfilerTimings where MiniProfilerId = @Id", new { mp.Id }).Single() == count);
        }

        private void AssertSqlTimingsExist(Timing t, int count)
        {
            Assert.That(_conn.Query<int>("select count(*) from MiniProfilerSqlTimings where ParentTimingId = @Id ", new { t.Id }).Single() == count);
        }

        private void AssertSqlParametersExist(SqlTiming s, int count)
        {
            Assert.That(_conn.Query<int>("select count(*) from MiniProfilerSqlTimingParameters where ParentSqlTimingId = @Id ", new { s.Id }).Single() == count);
        }
    }

    internal class SqlCeStorage : SqlServerStorage
    {
        public SqlCeStorage(string connectionString) : base(connectionString) { }

        protected override System.Data.Common.DbConnection GetConnection()
        {
            return new SqlCeConnection(ConnectionString);
        }

        /// <summary>
        /// CE doesn't support multiple result sets in one query.
        /// </summary>
        public override bool EnableBatchSelects
        {
            get { return false; }
        }
    }

}
