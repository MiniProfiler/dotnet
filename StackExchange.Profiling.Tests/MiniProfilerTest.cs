using System.Data.Common;
using NUnit.Framework;
using System;
using System.ComponentModel;
using System.Data;
using System.Runtime.Remoting;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using StackExchange.Profiling.Data;
using IsolationLevel = System.Data.IsolationLevel;

namespace StackExchange.Profiling.Tests
{
    [TestFixture]
    public class MiniProfilerTest : BaseTest
    {
        [Test]
        public void Simple()
        {
            using (GetRequest("http://localhost/Test.aspx", startAndStopProfiler: false))
            {
                MiniProfiler.Start();
                IncrementStopwatch(); // 1 ms
                MiniProfiler.Stop();

                var c = MiniProfiler.Current;

                Assert.That(c, Is.Not.Null);
                Assert.That(c.DurationMilliseconds, Is.EqualTo(StepTimeMilliseconds));
                Assert.That(c.Name, Is.EqualTo("/Test.aspx"));

                Assert.That(c.Root, Is.Not.Null);
                Assert.That(c.Root.HasChildren, Is.False);
            }
        }

        [Test]
        public void StepIf_Basic()
        {
            using (GetRequest())
            {
                MiniProfiler.Start();
                var mp1 = MiniProfiler.Current;

                IncrementStopwatch(); // 1 ms
                Timing goodTiming;
                Timing badTiming;

                using (goodTiming = (Timing)(mp1.StepIf("Yes", 1)))
                {
                    IncrementStopwatch(2);
                }
                using (badTiming = (Timing)(mp1.StepIf("No", 5)))
                {
                    IncrementStopwatch(); // 1 ms
                }
                MiniProfiler.Stop();

                Assert.IsTrue(mp1.Root.Children.Contains(goodTiming));
                Assert.IsTrue(!mp1.Root.Children.Contains(badTiming));
            }
        }

        [Test]
        public void StepIf_IncludeChildren()
        {
            using (GetRequest())
            {
                MiniProfiler.Start();
                var mp1 = MiniProfiler.Current;

                IncrementStopwatch(); // 1 ms
                Timing goodTiming;
                Timing badTiming;

                using (goodTiming = (Timing)(mp1.StepIf("Yes", 5, true)))
                {
                    IncrementStopwatch(2);
                    using (mp1.Step("#1"))
                    {
                        IncrementStopwatch(2);
                    }
                    using (mp1.Step("#2"))
                    {
                        IncrementStopwatch(2);
                    }
                }
                using (badTiming = (Timing)(mp1.StepIf("No", 5, false)))
                {
                    IncrementStopwatch(2);
                    using (mp1.Step("#1"))
                    {
                        IncrementStopwatch(2);
                    }
                    using (mp1.Step("#2"))
                    {
                        IncrementStopwatch(2);
                    }
                }
                MiniProfiler.Stop();

                Assert.IsTrue(mp1.Root.Children.Contains(goodTiming));
                Assert.IsTrue(!mp1.Root.Children.Contains(badTiming));
            }
        }

        [Test]
        public void CustomTimingIf_Basic()
        {
            using (GetRequest())
            {
                MiniProfiler.Start();
                var mp1 = MiniProfiler.Current;

                IncrementStopwatch(); // 1 ms
                CustomTiming goodTiming;
                CustomTiming badTiming;

                using (goodTiming = mp1.CustomTimingIf("Cat1", "Yes", 1))
                {
                    IncrementStopwatch(2);
                }
                using (badTiming = mp1.CustomTimingIf("Cat1", "No", 5))
                {
                    IncrementStopwatch(); // 1 ms
                }
                MiniProfiler.Stop();

                Assert.IsTrue(mp1.Root.CustomTimings["Cat1"].Contains(goodTiming));
                Assert.IsTrue(!mp1.Root.CustomTimings["Cat1"].Contains(badTiming));
            }
        }

        [Test]
        public void DiscardResults()
        {
            using (GetRequest(startAndStopProfiler: false))
            {
                MiniProfiler.Start();
                MiniProfiler.Stop(discardResults: true);

                var c = MiniProfiler.Current;

                Assert.That(c, Is.Null);
            }
        }

#if NET45
        [Test]
        public void ConfirmAsync()
        {
            CreateSqlCeDatabase<string>(true);

            var slowDbConnection = new SlowDbConnection(GetOpenSqlCeConnection<string>());
            var queryCompleted = false;

            slowDbConnection.QueryCompleted += () => queryCompleted = true;

            using (var connection = new ProfiledDbConnection(slowDbConnection, new NullLogger()))
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT 1;";
                    var tokenSource = new CancellationTokenSource();
                    command.ExecuteNonQueryAsync(tokenSource.Token)
                        .ContinueWith(t => { }, TaskContinuationOptions.OnlyOnFaulted);
                    tokenSource.Cancel();

                    Assert.That(queryCompleted, Is.False);
                }
            }
        }

        private class NullLogger : IDbProfiler
        {
            public bool IsActive { get; }

            public NullLogger()
            {
                IsActive = true;
            }

            public void ExecuteStart(IDbCommand profiledDbCommand, SqlExecuteType executeType)
            {
            }

            public void ExecuteFinish(IDbCommand profiledDbCommand, SqlExecuteType executeType, DbDataReader reader)
            {
            }

            public void ReaderFinish(IDataReader reader)
            {
            }

            public void OnError(IDbCommand profiledDbCommand, SqlExecuteType executeType, Exception exception)
            {
            }
        }

        private class SlowDbCommand : DbCommand
        {
            private readonly DbCommand _command;

            public event Action QueryCompleted;

            public SlowDbCommand(DbCommand command)
            {
                _command = command;
            }

            public override string CommandText
            {
                get { return _command.CommandText; }
                set { _command.CommandText = value; }
            }

            public override int CommandTimeout
            {
                get { return _command.CommandTimeout; }
                set { _command.CommandTimeout = value; }
            }

            public override CommandType CommandType
            {
                get { return _command.CommandType; }
                set { _command.CommandType = value; }
            }

            public override UpdateRowSource UpdatedRowSource
            {
                get { return _command.UpdatedRowSource; }
                set { _command.UpdatedRowSource = value; }
            }

            protected override DbConnection DbConnection
            {
                get { return _command.Connection; }
                set { _command.Connection = value; }
            }

            protected override DbTransaction DbTransaction
            {
                get { return _command.Transaction; }
                set { _command.Transaction = value; }
            }

            public override bool DesignTimeVisible
            {
                get { return _command.DesignTimeVisible; }
                set { _command.DesignTimeVisible = value; }
            }

            protected override DbParameterCollection DbParameterCollection => _command.Parameters;

            public override void Prepare()
            {
                _command.Prepare();
            }

            public override void Cancel()
            {
                _command.Cancel();
            }

            protected override DbParameter CreateDbParameter()
            {
                return _command.CreateParameter();
            }

            private T ExecuteSlowly<T>(Func<T> func)
            {
                Thread.Sleep(TimeSpan.FromSeconds(10));

                var result = func();

                var queryCompleted = QueryCompleted;
                queryCompleted?.Invoke();

                return result;
            }

            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
            {
                return ExecuteSlowly(() => _command.ExecuteReader(behavior));
            }

            public override int ExecuteNonQuery()
            {
                return ExecuteSlowly(() => _command.ExecuteNonQuery());
            }

            public override object ExecuteScalar()
            {
                return ExecuteSlowly(() => _command.ExecuteScalar());
            }

            private async Task<T> ExecuteSlowlyAsync<T>(Func<Task<T>> func, CancellationToken cancellationToken)
            {
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);

                var result = await func();

                QueryCompleted?.Invoke();
                
                return result;
            }

            protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
            {
                return ExecuteSlowlyAsync(
                    () => _command.ExecuteReaderAsync(behavior, cancellationToken),
                    cancellationToken);
            }

            public override Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
            {
                return ExecuteSlowlyAsync(
                    () => _command.ExecuteScalarAsync(cancellationToken),
                    cancellationToken);
            }

            public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
            {
                return ExecuteSlowlyAsync(
                    () => _command.ExecuteNonQueryAsync(cancellationToken),
                    cancellationToken);
            }
        }

        private class SlowDbConnection : DbConnection
        {
            private readonly DbConnection _connection;

            public event Action QueryCompleted;

            public SlowDbConnection(DbConnection connection)
            {
                _connection = connection;
            }

            public override ISite Site
            {
                get { return _connection.Site; }
                set { _connection.Site = value; }
            }

            public override string ConnectionString
            {
                get { return _connection.ConnectionString; }
                set { _connection.ConnectionString = value; }
            }

            public override event StateChangeEventHandler StateChange
            {
                add { _connection.StateChange += value; }
                remove { _connection.StateChange -= value; }
            }

            public override string Database => _connection.Database;

            public override ConnectionState State => _connection.State;

            public override string DataSource => _connection.DataSource;

            public override string ServerVersion => _connection.ServerVersion;

            public override object InitializeLifetimeService()
            {
                return _connection.InitializeLifetimeService();
            }

            public override ObjRef CreateObjRef(Type requestedType)
            {
                return _connection.CreateObjRef(requestedType);
            }

            protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
            {
                return _connection.BeginTransaction();
            }

            protected override DbCommand CreateDbCommand()
            {
                var command = new SlowDbCommand(_connection.CreateCommand());
                command.QueryCompleted += OnQueryCompleted;

                return command;
            }

            private void OnQueryCompleted()
            {
                QueryCompleted?.Invoke();
            }

            public override void ChangeDatabase(string databaseName)
            {
                _connection.ChangeDatabase(databaseName);
            }

            public override void EnlistTransaction(Transaction transaction)
            {
                _connection.EnlistTransaction(transaction);
            }

            public override DataTable GetSchema()
            {
                return _connection.GetSchema();
            }

            public override DataTable GetSchema(string collectionName)
            {
                return _connection.GetSchema(collectionName);
            }

            public override DataTable GetSchema(string collectionName, string[] restrictionValues)
            {
                return _connection.GetSchema(collectionName, restrictionValues);
            }

            public override void Open()
            {
                _connection.Open();
            }

            public override void Close()
            {
                _connection.Close();
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                _connection.Dispose();
            }
        }
#endif
    }
}
