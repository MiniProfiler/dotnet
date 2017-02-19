using System;
using System.Data;
using System.Diagnostics;

using StackExchange.Profiling.Data;

namespace Tests.Data
{
    /// <summary>
    /// The counting profiler.
    /// </summary>
    public class CountingDbProfiler : IDbProfiler
    {
        private readonly Stopwatch _watch = new Stopwatch();

        /// <summary>
        /// Gets or sets the execute start count.
        /// </summary>
        public int ExecuteStartCount { get; set; }

        /// <summary>
        /// Gets or sets the execute finish count.
        /// </summary>
        public int ExecuteFinishCount { get; set; }

        /// <summary>
        /// Gets or sets the reader finish count.
        /// </summary>
        public int ReaderFinishCount { get; set; }

        /// <summary>
        /// Gets or sets the error count.
        /// </summary>
        public int ErrorCount { get; set; }

        /// <summary>
        /// Gets or sets the error SQL.
        /// </summary>
        public string ErrorSql { get; set; }

        /// <summary>
        /// Gets a value indicating whether is active.
        /// </summary>
        bool IDbProfiler.IsActive => true;

        /// <summary>
        /// Gets a value indicating whether complete statement measured.
        /// </summary>
        public bool CompleteStatementMeasured => !_watch.IsRunning && _watch.ElapsedTicks > 0;

        void IDbProfiler.ExecuteStart(IDbCommand profiledDbCommand, SqlExecuteType executeType)
        {
            _watch.Start();
            ExecuteStartCount++;
            ErrorSql = null;
        }

        void IDbProfiler.ExecuteFinish(IDbCommand profiledDbCommand, SqlExecuteType executeType, System.Data.Common.DbDataReader reader)
        {
            if (reader == null)
            {
                _watch.Stop();
            }

            ExecuteFinishCount++;
        }

        void IDbProfiler.ReaderFinish(IDataReader reader)
        {
            _watch.Stop();
            ReaderFinishCount++;
        }

        void IDbProfiler.OnError(IDbCommand profiledDbCommand, SqlExecuteType executeType, Exception exception)
        {
            ErrorCount++;
            ErrorSql = profiledDbCommand.CommandText;
        }
    }
}
