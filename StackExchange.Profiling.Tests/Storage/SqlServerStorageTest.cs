namespace StackExchange.Profiling.Tests.Storage
{
    using System.Data.SqlServerCe;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    using NUnit.Framework;

    using StackExchange.Profiling.Data;
    using StackExchange.Profiling.Helpers.Dapper;
    using StackExchange.Profiling.Storage;

    /// <summary>
    /// The SQL server storage test.
    /// </summary>
    [TestFixture]
    public class SqlServerStorageTest : BaseTest
    {
        /// <summary>
        /// The connection.
        /// </summary>
        private SqlCeConnection _conn;

        /// <summary>
        /// The test fixture set up.
        /// </summary>
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            var sqlToExecute = SqlServerStorage.TableCreationScript.Replace("nvarchar(max)", "ntext").Split(';').Where(s => !string.IsNullOrWhiteSpace(s));
            var connStr = CreateSqlCeDatabase<SqlServerStorageTest>(sqlToExecute: sqlToExecute);

            MiniProfiler.Settings.Storage = new SqlCeStorage(connStr);
            this._conn = BaseTest.GetOpenSqlCeConnection<SqlServerStorageTest>();
        }

        /// <summary>
        /// The test fixture tear down.
        /// </summary>
        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            MiniProfiler.Settings.Storage = null;
        }

        /// <summary>
        /// The no child timings.
        /// </summary>
        [Test]
        public void NoChildTimings()
        {
            var mp = GetProfiler();
            this.AssertMiniProfilerExists(mp);
            this.AssertTimingsExist(mp, 1);

            var mp2 = MiniProfiler.Settings.Storage.Load(mp.Id);
            this.AssertProfilersAreEqual(mp, mp2);
        }

        /// <summary>
        /// with child timings.
        /// </summary>
        [Test]
        public void WithChildTimings()
        {
            var mp = GetProfiler(childDepth: 5);
            this.AssertMiniProfilerExists(mp);
            this.AssertTimingsExist(mp, 6);

            var mp2 = MiniProfiler.Settings.Storage.Load(mp.Id);
            this.AssertProfilersAreEqual(mp, mp2);
        }

        /// <summary>
        /// with SQL timings.
        /// </summary>
        [Test]
        public void WithSqlTimings()
        {
            MiniProfiler mp;

            using (BaseTest.GetRequest())
            using (var conn = this.GetProfiledConnection())
            {
                mp = MiniProfiler.Current;

                // one sql in the root timing
                conn.Query("select 1");

                using (mp.Step("Child step"))
                {
                    conn.Query("select 2 where 1 = @one", new { one = 1 });
                }
            }

            Assert.IsFalse(mp.HasDuplicateSqlTimings);

            this.AssertSqlTimingsExist(mp.Root, 1);
            var t = mp.Root.Children.Single();
            this.AssertSqlTimingsExist(t, 1);
            this.AssertSqlParametersExist(t.SqlTimings.Single(), 1);

            var mp2 = MiniProfiler.Settings.Storage.Load(mp.Id);
            this.AssertProfilersAreEqual(mp, mp2);
        }

        /// <summary>
        /// The with duplicate SQL timings.
        /// </summary>
        [Test]
        public void WithDuplicateSqlTimings()
        {
            MiniProfiler mp;

            using (BaseTest.GetRequest())
            using (var conn = this.GetProfiledConnection())
            {
                mp = MiniProfiler.Current;

                // one sql in the root timing
                conn.Query("select 1");

                using (mp.Step("Child step"))
                {
                    conn.Query("select 1");
                }
            }

            Assert.IsTrue(mp.HasDuplicateSqlTimings);

            this.AssertSqlTimingsExist(mp.Root, 1);
            this.AssertSqlTimingsExist(mp.Root.Children.Single(), 1);

            var mp2 = MiniProfiler.Settings.Storage.Load(mp.Id);
            this.AssertProfilersAreEqual(mp, mp2);
        }

        /// <summary>
        /// get the profiled connection.
        /// </summary>
        /// <returns>the profiled database connection</returns>
        private ProfiledDbConnection GetProfiledConnection()
        {
            return new ProfiledDbConnection(GetOpenSqlCeConnection<SqlServerStorageTest>(), MiniProfiler.Current);
        }

        /// <summary>
        /// The assert mini profiler exists.
        /// </summary>
        /// <param name="miniProfiler">The mini Profiler.</param>
        private void AssertMiniProfilerExists(MiniProfiler miniProfiler)
        {
            Assert.That(this._conn.Query<int>("select count(*) from MiniProfilers where Id = @Id", new { miniProfiler.Id }).Single() == 1);
        }

        /// <summary>
        /// The assert timings exist.
        /// </summary>
        /// <param name="profiler">The profiler.</param>
        /// <param name="count">The count.</param>
        private void AssertTimingsExist(MiniProfiler profiler, int count)
        {
            Assert.That(this._conn.Query<int>("select count(*) from MiniProfilerTimings where MiniProfilerId = @Id", new { profiler.Id }).Single() == count);
        }

        /// <summary>
        /// The assert SQL timings exist.
        /// </summary>
        /// <param name="timing">The timing.</param>
        /// <param name="count">The count.</param>
        private void AssertSqlTimingsExist(Timing timing, int count)
        {
            Assert.That(this._conn.Query<int>("select count(*) from MiniProfilerSqlTimings where ParentTimingId = @Id ", new { timing.Id }).Single() == count);
        }

        /// <summary>
        /// The assert SQL parameters exist.
        /// </summary>
        /// <param name="sqlTiming">The SQL Timing.</param>
        /// <param name="count">The count.</param>
        private void AssertSqlParametersExist(SqlTiming sqlTiming, int count)
        {
            Assert.That(this._conn.Query<int>("select count(*) from MiniProfilerSqlTimingParameters where ParentSqlTimingId = @Id ", new { sqlTiming.Id }).Single() == count);
        }
    }
}
