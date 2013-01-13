namespace StackExchange.Profiling.Tests.Data
{
    using System;
    using System.Data;
    using System.Diagnostics;

    using StackExchange.Profiling.Data;

    /// <summary>
    /// The counting profiler.
    /// </summary>
    public class CountingDbProfiler : IDbProfiler
    {
        /// <summary>
        /// The watch.
        /// </summary>
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
        bool IDbProfiler.IsActive
        {
            get { return true; }
        }

        /// <summary>
        /// Gets a value indicating whether complete statement measured.
        /// </summary>
        public bool CompleteStatementMeasured
        {
            get
            {
                return !this._watch.IsRunning && this._watch.ElapsedTicks > 0;
            }
        }

        /// <summary>
        /// execute the start.
        /// </summary>
        /// <param name="profiledDbCommand">The profiled DB command.</param>
        /// <param name="executeType">The execute type.</param>
        void IDbProfiler.ExecuteStart(IDbCommand profiledDbCommand, ExecuteType executeType)
        {
            this._watch.Start();
            this.ExecuteStartCount++;
            this.ErrorSql = null;
        }

        /// <summary>
        /// execute the finish.
        /// </summary>
        /// <param name="profiledDbCommand">The profiled DB command.</param>
        /// <param name="executeType">The execute type.</param>
        /// <param name="reader">The reader.</param>
        void IDbProfiler.ExecuteFinish(IDbCommand profiledDbCommand, ExecuteType executeType, System.Data.Common.DbDataReader reader)
        {
            if (reader == null)
            {
                this._watch.Stop();
            }

            this.ExecuteFinishCount++;
        }

        /// <summary>
        /// The reader finish.
        /// </summary>
        /// <param name="reader">
        /// The reader.
        /// </param>
        void IDbProfiler.ReaderFinish(IDataReader reader)
        {
            this._watch.Stop();
            this.ReaderFinishCount++;
        }

        /// <summary>
        /// on error.
        /// </summary>
        /// <param name="profiledDbCommand">
        /// The profiled DB command.
        /// </param>
        /// <param name="executeType">
        /// The execute type.
        /// </param>
        /// <param name="exception">
        /// The exception.
        /// </param>
        void IDbProfiler.OnError(IDbCommand profiledDbCommand, ExecuteType executeType, Exception exception)
        {
            this.ErrorCount++;
            this.ErrorSql = profiledDbCommand.CommandText;
        }
    }
}
