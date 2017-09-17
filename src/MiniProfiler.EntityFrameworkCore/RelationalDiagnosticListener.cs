using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using StackExchange.Profiling.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

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

        // See https://github.com/aspnet/EntityFramework/issues/8007
        private readonly ConcurrentDictionary<Guid, CustomTiming>
            _readers = new ConcurrentDictionary<Guid, CustomTiming>();

        /// <summary>
        /// Notifies the observer that the provider has finished sending push-based notifications.
        /// </summary>
        public void OnCompleted() { }
        /// <summary>
        /// Notifies the observer that the provider has experienced an error condition.
        /// </summary>
        /// <param name="error">An object that provides additional information about the error.</param>
        public void OnError(Exception error) => Trace.WriteLine(error);

        /// <summary>
        /// Provides the observer with new data.
        /// </summary>
        /// <param name="kv">The current notification information.</param>
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
            // This isn't as trivial as it appears due to the start offset of the request
            else if (kv.Key == RelationalEventId.ConnectionOpening.Name)
            {
                var data = (ConnectionEventData)kv.Value;
                var timing = MiniProfiler.Current.CustomTiming("sql",
                    data.IsAsync ? "Connection OpenAsync()" : "Connection Open()",
                    data.IsAsync ? "OpenAsync" : "Open");
                if (timing != null)
                {
                    _opening[data.ConnectionId] = timing;
                }
            }
            else if (kv.Key == RelationalEventId.ConnectionOpened.Name)
            {
                var data = (ConnectionEndEventData)kv.Value;
                if (_opening.TryRemove(data.ConnectionId, out var openingTiming))
                {
                    openingTiming?.Stop();
                }
            }
            else if (kv.Key == RelationalEventId.ConnectionClosing.Name)
            {
                var data = (ConnectionEventData)kv.Value;
                var timing = MiniProfiler.Current.CustomTiming("sql",
                    data.IsAsync ? "Connection CloseAsync()" : "Connection Close()",
                    data.IsAsync ? "CloseAsync" : "Close");
                if (timing != null)
                {
                    _closing[data.ConnectionId] = timing;
                }
            }
            else if (kv.Key == RelationalEventId.ConnectionClosed.Name)
            {
                var data = (ConnectionEndEventData)kv.Value;
                if (_closing.TryRemove(data.ConnectionId, out var closingTiming))
                {
                    closingTiming?.Stop();
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

        // Transactions - Not in yet
        //[DiagnosticName("Microsoft.EntityFrameworkCore.TransactionStarted")]
        //public void OnTransactionStarted()
        //{
        //    // Available: DbConnection connection, Guid connectionId, DbTransaction transaction
        //}

        //[DiagnosticName("Microsoft.EntityFrameworkCore.TransactionCommitted")]
        //public void OnTransactionCommitted()
        //{
        //    // Available: DbConnection connection, Guid connectionId, DbTransaction transaction, long startTimestamp, long currentTimestamp
        //}

        //[DiagnosticName("Microsoft.EntityFrameworkCore.TransactionRolledback")]
        //public void OnTransactionRolledback()
        //{
        //    // Available: DbConnection connection, Guid connectionId, DbTransaction transaction, long startTimestamp, long currentTimestamp
        //}

        //[DiagnosticName("Microsoft.EntityFrameworkCore.TransactionDisposed")]
        //public void OnTransactionDisposed()
        //{
        //    // Available: DbConnection connection, Guid connectionId, DbTransaction transaction
        //}

        //[DiagnosticName("Microsoft.EntityFrameworkCore.TransactionError")]
        //public void OnTransactionError()
        //{
        //    // Available: DbConnection connection, Guid connectionId, DbTransaction transaction, string action, Exception exception, long startTimestamp, long currentTimestamp
        //}
    }
}
