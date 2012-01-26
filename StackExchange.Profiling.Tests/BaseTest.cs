using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using System.Data.SqlServerCe;
using System.IO;
using System.Collections.Generic;

namespace StackExchange.Profiling.Tests
{
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

        static BaseTest()
        {
            // allows us to manually set ticks during tests
            MiniProfiler.Settings.StopwatchProvider = () => new UnitTestStopwatch();
        }

        /// <summary>
        /// Returns a simulated http request to <paramref name="url"/>.
        /// </summary>
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
        /// Returns a profiler for <paramref name="url"/>. Only child steps will take any time, e.g. when <paramref name="childDepth"/> is 0, the
        /// resulting <see cref="MiniProfiler.DurationMilliseconds"/> will be zero.
        /// </summary>
        /// <param name="childDepth">number of levels of child steps underneath result's <see cref="MiniProfiler.Root"/></param>
        /// <param name="stepsEachTakeMilliseconds">Amount of time each step will "do work for" in each step</param>
        public static MiniProfiler GetProfiler(string url = DefaultRequestUrl, int childDepth = 0, int stepsEachTakeMilliseconds = StepTimeMilliseconds)
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
        /// <param name="milliseconds"></param>
        public static void IncrementStopwatch(int milliseconds = StepTimeMilliseconds)
        {
            var sw = (UnitTestStopwatch)MiniProfiler.Current.Stopwatch;
            sw.ElapsedTicks += milliseconds * UnitTestStopwatch.TicksPerMillisecond;
        }

        public void AssertProfilersAreEqual(MiniProfiler mp1, MiniProfiler mp2)
        {
            Assert.AreEqual(mp1, mp2);
            AssertPublicPropertiesAreEqual(mp1, mp2);

            var timings1 = mp1.GetTimingHierarchy().ToList();
            var timings2 = mp2.GetTimingHierarchy().ToList();

            Assert.That(timings1.Count == timings2.Count);
            for (int i = 0; i < timings1.Count; i++)
            {
                var t1 = timings1[i];
                var t2 = timings2[i];
                Assert.AreEqual(t1, t2);

                Console.WriteLine();
                AssertPublicPropertiesAreEqual(t1, t2);

                if (!t1.HasSqlTimings && !t2.HasSqlTimings) continue;

                Assert.NotNull(t1.SqlTimings);
                Assert.NotNull(t2.SqlTimings);

                for (int j = 0; j < t1.SqlTimings.Count; j++)
                {
                    var s1 = t1.SqlTimings[j];
                    var s2 = t2.SqlTimings[j];
                    Assert.AreEqual(s1, s2);

                    Console.WriteLine();
                    AssertPublicPropertiesAreEqual(s1, s2);

                    if (s1.Parameters == null && s2.Parameters == null) continue;

                    Assert.NotNull(s1.Parameters);
                    Assert.NotNull(s2.Parameters);

                    for (int k = 0; k < s1.Parameters.Count; k++)
                    {
                        var p1 = s1.Parameters[k];
                        var p2 = s2.Parameters[k];
                        Assert.AreEqual(p1, p2);

                        Console.WriteLine();
                        AssertPublicPropertiesAreEqual(p1, p2);
                    }
                }
            }
        }

        protected void AssertPublicPropertiesAreEqual<T>(T t1, T t2)
        {
            Assert.NotNull(t1);
            Assert.NotNull(t2);

            // check public properties
            var props = from p in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        select p;

            foreach (var p in props)
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
        }

        private DateTime TrimToDecisecond(DateTime d)
        {
            return new DateTime(d.Ticks - (d.Ticks % (TimeSpan.TicksPerSecond / 10)));
        }

        /// <summary>
        /// Creates a SqlCe file database named after <typeparamref name="T"/>, returning the connection string to the database.
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
        /// Returns an open connection to the SqlCe database identified by <typeparamref name="T"/>. This database should have been
        /// created in <see cref="CreateSqlCeDatabase{T}"/>.
        /// </summary>
        public static SqlCeConnection GetOpenSqlCeConnection<T>()
        {
            var result = new SqlCeConnection(GetSqlCeConnectionStringFor<T>());
            result.Open();
            return result;
        }

        private static string GetSqlCeFileNameFor<T>()
        {
            return typeof(T).FullName + ".sdf";
        }

        private static string GetSqlCeConnectionStringFor<T>()
        {
            return "Data Source = " + GetSqlCeFileNameFor<T>();
        }
    }

    public class UnitTestStopwatch : StackExchange.Profiling.Helpers.IStopwatch
    {
        bool _isRunning = true;

        public long ElapsedTicks { get; set; }

        public static readonly long TicksPerSecond = TimeSpan.FromSeconds(1).Ticks;
        public static readonly long TicksPerMillisecond = TimeSpan.FromMilliseconds(1).Ticks;

        /// <summary>
        /// <see cref="MiniProfiler.GetRoundedMilliseconds"/> method will use this to determine how many ticks actually elapsed, so make it simple.
        /// </summary>
        public long Frequency
        {
            get { return TicksPerSecond; }
        }

        public bool IsRunning
        {
            get { return _isRunning; }
        }

        public void Stop()
        {
            _isRunning = false;
        }

    }

}
