namespace StackExchange.Profiling
{
    using System;
    using System.Collections.Concurrent;
    using System.Data;
    using System.Data.Common;
    using System.Linq;

    using StackExchange.Profiling.Data;

    /// <summary>
    /// Contains helper code to time SQL statements.
    /// </summary>
    public class SqlProfiler
    {
        /// <summary>
        /// The _in progress.
        /// </summary>
        private readonly ConcurrentDictionary<Tuple<object, ExecuteType>, SqlTiming> _inProgress = new ConcurrentDictionary<Tuple<object, ExecuteType>, SqlTiming>();

        /// <summary>
        /// The _in progress readers.
        /// </summary>
        private readonly ConcurrentDictionary<IDataReader, SqlTiming> _inProgressReaders = new ConcurrentDictionary<IDataReader, SqlTiming>();

        /// <summary>
        /// Initialises a new instance of the <see cref="SqlProfiler"/> class. 
        /// Returns a new <c>SqlProfiler</c> to be used in the 'profiler' session.
        /// </summary>
        /// <param name="profiler">
        /// The profiler.
        /// </param>
        public SqlProfiler(MiniProfiler profiler)
        {
            Profiler = profiler;
        }

        /// <summary>
        /// Gets the profiling session this <c>SqlProfiler</c> is part of.
        /// </summary>
        public MiniProfiler Profiler { get; private set; }

        /// <summary>
        /// Tracks when 'command' is started.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="type">The type.</param>
        public void ExecuteStartImpl(IDbCommand command, ExecuteType type)
        {
            var id = Tuple.Create((object)command, type);
            var sqlTiming = new SqlTiming(command, type, Profiler);

            _inProgress[id] = sqlTiming;
        }

        /// <summary>
        /// Returns all currently open commands on this connection
        /// </summary>
        /// <returns>the set of SQL timings.</returns>
        public SqlTiming[] GetInProgressCommands()
        {
            return _inProgress.Values.OrderBy(x => x.StartMilliseconds).ToArray();
        }

        /// <summary>
        /// Finishes profiling for 'command', recording durations.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="type">The type.</param>
        /// <param name="reader">The reader.</param>
        public void ExecuteFinishImpl(IDbCommand command, ExecuteType type, DbDataReader reader = null)
        {
            var id = Tuple.Create((object)command, type);
            var current = _inProgress[id];
            current.ExecutionComplete(reader != null);
            SqlTiming ignore;
            _inProgress.TryRemove(id, out ignore);
            if (reader != null)
            {
                _inProgressReaders[reader] = current;
            }
        }

        /// <summary>
        /// Called when 'reader' finishes its iterations and is closed.
        /// </summary>
        /// <param name="reader">The reader.</param>
        public void ReaderFinishedImpl(IDataReader reader)
        {
            SqlTiming stat;

            // this reader may have been disposed/closed by reader code, not by our using()
            if (_inProgressReaders.TryGetValue(reader, out stat))
            {
                stat.ReaderFetchComplete();
                SqlTiming ignore;
                _inProgressReaders.TryRemove(reader, out ignore);
            }
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
        /// <param name="sqlProfiler">The SQL Profiler.</param>
        /// <param name="command">The command.</param>
        /// <param name="type">The type.</param>
        public static void ExecuteStart(this SqlProfiler sqlProfiler, IDbCommand command, ExecuteType type)
        {
            if (sqlProfiler == null) return;
            sqlProfiler.ExecuteStartImpl(command, type);
        }

        /// <summary>
        /// Finishes profiling for 'command', recording durations.
        /// </summary>
        /// <param name="sqlProfiler">The SQL Profiler.</param>
        /// <param name="command">The command.</param>
        /// <param name="type">The type.</param>
        /// <param name="reader">The reader.</param>
        public static void ExecuteFinish(this SqlProfiler sqlProfiler, IDbCommand command, ExecuteType type, DbDataReader reader = null)
        {
            if (sqlProfiler == null) return;
            sqlProfiler.ExecuteFinishImpl(command, type, reader);
        }

        /// <summary>
        /// Called when 'reader' finishes its iterations and is closed.
        /// </summary>
        /// <param name="sqlProfiler">The SQL Profiler.</param>
        /// <param name="reader">The reader.</param>
        public static void ReaderFinish(this SqlProfiler sqlProfiler, IDataReader reader)
        {
            if (sqlProfiler == null) return;
            sqlProfiler.ReaderFinishedImpl(reader);
        }
    }
}