using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using StackExchange.Profiling.Data;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Contains helper code to time SQL statements.
    /// </summary>
    internal class SqlProfiler
    {
        /// <summary>
        /// Returns a new <c>SqlProfiler</c> to be used in the <paramref name="profiler"/> session.
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> this SQL timing halper is for.</param>
        public SqlProfiler(MiniProfiler profiler)
        {
            Profiler = profiler;
        }

        private readonly ConcurrentDictionary<Tuple<object, SqlExecuteType>, SqlTiming> _inProgress =
            new ConcurrentDictionary<Tuple<object, SqlExecuteType>, SqlTiming>();

        private readonly ConcurrentDictionary<IDataReader, SqlTiming> _inProgressReaders =
            new ConcurrentDictionary<IDataReader, SqlTiming>();

        /// <summary>
        /// Gets the profiling session this <c>SqlProfiler</c> is part of.
        /// </summary>
        public MiniProfiler Profiler { get; }

        /// <summary>
        /// Tracks when 'command' is started.
        /// </summary>
        /// <param name="command">The <see cref="IDbCommand"/> that started.</param>
        /// <param name="type">The execution type of the <paramref name="command"/>.</param>
        public void ExecuteStart(IDbCommand command, SqlExecuteType type)
        {
            var id = Tuple.Create((object)command, type);
            _inProgress[id] = new SqlTiming(command, type, Profiler);
        }

        /// <summary>
        /// Finishes profiling for 'command', recording durations.
        /// </summary>
        /// <param name="command">The <see cref="IDbCommand"/> that finished.</param>
        /// <param name="type">The execution type of the <paramref name="command"/>.</param>
        /// <param name="reader">(Optional) the reader piece of the <paramref name="command"/>, if it exists.</param>
        public void ExecuteFinish(IDbCommand command, SqlExecuteType type, DbDataReader reader = null)
        {
            var id = Tuple.Create((object)command, type);
            var current = _inProgress[id];
            current.ExecutionComplete(reader != null);
            _inProgress.TryRemove(id, out var ignore);
            if (reader != null)
            {
                _inProgressReaders[reader] = current;
            }
        }

        /// <summary>
        /// Called when 'reader' finishes its iterations and is closed.
        /// </summary>
        /// <param name="reader">The <see cref="IDataReader"/> that finished.</param>
        public void ReaderFinish(IDataReader reader)
        {
            // this reader may have been disposed/closed by reader code, not by our using()
            if (_inProgressReaders.TryGetValue(reader, out var stat))
            {
                stat.ReaderFetchComplete();
                _inProgressReaders.TryRemove(reader, out var ignore);
            }
        }
    }
}