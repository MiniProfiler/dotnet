namespace StackExchange.Profiling.Data
{
    using System;
    using System.Data;
    using System.Data.Common;

    /// <summary>
    /// A call back for <c>ProfiledDbConnection</c> and family
    /// </summary>
    public interface IDbProfiler
    {
        /// <summary>
        /// Gets a value indicating whether or not the profiler instance is active
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Called when a command starts executing
        /// </summary>
        /// <param name="profiledDbCommand">
        /// The profiled dB Command.
        /// </param>
        /// <param name="executeType">
        /// The execute Type.
        /// </param>
        void ExecuteStart(IDbCommand profiledDbCommand, ExecuteType executeType);

        /// <summary>
        /// Called when a reader finishes executing
        /// </summary>
        /// <param name="profiledDbCommand">The profiled DB Command.</param>
        /// <param name="executeType">The execute Type.</param>
        /// <param name="reader">The reader.</param>
        void ExecuteFinish(IDbCommand profiledDbCommand, ExecuteType executeType, DbDataReader reader);

        /// <summary>
        /// Called when a reader is done iterating through the data 
        /// </summary>
        /// <param name="reader">The reader.</param>
        void ReaderFinish(IDataReader reader);

        /// <summary>
        /// Called when an error happens during execution of a command 
        /// </summary>
        /// <param name="profiledDbCommand">The profiled DB Command.</param>
        /// <param name="executeType">The execute Type.</param>
        /// <param name="exception">The exception.</param>
        void OnError(IDbCommand profiledDbCommand, ExecuteType executeType, Exception exception);
    }
}
