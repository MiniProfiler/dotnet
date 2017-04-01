using System;
using System.Data;
using System.Data.Common;
using StackExchange.Profiling.Data;
using System.Collections.Concurrent;
using StackExchange.Profiling.Internal;

namespace StackExchange.Profiling
{
    public partial class MiniProfiler : IDbProfiler
    {
        private readonly ConcurrentDictionary<Tuple<object, SqlExecuteType>, CustomTiming> _inProgress =
            new ConcurrentDictionary<Tuple<object, SqlExecuteType>, CustomTiming>();
        private readonly ConcurrentDictionary<IDataReader, CustomTiming> _inProgressReaders =
            new ConcurrentDictionary<IDataReader, CustomTiming>();

        /// <summary>
        /// Tracks when 'command' is started.
        /// </summary>
        /// <param name="profiledDbCommand">The <see cref="IDbCommand"/> that started.</param>
        /// <param name="executeType">The execution type of the <paramref name="profiledDbCommand"/>.</param>
        void IDbProfiler.ExecuteStart(IDbCommand profiledDbCommand, SqlExecuteType executeType)
        {
            var id = Tuple.Create((object)profiledDbCommand, executeType);
            _inProgress[id] = profiledDbCommand.GetTiming(executeType.ToString(), this);
        }

        /// <summary>
        /// Finishes profiling for 'command', recording durations.
        /// </summary>
        /// <param name="profiledDbCommand">The <see cref="IDbCommand"/> that finished.</param>
        /// <param name="executeType">The execution type of the <paramref name="profiledDbCommand"/>.</param>
        /// <param name="reader">(Optional) the reader piece of the <paramref name="profiledDbCommand"/>, if it exists.</param>
        void IDbProfiler.ExecuteFinish(IDbCommand profiledDbCommand, SqlExecuteType executeType, DbDataReader reader)
        {
            var id = Tuple.Create((object)profiledDbCommand, executeType);
            if (_inProgress.TryRemove(id, out var current))
            {
                if (reader != null)
                {
                    _inProgressReaders[reader] = current;
                    current.FirstFetchCompleted();
                }
                else
                {
                    current.Stop();
                }
            }
        }

        /// <summary>
        /// Called when 'reader' finishes its iterations and is closed.
        /// </summary>
        /// <param name="reader">The <see cref="IDataReader"/> that finished.</param>
        void IDbProfiler.ReaderFinish(IDataReader reader)
        {
            // this reader may have been disposed/closed by reader code, not by our using()
            if (_inProgressReaders.TryRemove(reader, out var stat))
            {
                stat.Stop();
            }
        }

        /// <summary>
        /// Called when a command errors.
        /// </summary>
        /// <param name="profiledDbCommand">The <see cref="IDbCommand"/> that finished.</param>
        /// <param name="executeType">The execution type of the <paramref name="profiledDbCommand"/>.</param>
        /// <param name="exception">The exception thrown.</param>
        void IDbProfiler.OnError(IDbCommand profiledDbCommand, SqlExecuteType executeType, Exception exception)
        {
            var id = Tuple.Create((object)profiledDbCommand, executeType);
            if (_inProgress.TryRemove(id, out var command))
            {
                command.Errored = true;
                command.Stop();
            }
        }

        bool IDbProfiler.IsActive => IsActive;
    }
}
