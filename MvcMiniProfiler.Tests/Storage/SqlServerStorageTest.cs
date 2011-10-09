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

namespace MvcMiniProfiler.Tests.Storage
{
    [TestFixture]
    public class SqlServerStorageTest : BaseTest
    {
        static string Filename = typeof(SqlServerStorageTest).FullName + ".sdf";
        static string ConnectionString = "Data Source = " + Filename;

        public static SqlCeConnection GetOpenConnection()
        {
            var result = new SqlCeConnection(ConnectionString);
            result.Open();
            return result;
        }

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            CreateDatabase();
            MiniProfiler.Settings.Storage = new SqlCeStorage(ConnectionString);
        }

        private void CreateDatabase()
        {
            if (File.Exists(Filename))
                File.Delete(Filename);

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
        public void SaveResults()
        {
            var mp = GetProfiler();

            using (var conn = GetOpenConnection())
            {
                Assert.That(conn.Query<int>("select count(*) from MiniProfilers where Id = @Id", new { mp.Id }).Single() == 1);
                Assert.That(conn.Query<int>("select count(*) from MiniProfilerTimings where MiniProfilerId = @Id", new { mp.Id }).Single() == 1);
            }
        }
    }

    class SqlCeStorage : SqlServerStorage
    {
        public SqlCeStorage(string connectionString) : base(connectionString) { }

        protected override System.Data.Common.DbConnection GetConnection()
        {
            return new SqlCeConnection(ConnectionString);
        }
    }
}
