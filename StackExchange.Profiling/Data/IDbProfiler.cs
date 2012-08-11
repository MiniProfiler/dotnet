using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace StackExchange.Profiling.Data
{
    /// <summary>
    /// A callback for ProfiledDbConnection and family
    /// </summary>
    public interface IDbProfiler
    {
        /// <summary>
        /// Called when a command starts executing
        /// </summary>
        /// <param name="profiledDbCommand"></param>
        /// <param name="executeType"></param>
        void ExecuteStart(IDbCommand profiledDbCommand, ExecuteType executeType);
        
        /// <summary>
        /// Called when a reader finishes executing
        /// </summary>
        /// <param name="profiledDbCommand"></param>
        /// <param name="executeType"></param>
        /// <param name="reader"></param>
        void ExecuteFinish(IDbCommand profiledDbCommand, ExecuteType executeType, DbDataReader reader);

        /// <summary>
        /// Called when a reader is done iterating through the data 
        /// </summary>
        /// <param name="reader"></param>
        void ReaderFinish(IDataReader reader);

        /// <summary>
        /// Called when an error happens during execution of a command 
        /// </summary>
        /// <param name="profiledDbCommand"></param>
        /// <param name="executeType"></param>
        /// <param name="exception"></param>
        void OnError(IDbCommand profiledDbCommand, ExecuteType executeType, Exception exception);

        /// <summary>
        /// True if the profiler instance is active
        /// </summary>
        bool IsActive { get; }
    }
}
