using Microsoft.EntityFrameworkCore.Storage;
using StackExchange.Profiling.Internal;
using System;
using System.Collections.Concurrent;
#if NETSTANDARD2_0
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Collections.Generic;
#else
using Microsoft.Extensions.DiagnosticAdapter;
using System.Data.Common;
#endif

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
        
#if NETSTANDARD2_0
        // See https://github.com/aspnet/EntityFramework/issues/8007
        private readonly ConcurrentDictionary<Guid, CustomTiming>
            _readers = new ConcurrentDictionary<Guid, CustomTiming>();

        public void OnCompleted() { }
        public void OnError(Exception error) { }

        public void OnNext(KeyValuePair<string, object> kv)
        {
            if (kv.Key == RelationalEventId.CommandExecuting.Name)
            {
                var data = (CommandEventData)kv.Value;
                var timing = data.Command.GetTiming(data.ExecuteMethod + (data.IsAsync ? " (Async)" : null), MiniProfiler.Current);
                if (timing != null)
                {
                    _commands[data.CommandId] = timing;
                }
            }
            else if (kv.Key == RelationalEventId.CommandExecuted.Name)
            {
                var data = (CommandExecutedEventData)kv.Value;
                if (_commands.TryRemove(data.CommandId, out var current))
                {
                    // A completion for a DataReader only means we *started* getting data back, not finished.
                    if (data.Result is RelationalDataReader reader)
                    {
                        _readers[data.CommandId] = current;
                        current.FirstFetchCompleted();
                    }
                    else
                    {
                        current.Stop();
                    }
                }
            }
            else if (kv.Key == RelationalEventId.CommandError.Name)
            {
                var data = (CommandErrorEventData)kv.Value;
                if (_commands.TryRemove(data.CommandId, out var command))
                {
                    command.Errored = true;
                    command.Stop();
                }
            }
            else if (kv.Key == RelationalEventId.DataReaderDisposing.Name)
            {
                var data = (DataReaderDisposingEventData)kv.Value;
                if (_readers.TryRemove(data.CommandId, out var reader))
                {
                    reader.Stop();
                }
            }
            // TODO consider switching to ConnectionEndEventData.Duration
            // This isn't as trivia as it appears due to the start offset of the request
            else if (kv.Key == RelationalEventId.ConnectionOpening.Name)
            {
                var data = (ConnectionEventData)kv.Value;
                _opening[data.ConnectionId] = MiniProfiler.Current.CustomTiming("sql",
                    data.IsAsync ? "Connection OpenAsync()" : "Connection Open()",
                    data.IsAsync ? "OpenAsync" : "Open");
            }
            else if (kv.Key == RelationalEventId.ConnectionOpened.Name)
            {
                var data = (ConnectionEndEventData)kv.Value;
                if (_opening.TryRemove(data.ConnectionId, out var openingTiming))
                {
                    openingTiming.Stop();
                }
            }
            else if (kv.Key == RelationalEventId.ConnectionClosing.Name)
            {
                var data = (ConnectionEventData)kv.Value;
                _closing[data.ConnectionId] = MiniProfiler.Current.CustomTiming("sql",
                    data.IsAsync ? "Connection CloseAsync()" : "Connection Close()",
                    data.IsAsync ? "CloseAsync" : "Close");
            }
            else if (kv.Key == RelationalEventId.ConnectionClosed.Name)
            {
                var data = (ConnectionEndEventData)kv.Value;
                if (_closing.TryRemove(data.ConnectionId, out var closingTiming))
                {
                    closingTiming.Stop();
                }
            }
            else if (kv.Key == RelationalEventId.ConnectionError.Name)
            {
                var data = (ConnectionErrorEventData)kv.Value;
                if (_opening.TryRemove(data.ConnectionId, out var openingTiming))
                {
                    openingTiming.Errored = true;
                }
                if (_closing.TryRemove(data.ConnectionId, out var closingTiming))
                {
                    closingTiming.Errored = true;
                }
            }
        }
#else
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
#endif

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
