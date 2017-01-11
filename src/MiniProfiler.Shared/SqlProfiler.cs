using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Linq;
using StackExchange.Profiling.Data;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Contains helper code to time SQL statements.
    /// </summary>
    public class SqlProfiler
    {
        /// <summary>
        /// Returns a new <c>SqlProfiler</c> to be used in the <paramref name="profiler"/> session.
        /// </summary>
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
        public MiniProfiler Profiler { get; private set; }

        /// <summary>
        /// Tracks when 'command' is started.
        /// </summary>
        public void ExecuteStartImpl(IDbCommand command, SqlExecuteType type)
        {
            var id = Tuple.Create((object)command, type);
            var sqlTiming = new SqlTiming(command, type, Profiler);

            _inProgress[id] = sqlTiming;
        }

        /// <summary>
        /// Finishes profiling for 'command', recording durations.
        /// </summary>
        public void ExecuteFinishImpl(IDbCommand command, SqlExecuteType type, DbDataReader reader = null)
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
        public void ReaderFinishedImpl(IDataReader reader)
        {
            // this reader may have been disposed/closed by reader code, not by our using()
            if (_inProgressReaders.TryGetValue(reader, out var stat))
            {
                stat.ReaderFetchComplete();
                _inProgressReaders.TryRemove(reader, out var ignore);
            }
        }

        /// <summary>
        /// Returns all currently open commands on this connection
        /// </summary>
        public SqlTiming[] GetInProgressCommands()
        {
            return _inProgress.Values.OrderBy(x => x.StartMilliseconds).ToArray();
        }
    }

    /// <summary>
    /// Helper methods that allow operation on <c>SqlProfilers</c>, regardless of their instantiation.
    /// </summary>
    public static class SqlProfilerExtensions
    {
        /// <summary>
        /// Tracks when 'command' is started.
        /// </summary>
        public static void ExecuteStart(this SqlProfiler sqlProfiler, IDbCommand command, SqlExecuteType type)
        {
            if (sqlProfiler == null) return;
            sqlProfiler.ExecuteStartImpl(command, type);
        }

        /// <summary>
        /// Finishes profiling for 'command', recording durations.
        /// </summary>
        public static void ExecuteFinish(
            this SqlProfiler sqlProfiler,
            IDbCommand command,
            SqlExecuteType type,
            DbDataReader reader = null)
        {
            if (sqlProfiler == null) return;
            sqlProfiler.ExecuteFinishImpl(command, type, reader);
        }

        /// <summary>
        /// Called when 'reader' finishes its iterations and is closed.
        /// </summary>
        public static void ReaderFinish(this SqlProfiler sqlProfiler, IDataReader reader)
        {
            if (sqlProfiler == null) return;
            sqlProfiler.ReaderFinishedImpl(reader);
        }
    }
}