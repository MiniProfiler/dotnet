using System;
using System.Collections.Generic;
using System.Data.Common;


namespace MvcMiniProfiler
{
    /// <summary>
    /// Categories of sql statements.
    /// </summary>
    public enum ExecuteType
    {
        /// <summary>
        /// Unknown
        /// </summary>
        None = 0,

        /// <summary>
        /// DML statements that alter database state, e.g. INSERT, UPDATE
        /// </summary>
        NonQuery,

        /// <summary>
        /// Statements that return a single record
        /// </summary>
        Scalar,

        /// <summary>
        /// Statements that iterate over a result set
        /// </summary>
        Reader
    }

    // TODO: refactor this out into MiniProfiler
    internal class SqlProfiler
    {
        Dictionary<Tuple<object, ExecuteType>, SqlTiming> _inProgress = new Dictionary<Tuple<object, ExecuteType>, SqlTiming>();
        Dictionary<DbDataReader, SqlTiming> _inProgressReaders = new Dictionary<DbDataReader, SqlTiming>();

        internal MiniProfiler _profiler;

        public SqlProfiler(MiniProfiler profiler)
        {
            _profiler = profiler;
        }

        public void ExecuteStartImpl(DbCommand command, ExecuteType type)
        {
            var id = Tuple.Create((object)command, type);
            var sqlTiming = new SqlTiming(command, type, _profiler);

            _inProgress[id] = sqlTiming;
        }

        public void ExecuteFinishImpl(DbCommand command, ExecuteType type, DbDataReader reader = null)
        {
            var id = Tuple.Create((object)command, type);
            var current = _inProgress[id];
            current.ExecutionComplete(isReader: reader != null);
            _inProgress.Remove(id);
            if (reader != null)
            {
                _inProgressReaders[reader] = current;
            }
        }

        public void ReaderFinishedImpl(DbDataReader reader)
        {
            var stat = _inProgressReaders[reader];
            stat.ReaderFetchComplete();
            _inProgressReaders.Remove(reader);
        }

        public List<SqlTiming> GetExecutionStats()
        {
            var result = new List<SqlTiming>();
            foreach (var t in _profiler.GetTimingHierarchy())
            {
                if (t.HasSqlTimings)
                    result.AddRange(t.SqlTimings);
            }
            return result;
        }
    }

    internal static class SqlProfilerExtensions
    {
        public static void ExecuteStart(this SqlProfiler sqlProfiler, DbCommand command, ExecuteType type)
        {
            if (sqlProfiler == null) return;
            sqlProfiler.ExecuteStartImpl(command, type);
        }

        public static void ExecuteFinish(this SqlProfiler sqlProfiler, DbCommand command, ExecuteType type, DbDataReader reader = null)
        {
            if (sqlProfiler == null) return;
            sqlProfiler.ExecuteFinishImpl(command, type, reader);
        }

        public static void ReaderFinish(this SqlProfiler sqlProfiler, DbDataReader reader)
        {
            if (sqlProfiler == null) return;
            sqlProfiler.ReaderFinishedImpl(reader);
        }

    }
}