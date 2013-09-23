using System.Collections;
using System.Data.Common;

namespace StackExchange.Profiling.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Data.SqlServerCe;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using NUnit.Framework;

    /// <summary>
    /// The base test.
    /// </summary>
    public abstract class BaseTest
    {
        /// <summary>
        /// Amount of time each <see cref="MiniProfilerExtensions.Step"/> will take for unit tests.
        /// </summary>
        public const int StepTimeMilliseconds = 1;

        /// <summary>
        /// Url that <see cref="GetRequest"/> and <see cref="GetProfiler"/> will hit.
        /// </summary>
        public const string DefaultRequestUrl = "http://localhost/Test.aspx";

        /// <summary>
        /// Initialises static members of the <see cref="BaseTest"/> class.
        /// </summary>
        static BaseTest()
        {
            // allows us to manually set ticks during tests
            MiniProfiler.Settings.StopwatchProvider = () => new UnitTestStopwatch();
        }

        /// <summary>
        /// Returns a simulated http request to <paramref name="url"/>.
        /// </summary>
        /// <param name="url">The url.</param>
        /// <param name="startAndStopProfiler">The start And Stop Profiler.</param>
        /// <returns>the request</returns>
        public static IDisposable GetRequest(string url = DefaultRequestUrl, bool startAndStopProfiler = true)
        {
            var result = new Subtext.TestLibrary.HttpSimulator();

            result.SimulateRequest(new Uri(url));

            if (startAndStopProfiler)
            {
                MiniProfiler.Start();
                result.OnBeforeDispose += () => MiniProfiler.Stop();
            }

            return result;
        }

        /// <summary>
        /// Returns a profiler for <paramref name="url"/>. Only child steps will take any time, 
        /// e.g. when <paramref name="childDepth"/> is 0, the resulting <see cref="MiniProfiler.DurationMilliseconds"/> will be zero.
        /// </summary>
        /// <param name="childDepth">number of levels of child steps underneath result's <see cref="MiniProfiler.Root"/></param>
        /// <param name="stepsEachTakeMilliseconds">Amount of time each step will "do work for" in each step</param>
        /// <returns>the mini profiler</returns>
        public static MiniProfiler GetProfiler(
            string url = DefaultRequestUrl, 
            int childDepth = 0, 
            int stepsEachTakeMilliseconds = StepTimeMilliseconds)
        {
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
                        IncrementStopwatch(stepsEachTakeMilliseconds);
                        step();
                    }
                }
            };

            using (GetRequest(url, startAndStopProfiler: false))
            {
                result = MiniProfiler.Start();
                step();
                MiniProfiler.Stop();
            }

            return result;
        }

        /// <summary>
        /// Increments the currently running <see cref="MiniProfiler.Stopwatch"/> by <paramref name="milliseconds"/>.
        /// </summary>
        /// <param name="milliseconds">The milliseconds.</param>
        public static void IncrementStopwatch(int milliseconds = StepTimeMilliseconds)
        {
            var sw = (UnitTestStopwatch)MiniProfiler.Current.Stopwatch;
            sw.ElapsedTicks += milliseconds * UnitTestStopwatch.TicksPerMillisecond;
        }

        /// <summary>
        /// Creates a <c>SqlCe</c> file database named after <typeparamref name="T"/>, returning the connection string to the database.
        /// </summary>
        public static string CreateSqlCeDatabase<T>(bool deleteIfExists = false, IEnumerable<string> sqlToExecute = null)
        {
            var filename = GetSqlCeFileNameFor<T>();
            var connString = GetSqlCeConnectionStringFor<T>();

            if (File.Exists(filename))
            {
                if (deleteIfExists)
                {
                    File.Delete(filename);
                }
                else
                {
                    return connString;
                }
            }

            var engine = new SqlCeEngine(connString);
            engine.CreateDatabase();

            if (sqlToExecute != null)
            {
                using (var conn = GetOpenSqlCeConnection<T>())
                {
                    foreach (var sql in sqlToExecute)
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = sql;
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }

            return connString;
        }

        /// <summary>
        /// Returns an open connection to the <c>SqlCe</c> database identified by <typeparamref name="T"/>. This database should have been
        /// created in <see cref="CreateSqlCeDatabase{T}"/>.
        /// </summary>
        /// <typeparam name="T">the connection type</typeparam>
        /// <returns>the connection</returns>
        public static SqlCeConnection GetOpenSqlCeConnection<T>()
        {
            var result = new SqlCeConnection(GetSqlCeConnectionStringFor<T>());
            result.Open();
            return result;
        }

        /// <summary>
        /// Returns an open connection that will have its queries profiled.
        /// </summary>
        public static DbConnection GetSqliteConnection()
        {
            DbConnection cnn = new System.Data.SQLite.SQLiteConnection("Data Source=:memory:");

            // to get profiling times, we have to wrap whatever connection we're using in a ProfiledDbConnection
            // when MiniProfiler.Current is null, this connection will not record any database timings
            if (MiniProfiler.Current != null)
            {
                cnn = new Data.ProfiledDbConnection(cnn, MiniProfiler.Current);
            }

            cnn.Open();
            return cnn;
        }

        /// <summary>
        /// The assert profilers are equal.
        /// </summary>
        public void AssertProfilersAreEqual(MiniProfiler mp1, MiniProfiler mp2)
        {
            Assert.AreEqual(mp1, mp2);
            AssertPublicPropertiesAreEqual(mp1, mp2);
            AssertTimingsAreEqualAndRecurse(mp1.Root, mp2.Root);
        }

        protected void AssertTimingsAreEqualAndRecurse(Timing t1, Timing t2)
        {
            Console.WriteLine();
            Console.WriteLine();

            Assert.NotNull(t1);
            Assert.NotNull(t2);

            AssertPublicPropertiesAreEqual(t1, t2);

            if (t1.CustomTimings != null || t2.CustomTimings != null)
            {
                Assert.NotNull(t1.CustomTimings);
                Assert.NotNull(t2.CustomTimings);

                Assert.AreEqual(t1.CustomTimings.Count, t2.CustomTimings.Count);

                foreach (var pair1 in t1.CustomTimings)
                {
                    Console.WriteLine();

                    var ct1 = pair1.Value;
                    List<CustomTiming> ct2;
                    Assert.True(t2.CustomTimings.TryGetValue(pair1.Key, out ct2));

                    for (int i = 0; i < ct1.Count; i++)
                    {
                        AssertPublicPropertiesAreEqual(ct1[i], ct2[i]);
                    }
                }
            }

            if (t1.Children != null || t2.Children != null)
            {
                Assert.NotNull(t1.Children);
                Assert.NotNull(t2.Children);

                Assert.AreEqual(t1.Children.Count, t2.Children.Count);

                for (int i = 0; i < t1.Children.Count; i++)
                {
                    AssertTimingsAreEqualAndRecurse(t1.Children[i], t2.Children[i]);
                }
            }
        }

        /// <summary>
        /// Doesn't handle collection properties!
        /// </summary>
        protected void AssertPublicPropertiesAreEqual<T>(T t1, T t2)
        {
            Assert.NotNull(t1);
            Assert.NotNull(t2);

            // we'll handle any collections elsewhere
            var props = from p in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        where p.IsDefined(typeof(System.Runtime.Serialization.DataMemberAttribute), false)
                        && !p.PropertyType.GetInterfaces().Any(i => i.Equals(typeof(IDictionary)) || i.Equals(typeof(IList)))
                        select p;

            foreach (var p in props)
            {
                try
                {
                    var val1 = p.GetValue(t1, null);
                    var val2 = p.GetValue(t2, null);

                    // datetimes are sometimes serialized with different precisions - just look care about the 10th of a second
                    if (p.PropertyType == typeof(DateTime))
                    {
                        val1 = TrimToDecisecond((DateTime)val1);
                        val2 = TrimToDecisecond((DateTime)val2);
                    }

                    var name = typeof(T).Name + "." + p.Name;
                    Assert.AreEqual(val1, val2, name + " have different values");
                    Console.WriteLine("{0, 50}: {1} == {2}", name, val1 ?? "<null>", val2 ?? "<null>");
                }
                catch (Exception ex)
                {
                    Assert.Fail("AssertPublicPropertiesAreEqual had an exception on " + p.Name + "; " + ex);
                }
            }
        }

        /// <summary>
        /// get the SQL CE file name for.
        /// </summary>
        /// <typeparam name="T">the database type</typeparam>
        /// <returns>a string containing the file name</returns>
        private static string GetSqlCeFileNameFor<T>()
        {
            return typeof(T).FullName + ".sdf";
        }

        /// <summary>
        /// get the SQL CE connection string for.
        /// </summary>
        /// <typeparam name="T">the database type</typeparam>
        /// <returns>the file name</returns>
        private static string GetSqlCeConnectionStringFor<T>()
        {
            return "Data Source = " + GetSqlCeFileNameFor<T>();
        }

        /// <summary>
        /// trim to <c>decisecond</c>.
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        /// <returns>the trimmed date</returns>
        private DateTime TrimToDecisecond(DateTime dateTime)
        {
            return new DateTime(dateTime.Ticks - (dateTime.Ticks % (TimeSpan.TicksPerSecond / 10)));
        }
    }
}
