using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DiagnosticAdapter;
using StackExchange.Profiling.Internal;
using System;
using System.Collections.Concurrent;
using System.Data.Common;

namespace StackExchange.Profiling.Data
{
    /// <summary>
    /// Diagnostic listener for Microsoft.EntityFrameworkCore.* events
    /// </summary>
    public class RelationalDiagnosticListener : IMiniProfilerDiagnosticListener
    {
        // Maps to https://github.com/aspnet/EntityFramework/blob/f386095005e46ea3aa4d677e4439cdac113dbfb1/src/EFCore.Relational/Internal/RelationalDiagnostics.cs
        // See https://github.com/aspnet/EntityFramework/issues/7939 for info

        public string ListenerName => "Microsoft.EntityFrameworkCore";

        // Commands
        // Tracking currently open items, connections, and transactions, for logging upon their completion or error
        private readonly ConcurrentDictionary<Guid, CustomTiming>
            _commands = new ConcurrentDictionary<Guid, CustomTiming>(),
            _opening = new ConcurrentDictionary<Guid, CustomTiming>(),
            _closing = new ConcurrentDictionary<Guid, CustomTiming>();

        // Until EF has a Guid on DbReader's in 2.0, we have to hackily do this
        // Tracking here: https://github.com/aspnet/EntityFramework/issues/8007
        private readonly ConcurrentDictionary<DbDataReader, CustomTiming>
            _readers = new ConcurrentDictionary<DbDataReader, CustomTiming>();

        [DiagnosticName("Microsoft.EntityFrameworkCore.BeforeExecuteCommand")]
        public void OnBeforeExecuteCommand(DbCommand command, string executeMethod, Guid instanceId, bool async)
        {
            // Available: Guid connectionId, DbCommand command, string executeMethod, Guid instanceId, long startTimestamp, bool async
            _commands[instanceId] = command.GetTiming(executeMethod + (async ? " (Async)" : null), MiniProfiler.Current);
        }

        [DiagnosticName("Microsoft.EntityFrameworkCore.AfterExecuteCommand")]
        public void OnAfterExecuteCommand(object methodResult, Guid instanceId)
        {
            // Available: Guid connectionId, DbCommand command, string executeMethod, object methodResult, Guid instanceId, long startTimestamp, long currentTimestamp, bool async
            if (!_commands.TryRemove(instanceId, out var current))
            {
                return;
            }

            // A completion for a DataReader only means we *started* getting data back, not finished.
            if (methodResult is RelationalDataReader reader)
            {
                // TODO: Switch to Guid in 2.0
                _readers[reader.DbDataReader] = current;
                current.FirstFetchCompleted();
            }
            else
            {
                current.Stop();
            }
        }

        [DiagnosticName("Microsoft.EntityFrameworkCore.CommandExecutionError")]
        public void OnCommandExecutionError(Guid instanceId)
        {
            // Available: Guid connectionId, DbCommand command, string executeMethod, Guid instanceId, long startTimestamp, long currentTimestamp, Exception exception, bool async
            if (_commands.TryRemove(instanceId, out var command))
            {
                command.Errored = true;
                command.Stop();
            }
        }

        [DiagnosticName("Microsoft.EntityFrameworkCore.DataReaderDisposing")]
        public void OnDataReaderDisposing(DbDataReader dataReader)
        {
            // Available: DbConnection connection, Guid connectionId, DbDataReader dataReader, int recordsAffected, long startTimestamp, long currentTimestamp
            // TODO: Move to a Guid after https://github.com/aspnet/EntityFramework/issues/8007
            if (_readers.TryRemove(dataReader, out var reader))
            {
                reader.Stop();
            }
        }

        // Connections
        [DiagnosticName("Microsoft.EntityFrameworkCore.ConnectionOpening")]
        public void OnConnectionOpening(Guid instanceId, bool async)
        {
            // Available: DbConnection connection, Guid connectionId, Guid instanceId, long startTimestamp, bool async
            _opening[instanceId] = MiniProfiler.Current.CustomTiming("sql",
                    async ? "Connection OpenAsync()" : "Connection Open()",
                    async ? "OpenAsync" : "Open");
        }

        [DiagnosticName("Microsoft.EntityFrameworkCore.ConnectionOpened")]
        public void OnConnectionOpened(Guid instanceId)
        {
            // Available: DbConnection connection, Guid connectionId, Guid instanceId, long startTimestamp, long currentTimestamp, bool async
            if (_opening.TryRemove(instanceId, out var openingTiming))
            {
                openingTiming.Stop();
            }
        }

        [DiagnosticName("Microsoft.EntityFrameworkCore.ConnectionClosing")]
        public void OnConnectionClosing(Guid instanceId, bool async)
        {
            // Available: DbConnection connection, Guid connectionId, Guid instanceId, long startTimestamp, bool async
            _closing[instanceId] = MiniProfiler.Current.CustomTiming("sql",
                    async ? "Connection CloseAsync()" : "Connection Close()",
                    async ? "CloseAsync" : "Close");
        }

        [DiagnosticName("Microsoft.EntityFrameworkCore.ConnectionClosed")]
        public void OnConnectionClosed(Guid instanceId)
        {
            // Available: DbConnection connection, Guid connectionId, Guid instanceId, long startTimestamp, long currentTimestamp, bool async
            if (_closing.TryRemove(instanceId, out var closingTiming))
            {
                closingTiming.Stop();
            }
        }

        [DiagnosticName("Microsoft.EntityFrameworkCore.ConnectionError")]
        public void OnConnectionError(Guid instanceId)
        {
            // Available: DbConnection connection, Guid connectionId, Exception exception, Guid instanceId, long startTimestamp, long currentTimestamp, bool async
            if (_opening.TryRemove(instanceId, out var openingTiming))
            {
                openingTiming.Errored = true;
            }
            if (_closing.TryRemove(instanceId, out var closingTiming))
            {
                closingTiming.Errored = true;
            }
        }

        // Transactions
        //[DiagnosticName("Microsoft.EntityFrameworkCore.TransactionStarted")]
        //public void OnTransactionStarted()
        //{
        //    // Available: DbConnection connection, Guid connectionId, DbTransaction transaction
        //}

        //[DiagnosticName("Microsoft.EntityFrameworkCore.TransactionCommitted")]
        //public void OnTransactionCommitted()
        //{
        //    // Avaialble: DbConnection connection, Guid connectionId, DbTransaction transaction, long startTimestamp, long currentTimestamp
        //}

        //[DiagnosticName("Microsoft.EntityFrameworkCore.TransactionRolledback")]
        //public void OnTransactionRolledback()
        //{
        //    // Available: DbConnection connection, Guid connectionId, DbTransaction transaction, long startTimestamp, long currentTimestamp
        //}

        //[DiagnosticName("Microsoft.EntityFrameworkCore.TransactionDisposed")]
        //public void OnTransactionDisposed()
        //{
        //    // Avaialble: DbConnection connection, Guid connectionId, DbTransaction transaction
        //}

        //[DiagnosticName("Microsoft.EntityFrameworkCore.TransactionError")]
        //public void OnTransactionError()
        //{
        //    // Available: DbConnection connection, Guid connectionId, DbTransaction transaction, string action, Exception exception, long startTimestamp, long currentTimestamp
        //}

        // Refer to https://github.com/aspnet/EntityFramework/blob/dev/src/EFCore.Relational/Storage/Internal/RelationalCommand.cs
        private SqlExecuteType GetSqlExecuteType(string str)
        {
            switch (str)
            {
                case "ExecuteNonQuery": return SqlExecuteType.NonQuery;
                case "ExecuteScalar": return SqlExecuteType.Scalar;
                case "ExecuteReader": return SqlExecuteType.Reader;
                default: return SqlExecuteType.None;
            }
        }
    }
}
