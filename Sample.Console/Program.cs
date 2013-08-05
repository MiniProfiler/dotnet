namespace Sample.Console
{
    using System.Data.Common;

    using Dapper;

    using StackExchange.Profiling;

    /// <summary>
    /// simple sample console application.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// application entry point.
        /// </summary>
        /// <param name="args">application command line arguments.</param>
        public static void Main(string[] args)
        {
            SetupProfiling();
            Test();
            Report();
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
            }

            MiniProfiler.Stop();
        }

        /// <summary>
        /// produce a profiling report.
        /// </summary>
        public static void Report()
        {
            System.Console.WriteLine(MiniProfiler.Current.RenderPlainText());
            System.Console.ReadKey();
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
