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

        /// <summary>
        /// Diagnostic Listener name to handle
        /// </summary>
        public string ListenerName => "Microsoft.EntityFrameworkCore";

        // Tracking currently open items, connections, and transactions, for logging upon their completion or error
        private readonly ConcurrentDictionary<Guid, CustomTiming>
            _commands = new ConcurrentDictionary<Guid, CustomTiming>(),
            _opening = new ConcurrentDictionary<Guid, CustomTiming>(),
            _closing = new ConcurrentDictionary<Guid, CustomTiming>();

        // Until EF has a Guid on DbReader's in 2.0, we have to hackily do this
        // Tracking here: https://github.com/aspnet/EntityFramework/issues/8007
        private readonly ConcurrentDictionary<DbDataReader, CustomTiming>
            _readers = new ConcurrentDictionary<DbDataReader, CustomTiming>();

        /// <summary>
        /// Handles BeforeExecuteCommand events. Fired just before a command is started.
        /// </summary>
        /// <param name="command">The <see cref="DbCommand"/> being executed.</param>
        /// <param name="executeMethod">The execution method of the command, e.g. "ExecuteNonQuery"</param>
        /// <param name="instanceId">The <see cref="Guid"/> identifier for this command.</param>
        /// <param name="async">Whether this command was executed asynchronously.</param>
        [DiagnosticName("Microsoft.EntityFrameworkCore.BeforeExecuteCommand")]
        public void OnBeforeExecuteCommand(DbCommand command, string executeMethod, Guid instanceId, bool async)
        {
            // Available: Guid connectionId, DbCommand command, string executeMethod, Guid instanceId, long startTimestamp, bool async
            var timing = command.GetTiming(executeMethod + (async ? " (Async)" : null), MiniProfiler.Current);
            if (timing != null)
            {
                _commands[instanceId] = timing;
            }
        }

        /// <summary>
        /// Handles AfterExecuteCommand events. Fired just after a command finishes.
        /// </summary>
        /// <param name="methodResult">The rest of the execution, e.g. the <see cref="RelationalDataReader"/>.</param>
        /// <param name="instanceId">The <see cref="Guid"/> identifier for this command.</param>
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

        /// <summary>
        /// Handles CommandExecutionError events. Fired when a command goes boom during execution.
        /// </summary>
        /// <param name="instanceId">The <see cref="Guid"/> identifier for the command that errored.</param>
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

        /// <summary>
        /// Handles DataReaderDisposing events. Fired when a <see cref="DbDataReader"/> is disposed.
        /// Usually, this is when it finishes consuming the available data.
        /// </summary>
        /// <param name="dataReader">The <see cref="DbDataReader"/> that is being disposed.</param>
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

        /// <summary>
        /// Handles ConnectionOpening events.
        /// </summary>
        /// <param name="instanceId">The <see cref="Guid"/> identifier for this *specific open*, not the connection.</param>
        /// <param name="async">Whether this connection is opening asynchronusly.</param>
        [DiagnosticName("Microsoft.EntityFrameworkCore.ConnectionOpening")]
        public void OnConnectionOpening(Guid instanceId, bool async)
        {
            // Available: DbConnection connection, Guid connectionId, Guid instanceId, long startTimestamp, bool async
            _opening[instanceId] = MiniProfiler.Current.CustomTiming("sql",
                    async ? "Connection OpenAsync()" : "Connection Open()",
                    async ? "OpenAsync" : "Open");
        }

        /// <summary>
        /// Handles ConnectionOpened events.
        /// </summary>
        /// <param name="instanceId">The <see cref="Guid"/> identifier for this *specific open*, not the connection.</param>
        [DiagnosticName("Microsoft.EntityFrameworkCore.ConnectionOpened")]
        public void OnConnectionOpened(Guid instanceId)
        {
            // Available: DbConnection connection, Guid connectionId, Guid instanceId, long startTimestamp, long currentTimestamp, bool async
            if (_opening.TryRemove(instanceId, out var openingTiming))
            {
                openingTiming.Stop();
            }
        }

        /// <summary>
        /// Handles ConnectionClosing events.
        /// </summary>
        /// <param name="instanceId">The <see cref="Guid"/> identifier for this *specific close*, not the connection.</param>
        /// <param name="async">Whether this connection is closing asynchronusly.</param>
        [DiagnosticName("Microsoft.EntityFrameworkCore.ConnectionClosing")]
        public void OnConnectionClosing(Guid instanceId, bool async)
        {
            // Available: DbConnection connection, Guid connectionId, Guid instanceId, long startTimestamp, bool async
            _closing[instanceId] = MiniProfiler.Current.CustomTiming("sql",
                    async ? "Connection CloseAsync()" : "Connection Close()",
                    async ? "CloseAsync" : "Close");
        }

        /// <summary>
        /// Handles ConnectionClosed events.
        /// </summary>
        /// <param name="instanceId">The <see cref="Guid"/> identifier for this *specific close*, not the connection.</param>
        [DiagnosticName("Microsoft.EntityFrameworkCore.ConnectionClosed")]
        public void OnConnectionClosed(Guid instanceId)
        {
            // Available: DbConnection connection, Guid connectionId, Guid instanceId, long startTimestamp, long currentTimestamp, bool async
            if (_closing.TryRemove(instanceId, out var closingTiming))
            {
                closingTiming.Stop();
            }
        }

        /// <summary>
        /// Handles ConnectionError events. Fires when a connection goes boom while opening or closing.
        /// </summary>
        /// <param name="instanceId">The <see cref="Guid"/> identifier for this *specific open or close*, not the connection.</param>
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

        // Transactions - Not in yet
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
    }
}
