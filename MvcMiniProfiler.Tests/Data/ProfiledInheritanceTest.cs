using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

using MvcMiniProfiler.Data;
using MvcMiniProfiler.Helpers;
using System.Data.Common;

namespace MvcMiniProfiler.Tests.Data
{
    /// <summary>
    /// Tests different aspects of inheriting from <see cref="ProfiledDbConnection"/> and related classes.
    /// </summary>
    [TestFixture]
    public class ProfiledInheritanceTest : BaseTest
    {

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            CreateSqlCeDatabase<ProfiledInheritanceTest>(sqlToExecute: new[] { "create table TestTable (Id int null)" });
        }

        [Test]
        public void ExecuteNonQuery()
        {
            // with MiniProfiler
            using (GetRequest())
            using (var conn = GetChildConnection())
            {
                conn.Execute("insert into TestTable values (1)");
                Assert.That(conn.ExecutionCount == 1);
                Assert.That(conn.IsExecutionDurationValid);

                conn.Execute("delete from TestTable where Id = 1");
                Assert.That(conn.ExecutionCount == 2);
                Assert.That(conn.IsExecutionDurationValid);
            }

            // no MiniProfiler
            using (var conn = GetChildConnection())
            {
                conn.Execute("insert into TestTable values (1)");
                Assert.That(conn.ExecutionCount == 1);
                Assert.That(conn.IsExecutionDurationValid);

                conn.Execute("delete from TestTable where Id = 1");
                Assert.That(conn.ExecutionCount == 2);
                Assert.That(conn.IsExecutionDurationValid);
            }
        }

        [Test]
        public void ExecuteScalar()
        {
            // with MiniProfiler
            using (GetRequest())
            using (var conn = GetChildConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "select 1";
                cmd.ExecuteScalar();

                Assert.That(conn.ExecutionCount == 1);
                Assert.That(conn.IsExecutionDurationValid);
            }

            // no MiniProfiler
            using (var conn = GetChildConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "select 1";
                cmd.ExecuteScalar();

                Assert.That(conn.ExecutionCount == 1);
                Assert.That(conn.IsExecutionDurationValid);
            }
        }

        [Test]
        public void ExecuteDataReader()
        {
            // with MiniProfiler
            using (GetRequest())
            using (var conn = GetChildConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "select 1";

                using (cmd.ExecuteReader())
                {
                }

                Assert.That(conn.ExecutionCount == 1);
                Assert.That(conn.IsExecutionDurationValid);
            }

            // TODO: fix this test after conferring with Sam on how best to procede
            // no MiniProfiler
            using (var conn = GetChildConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "select 1";

                using (cmd.ExecuteReader())
                {
                }

                Assert.That(conn.ExecutionCount == 1);
                Assert.That(conn.IsExecutionDurationValid);
            }
        }

        private ChildConnection GetChildConnection()
        {
            return new ChildConnection(GetOpenSqlCeConnection<ProfiledInheritanceTest>(), MiniProfiler.Current);
        }

        /// <summary>
        /// Demonstrates wrapping a <see cref="ProfiledDbConnection"/> to collect extra data from any commands it creates.
        /// </summary>
        public class ChildConnection : ProfiledDbConnection
        {
            /// <summary>
            /// Number of sql commands that are executed on this connection.
            /// </summary>
            public int ExecutionCount { get; private set; }

            /// <summary>
            /// Arbitrary time units that execution took - for testing purposes, this should be incremented by 1 each time a query is executed.
            /// See <see cref="IsExecutionDurationValid"/>.
            /// </summary>
            public int ExecutionDuration { get; private set; }

            /// <summary>
            /// Returns true if every time <see cref="IncrementExecutionCount"/> was called, <see cref="ExecutionFinished"/> was also called.
            /// </summary>
            public bool IsExecutionDurationValid
            {
                get { return ExecutionCount == ExecutionDuration; }
            }

            public ChildConnection(DbConnection connection, MiniProfiler profiler)
                : base(connection, profiler)
            {
            }

            protected override DbCommand CreateDbCommand()
            {
                return new ChildCommand(InnerConnection.CreateCommand(), this);
            }

            public void IncrementExecutionCount()
            {
                ExecutionCount++;
            }

            public void ExecutionStarted()
            {
                // do nothing here - in real situations, you could start an independent Stopwatch
            }

            public void ExecutionFinished()
            {
                ExecutionDuration++;
            }
        }

        public class ChildCommand : ProfiledDbCommand
        {
            public ChildConnection ChildConnection { get; set; }

            public ChildCommand(DbCommand cmd, ChildConnection conn)
                : base(cmd, conn.InnerConnection, conn.Profiler)
            {
                ChildConnection = conn;
            }

            protected override DbDataReader ExecuteDbDataReader(System.Data.CommandBehavior behavior)
            {
                ChildConnection.IncrementExecutionCount();
                ChildConnection.ExecutionStarted();

                try
                {
                    return new ChildDataReader(base.ExecuteDbDataReader(behavior), ChildConnection);
                }
                catch
                {
                    ChildConnection.ExecutionFinished();
                    throw;
                }
            }

            public override int ExecuteNonQuery()
            {
                ChildConnection.IncrementExecutionCount();
                ChildConnection.ExecutionStarted();

                try
                {
                    return base.ExecuteNonQuery();
                }
                finally
                {
                    ChildConnection.ExecutionFinished();
                }
            }

            public override object ExecuteScalar()
            {
                ChildConnection.IncrementExecutionCount();
                ChildConnection.ExecutionStarted();

                try
                {
                    return base.ExecuteScalar();
                }
                finally
                {
                    ChildConnection.ExecutionFinished();
                }
            }
        }

        public class ChildDataReader : ProfiledDbDataReader
        {
            public ChildConnection ChildConnection { get; set; }

            public ChildDataReader(DbDataReader reader, ChildConnection conn)
                : base(reader, conn.InnerConnection, conn.Profiler)
            {
                ChildConnection = conn;
            }

            public override void Close()
            {
                base.Close();
                ChildConnection.ExecutionFinished();
            }
        }

    }
}
