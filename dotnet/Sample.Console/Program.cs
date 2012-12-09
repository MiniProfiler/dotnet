using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Profiling;

using Dapper;

namespace Sample.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            SetupProfiling();
            Test();
            Report();
        }

        static void SetupProfiling()
        {
            MiniProfiler.Settings.ProfilerProvider = new SingletonProfilerProvider();
        }

        static void Test()
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
            }

            MiniProfiler.Stop();
        }

        static void Report()
        {
            System.Console.WriteLine(MiniProfiler.Current.RenderPlainText());
            System.Console.ReadKey();
        }

        /// <summary>
        /// Returns an open connection that will have its queries profiled.
        /// </summary>
        static DbConnection GetConnection()
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
