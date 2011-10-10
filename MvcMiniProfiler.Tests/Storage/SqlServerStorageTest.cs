using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MvcMiniProfiler.Storage;
using NUnit.Framework;
using System.IO;
using System.Data.SqlServerCe;
using MvcMiniProfiler.Helpers;
using MvcMiniProfiler.Data;

namespace MvcMiniProfiler.Tests.Storage
{
    [TestFixture]
    public class SqlServerStorageTest : BaseTest
    {
        static string Filename = typeof(SqlServerStorageTest).FullName + ".sdf";
        static string ConnectionString = "Data Source = " + Filename;

        private SqlCeConnection _conn;

        public static SqlCeConnection GetOpenConnection()
        {
            var result = new SqlCeConnection(ConnectionString);
            result.Open();
            return result;
        }

        public static ProfiledDbConnection GetProfiledConnection()
        {
            return new ProfiledDbConnection(GetOpenConnection(), MiniProfiler.Current);
        }

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            CreateDatabase();
            MiniProfiler.Settings.Storage = new SqlCeStorage(ConnectionString);
            _conn = GetOpenConnection();
        }

        private void CreateDatabase()
        {
            // NOTE: if schema changes (hopefully not), then you'll have to delete this manually
            if (File.Exists(Filename))
                return;

            var engine = new SqlCeEngine(ConnectionString);
            engine.CreateDatabase();

            using (var conn = GetOpenConnection())
            {
                foreach (var sql in SqlServerStorage.TableCreationScript.Split(';').Where(s => !string.IsNullOrWhiteSpace(s)))
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = sql.Replace("nvarchar(max)", "ntext");
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            MiniProfiler.Settings.Storage = null;
        }

        [Test]
        public void Save_NoChildTimings()
        {
            var mp = GetProfiler();
            AssertMiniProfilerExists(mp);
            AssertTimingsExist(mp, 1);
        }

        [Test]
        public void Save_WithChildTimings()
        {
            var mp = GetProfiler(childDepth: 5);
            AssertMiniProfilerExists(mp);
            AssertTimingsExist(mp, 6);
        }

        [Test]
        public void Save_WithSqlTimings()
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
                    conn.Query("select 2");
                }
            }

            AssertSqlTimingsExistOnTiming(mp.Root, 1);
            AssertSqlTimingsExistOnTiming(mp.Root.Children.Single(), 1);
        }

        [Test]
        public void Load_NoChildTimings()
        {
            var mp = GetProfiler();
            var mp2 = MiniProfiler.Settings.Storage.Load(mp.Id);
        }

        private void AssertMiniProfilerExists(MiniProfiler mp)
        {
            Assert.That(_conn.Query<int>("select count(*) from MiniProfilers where Id = @Id", new { mp.Id }).Single() == 1);
        }

        private void AssertTimingsExist(MiniProfiler mp, int count)
        {
            Assert.That(_conn.Query<int>("select count(*) from MiniProfilerTimings where MiniProfilerId = @Id", new { mp.Id }).Single() == count);
        }

        private void AssertSqlTimingsExistOnTiming(Timing t, int count)
        {
            Assert.That(_conn.Query<int>("select count(*) from MiniProfilerSqlTimings where ParentTimingId = @Id ", new { t.Id }).Single() == count);
        }
    }

    class SqlCeStorage : SqlServerStorage
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
