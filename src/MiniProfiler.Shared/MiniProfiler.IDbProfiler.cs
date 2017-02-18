using System;
using System.Data;
using System.Data.Common;
using StackExchange.Profiling.Data;

namespace StackExchange.Profiling
{
    public partial class MiniProfiler : IDbProfiler
    {
        /// <summary>
        /// Contains information about queries executed during this profiling session.
        /// </summary>
        internal SqlProfiler SqlProfiler { get; }

        void IDbProfiler.ExecuteStart(IDbCommand profiledDbCommand, SqlExecuteType executeType) =>
            SqlProfiler?.ExecuteStart(profiledDbCommand, executeType);

        void IDbProfiler.ExecuteFinish(IDbCommand profiledDbCommand, SqlExecuteType executeType, DbDataReader reader) =>
            SqlProfiler?.ExecuteFinish(profiledDbCommand, executeType, reader);

        void IDbProfiler.ReaderFinish(IDataReader reader) =>
            SqlProfiler?.ReaderFinish(reader);

        void IDbProfiler.OnError(IDbCommand profiledDbCommand, SqlExecuteType executeType, Exception exception)
        {
            // TODO: implement errors aggregation and presentation
        }

        bool IDbProfiler.IsActive => IsActive;
    }
}
