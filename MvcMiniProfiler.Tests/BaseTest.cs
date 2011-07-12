using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.IO;
using System.Data.SqlServerCe;


namespace MvcMiniProfiler.Tests
{
    public abstract class BaseTest
    {

        const string cnnStr = "Data Source = Test.sdf;";
        public DbConnection connection;

        public BaseTest()
        {
            //if (File.Exists("Test.sdf"))
            //    File.Delete("Test.sdf");

            //var engine = new SqlCeEngine(cnnStr);
            //engine.CreateDatabase();
            //connection = new SqlCeConnection(cnnStr);
            //connection.Open();
        }

        public IDisposable SimulateRequest(string url)
        {
            var result = new Subtext.TestLibrary.HttpSimulator();

            result.SimulateRequest(new Uri(url));

            return result;
        }
    }
}
