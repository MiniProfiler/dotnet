using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using StackExchange.Profiling.Data;
using StackExchange.Profiling.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Profiling.Tests
{
    /// <summary>
    /// Tests for <see cref="IDbProfiler"/>.
    /// </summary>
    public class DbProfilerTests : BaseTest, IClassFixture<SqliteFixture>
    {
        public SqliteFixture Fixture;

        public DbProfilerTests(SqliteFixture fixture, ITestOutputHelper output) : base(output)
        {
            Fixture = fixture;
        }

        [Fact]
        public void NonQuery()
        {
            using (var conn = GetConnection())
            {
                var profiler = conn.CountingProfiler;

                conn.Execute("CREATE TABLE TestTable (Id int null)");

                conn.Execute("INSERT INTO TestTable VALUES (1)");
                Assert.Equal(2, profiler.ExecuteStartCount);
                Assert.Equal(2, profiler.ExecuteFinishCount);
                Assert.True(profiler.CompleteStatementMeasured);

                conn.Execute("DELETE FROM TestTable WHERE Id = 1");
                Assert.Equal(3, profiler.ExecuteStartCount);
                Assert.Equal(3, profiler.ExecuteFinishCount);
                Assert.True(profiler.CompleteStatementMeasured);
            }
        }

        [Fact]
        public async Task NonQueryAsync()
        {
            using (var conn = GetConnection())
            {
                var profiler = conn.CountingProfiler;
                conn.Execute("CREATE TABLE TestTable (Id int null)");

                await conn.ExecuteAsync("INSERT INTO TestTable VALUES (1)").ConfigureAwait(false);
                Assert.Equal(2, profiler.ExecuteStartCount);
                Assert.Equal(2, profiler.ExecuteFinishCount);
                Assert.True(profiler.CompleteStatementMeasured);

                await conn.ExecuteAsync("DELETE FROM TestTable WHERE Id = 1").ConfigureAwait(false);
                Assert.Equal(3, profiler.ExecuteStartCount);
                Assert.Equal(3, profiler.ExecuteFinishCount);
                Assert.True(profiler.CompleteStatementMeasured);
            }
        }

        [Fact]
        public void Scalar()
        {
            using (var conn = GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                var profiler = conn.CountingProfiler;

                cmd.CommandText = "select 1";
                cmd.ExecuteScalar();

                Assert.Equal(1, profiler.ExecuteStartCount);
                Assert.Equal(1, profiler.ExecuteFinishCount);
                Assert.True(profiler.CompleteStatementMeasured);
            }
        }

        [Fact]
        public async Task ScalarAsync()
        {
            using (var conn = GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                var profiler = conn.CountingProfiler;

                cmd.CommandText = "select 1";
                await cmd.ExecuteScalarAsync().ConfigureAwait(false);

                Assert.Equal(1, profiler.ExecuteStartCount);
                Assert.Equal(1, profiler.ExecuteFinishCount);
                Assert.True(profiler.CompleteStatementMeasured);
            }
        }

        [Fact]
        public void DataReader()
        {
            using (var conn = GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                var profiler = conn.CountingProfiler;

                cmd.CommandText = "select 1";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.NextResult()) { }
                }

                Assert.Equal(1, profiler.ExecuteStartCount);
                Assert.Equal(1, profiler.ExecuteFinishCount);
                Assert.Equal(1, profiler.ReaderFinishCount);
                Assert.True(profiler.CompleteStatementMeasured);
            }
        }

        [Fact]
        public async Task DataReaderAsync()
        {
            using (var conn = GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                var profiler = conn.CountingProfiler;

                cmd.CommandText = "select 1";

                using (var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.NextResultAsync().ConfigureAwait(false)) { }
                }

                Assert.Equal(1, profiler.ExecuteStartCount);
                Assert.Equal(1, profiler.ExecuteFinishCount);
                Assert.Equal(1, profiler.ReaderFinishCount);
                Assert.True(profiler.CompleteStatementMeasured);
            }
        }

        [Fact]
        public void DataReaderViaProfiledDbCommandWithNullConnection()
        {
            // https://github.com/MiniProfiler/dotnet/issues/313

            using (var conn = GetConnection())
            {
                var command = new SqliteCommand("select 1");
                var wrappedCommand = new ProfiledDbCommand(command, conn, conn.Profiler);

                var reader = wrappedCommand.ExecuteReader(CommandBehavior.SequentialAccess);
                Assert.True(reader.Read());
            }
        }

        [Fact]
        public void Errors()
        {
            using (var conn = GetConnection())
            {
                const string BadSql = "TROGDOR BURNINATE";

                try
                {
                    conn.Execute(BadSql);
                }
                catch (DbException) { /* yep */ }

                var profiler = conn.CountingProfiler;

                Assert.Equal(1, profiler.ErrorCount);
                Assert.Equal(1, profiler.ExecuteStartCount);
                Assert.Equal(1, profiler.ExecuteFinishCount);
                Assert.Equal(profiler.ErrorSql, BadSql);

                try
                {
                    conn.Query<int>(BadSql);
                }
                catch (DbException) { /* yep */ }

                Assert.Equal(2, profiler.ErrorCount);
                Assert.Equal(2, profiler.ExecuteStartCount);
                Assert.Equal(2, profiler.ExecuteFinishCount);
                Assert.Equal(profiler.ErrorSql, BadSql);

                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = BadSql;
                        cmd.ExecuteScalar();
                    }
                }
                catch (DbException) { /* yep */ }

                Assert.Equal(3, profiler.ExecuteStartCount);
                Assert.Equal(3, profiler.ExecuteFinishCount);
                Assert.Equal(3, profiler.ErrorCount);
                Assert.Equal(profiler.ErrorSql, BadSql);
            }
        }

        [Fact]
        public async Task ErrorsAsync()
        {
            using (var conn = GetConnection())
            {
                const string BadSql = "TROGDOR BURNINATE";

                try
                {
                    await conn.ExecuteAsync(BadSql).ConfigureAwait(false);
                }
                catch (DbException) { /* yep */ }

                var profiler = conn.CountingProfiler;

                Assert.Equal(1, profiler.ErrorCount);
                Assert.Equal(1, profiler.ExecuteStartCount);
                Assert.Equal(1, profiler.ExecuteFinishCount);
                Assert.Equal(profiler.ErrorSql, BadSql);

                try
                {
                    await conn.QueryAsync<int>(BadSql).ConfigureAwait(false);
                }
                catch (DbException) { /* yep */ }

                Assert.Equal(2, profiler.ErrorCount);
                Assert.Equal(2, profiler.ExecuteStartCount);
                Assert.Equal(2, profiler.ExecuteFinishCount);
                Assert.Equal(profiler.ErrorSql, BadSql);

                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = BadSql;
                        await cmd.ExecuteScalarAsync().ConfigureAwait(false);
                    }
                }
                catch (DbException) { /* yep */ }

                Assert.Equal(3, profiler.ExecuteStartCount);
                Assert.Equal(3, profiler.ExecuteFinishCount);
                Assert.Equal(3, profiler.ErrorCount);
                Assert.Equal(profiler.ErrorSql, BadSql);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TrackingOptions(bool track)
        {
            var options = new MiniProfilerTestOptions { TrackConnectionOpenClose = track };
            var profiler = options.StartProfiler("Tracking: " + track);

            const string cmdString = "Select 1";
            GetUnopenedConnection(profiler).Query(cmdString);

            CheckConnectionTracking(track, profiler, cmdString, false, false);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TrackingOptionsExplicitClose(bool track)
        {
            var options = new MiniProfilerTestOptions { TrackConnectionOpenClose = track };
            var profiler = options.StartProfiler("Tracking: " + track);

            const string cmdString = "Select 1";
            var conn = GetUnopenedConnection(profiler);
            conn.Open();
            conn.Query(cmdString);
            conn.Close();

            CheckConnectionTracking(track, profiler, cmdString, false, true);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TrackingOptionsAsync(bool track)
        {
            var options = new MiniProfilerTestOptions { TrackConnectionOpenClose = track };
            var profiler = options.StartProfiler("Tracking: " + track);

            const string cmdString = "Select 1";
            await GetUnopenedConnection(profiler).QueryAsync(cmdString).ConfigureAwait(false);

            CheckConnectionTracking(track, profiler, cmdString, true, true);
        }

        [Fact]
        public void ShimProfiler()
        {
            var options = new MiniProfilerTestOptions();
            var profiler = options.StartProfiler("Shimming");
            var currentDbProfiler = new CurrentDbProfiler(() => MiniProfiler.Current);

            const string cmdString = "Select 1";
            GetUnopenedConnection(currentDbProfiler).Query(cmdString);

            CheckConnectionTracking(false, profiler, cmdString, false, false);
        }

        private class CurrentDbProfiler : IDbProfiler
        {
            private Func<IDbProfiler> GetProfiler { get; }
            public CurrentDbProfiler(Func<IDbProfiler> getProfiler) => GetProfiler = getProfiler;

            public bool IsActive => GetProfiler()?.IsActive ?? false;

            public void ExecuteFinish(IDbCommand profiledDbCommand, SqlExecuteType executeType, DbDataReader reader) =>
                GetProfiler()?.ExecuteFinish(profiledDbCommand, executeType, reader);

            public void ExecuteStart(IDbCommand profiledDbCommand, SqlExecuteType executeType) =>
                GetProfiler()?.ExecuteStart(profiledDbCommand, executeType);

            public void OnError(IDbCommand profiledDbCommand, SqlExecuteType executeType, Exception exception) =>
                GetProfiler()?.OnError(profiledDbCommand, executeType, exception);

            public void ReaderFinish(IDataReader reader) => GetProfiler()?.ReaderFinish(reader);
        }

        private void CheckConnectionTracking(bool track, MiniProfiler profiler, string command, bool async, bool expectClose)
        {
            Assert.NotNull(profiler.Root.CustomTimings);
            Assert.Single(profiler.Root.CustomTimings);
            var sqlTimings = profiler.Root.CustomTimings["sql"];
            Assert.NotNull(sqlTimings);

            if (track)
            {
                Assert.Equal(expectClose ? 3 : 2, sqlTimings.Count);
                Assert.Equal(async ? "Connection OpenAsync()" : "Connection Open()", sqlTimings[0].CommandString);
                Assert.Equal(command, sqlTimings[1].CommandString);
                if (expectClose)
                {
                    Assert.Equal("Connection Close()", sqlTimings[2].CommandString);
                }
            }
            else
            {
                Assert.Single(sqlTimings);
                Assert.Equal(command, sqlTimings[0].CommandString);
            }
        }

        private ProfiledDbConnection GetUnopenedConnection(IDbProfiler profiler) => new ProfiledDbConnection(Fixture.GetConnection(), profiler);

        private CountingConnection GetConnection()
        {
            var connection = Fixture.GetConnection();
            var result = new CountingConnection(connection, new CountingDbProfiler());
            result.Open();
            return result;
        }

        public class CountingConnection : ProfiledDbConnection
        {
            public CountingDbProfiler CountingProfiler { get; set; }

            public CountingConnection(DbConnection connection, IDbProfiler profiler)
                : base(connection, profiler)
            {
                CountingProfiler = (CountingDbProfiler)profiler;
            }
        }
    }

    public class SqliteFixture : IDisposable
    {
        private SqliteConnection Doorstop { get; }
        public SqliteConnection GetConnection() => new SqliteConnection("Data Source= :memory:; Cache = Shared");

        public SqliteFixture()
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    conn.Close();
                }
            }
            catch (Exception e)
            {
                Skip.Inconclusive("Sqlite Failure: " + e.Message);
            }
        }

        public void Dispose()
        {
            Doorstop?.Close();
        }
    }
}
