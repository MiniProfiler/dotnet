using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlServerCe;
using System.IO;
using MvcMiniProfiler.Data;
using NUnit.Framework;

namespace MvcMiniProfiler.Tests.Data
{
    [TestFixture]
    public class IDbProfilerTest : BaseTest
    {
        const string cnnStr = "Data Source = Test.sdf;";
        public DbConnection connection;

        public IDbProfilerTest()
        {
            AppDomain.CurrentDomain.SetData("SQLServerCompactEditionUnderWebHosting", true);

            if (File.Exists("Test.sdf"))
                File.Delete("Test.sdf");

            var engine = new SqlCeEngine(cnnStr);
            engine.CreateDatabase();
            connection = new SqlCeConnection(cnnStr);
            connection.Open();
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
                da.SelectCommand.Connection = connection;

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
