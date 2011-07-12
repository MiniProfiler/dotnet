using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.IO;
using System.Data.SqlServerCe;
using System.Threading;


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

        public static IDisposable SimulateRequest(string url)
        {
            var result = new Subtext.TestLibrary.HttpSimulator();

            result.SimulateRequest(new Uri(url));

            return result;
        }

        /// <summary>
        /// Returns a profiler for <paramref name="url"/>.
        /// </summary>
        /// <param name="childDepth">number of levels of child steps underneath result's <see cref="MiniProfiler.Root"/></param>
        /// <param name="stepSleepMilliseconds">Amount of time to sleep in each step</param>
        public static MiniProfiler GetProfiler(string url = "http://localhost/Test.aspx", int childDepth = 0, int stepSleepMilliseconds = 0)
        {
            if (childDepth < 0) childDepth = 0;
            if (stepSleepMilliseconds < 0) stepSleepMilliseconds = 0;

            MiniProfiler result = null;
            Action step = null;
            var curDepth = 0;

            // recursively add child steps
            step = () =>
            {
                if (curDepth++ < childDepth)
                {
                    using (result.Step("Depth " + curDepth))
                    {
                        Thread.Sleep(stepSleepMilliseconds);
                        step();
                    }
                }
            };

            using (var req = SimulateRequest(url))
            {
                result = MiniProfiler.Start();
                Thread.Sleep(stepSleepMilliseconds);
                step();
                MiniProfiler.Stop();
            }

            return result;
        }
    }
}
