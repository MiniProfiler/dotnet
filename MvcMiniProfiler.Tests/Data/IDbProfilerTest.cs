using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MvcMiniProfiler.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;

using Assert = NUnit.Framework.Assert;
using System.IO;
using System.Data.Common;
using System.Data.SqlServerCe;

namespace MvcMiniProfiler.Tests.Data
{
    [TestClass]
    public class IDbProfilerTest : IDbProfiler
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

        [TestMethod]
        public void TestDataAdapter()
        {
            var factory = new EFProfiledDbProviderFactory(this, new System.Data.SqlServerCe.SqlCeProviderFactory());

            using (var da = factory.CreateDataAdapter())
            {
                var cmd = factory.CreateCommand();
                cmd.CommandText = "select 1 as A, 2 as B";
                da.SelectCommand = cmd;
                da.SelectCommand.Connection = connection;

                DataSet ds = new DataSet();
                da.Fill(ds);

                Assert.That(((int)ds.Tables[0].Rows[0][0]) == 1);
            }

            Assert.That(ExecuteStartCount == 1);
            Assert.That(ReaderFinishCount == 1);
            Assert.That(ExecuteFinishCount == 1);
        }

        // IDbProfiler members
        public int ExecuteStartCount { get; set; }
        public int ExecuteFinishCount { get; set; }
        public int ReaderFinishCount { get; set; }

        public void ExecuteStart(System.Data.Common.DbCommand profiledDbCommand, ExecuteType executeType)
        {
            ExecuteStartCount++;
        }

        public void ExecuteFinish(System.Data.Common.DbCommand profiledDbCommand, ExecuteType executeType, System.Data.Common.DbDataReader reader)
        {
            ExecuteFinishCount++;
        }

        public void ExecuteFinish(System.Data.Common.DbCommand profiledDbCommand, ExecuteType executeType)
        {
            ExecuteFinishCount++;
        }

        public void ReaderFinish(System.Data.Common.DbDataReader reader)
        {
            ReaderFinishCount++;
        }

        public bool IsActive { get; set; }
    }
}
