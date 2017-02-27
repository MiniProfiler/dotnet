using System;
using System.Data.Common;
using System.Diagnostics;
using System.Net;
using System.Threading;

using Dapper;
using StackExchange.Profiling;

namespace Samples.Console
{
    /// <summary>
    /// simple sample console application.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// application entry point.
        /// </summary>
        /// <param name="args">application command line arguments.</param>
        public static void Main()
        {
            try
            {
                SetupProfiling();
                //Test();
                TestMultiThreaded();
                Report();

                if (Debugger.IsAttached)
                    System.Console.ReadKey();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex);
            }
        }

        /// <summary>
        /// setup the profiling.
        /// </summary>
        public static void SetupProfiling()
        {
            MiniProfiler.Settings.ProfilerProvider = new SingletonProfilerProvider();
        }

        /// <summary>
        /// test the profiling.
        /// </summary>
        public static void Test()
        {
            var mp = MiniProfiler.Start();

            using (mp.Step("Level 1"))
            using (var conn = GetConnection())
            {
                conn.Query<long>("select 1");

                using (mp.Step("Level 2"))
                {
                    conn.Query<long>("select 1");
                }

                using (var wc = new WebClient())
                using (mp.CustomTiming("http", "GET http://google.com"))
                {
                    wc.DownloadString("http://google.com");
                }
            }

            MiniProfiler.Stop();
        }

        public static void TestMultiThreaded()
        {
            var mp = MiniProfiler.Start("Locking");
            Action doWork = () => Thread.Sleep(new Random().Next(1, 50));

            using (mp.Step("outer"))
            {
                System.Threading.Tasks.Parallel.For(0, 5, i =>
                {
                    doWork();

                    using (mp.Step("step " + i))
                    {
                        doWork();

                        using (mp.Step("sub-step" + i))
                        {
                            doWork();
                        }
                    }
                });
            }
        }

        /// <summary>
        /// produce a profiling report.
        /// </summary>
        public static void Report()
        {
            System.Console.WriteLine(MiniProfiler.Current.RenderPlainText());
        }

        /// <summary>
        /// Returns an open connection that will have its queries profiled.
        /// </summary>
        /// <returns>the database connection abstraction</returns>
        public static DbConnection GetConnection()
        {
            DbConnection cnn = new System.Data.SQLite.SQLiteConnection("Data Source=:memory:");

            // to get profiling times, we have to wrap whatever connection we're using in a ProfiledDbConnection
            // when MiniProfiler.Current is null, this connection will not record any database timings
            if (MiniProfiler.Current != null)
            {
                cnn = new StackExchange.Profiling.Data.ProfiledDbConnection(cnn, MiniProfiler.Current);
            }

            cnn.Open();
            return cnn;
        }
    }
}
