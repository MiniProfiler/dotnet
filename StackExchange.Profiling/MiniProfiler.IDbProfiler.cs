using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using StackExchange.Profiling.Data;
using System.Data.Common;

namespace StackExchange.Profiling
{
    partial class MiniProfiler : IDbProfiler
    {
        /// <summary>
        /// Contains information about queries executed during this profiling session.
        /// </summary>
        internal SqlProfiler SqlProfiler { get; private set; }

        // IDbProfiler methods

        void IDbProfiler.ExecuteStart(IDbCommand profiledDbCommand, SqlExecuteType executeType)
        {
            SqlProfiler.ExecuteStart(profiledDbCommand, executeType);
        }

        void IDbProfiler.ExecuteFinish(IDbCommand profiledDbCommand, SqlExecuteType executeType, DbDataReader reader)
        {
            if (reader != null)
            {
                SqlProfiler.ExecuteFinish(profiledDbCommand, executeType, reader);
            }
            else
            {
                SqlProfiler.ExecuteFinish(profiledDbCommand, executeType);
            }
        }

        void IDbProfiler.ReaderFinish(IDataReader reader)
        {
            SqlProfiler.ReaderFinish(reader);
        }

        void IDbProfiler.OnError(IDbCommand profiledDbCommand, SqlExecuteType executeType, Exception exception)
        {
            // TODO: implement errors aggregation and presentation
        }


        bool _isActive;
        bool IDbProfiler.IsActive { get { return _isActive; } }
        internal bool IsActive
        {
            get { return _isActive; }
            set { _isActive = value; }
        }
    }
}
