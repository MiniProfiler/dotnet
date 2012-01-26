using System;
using StackExchange.Profiling.Data;
using System.Data.Common;
using System.Diagnostics;

namespace StackExchange.Profiling.Tests.Data
{
    class CountingDbProfiler : IDbProfiler
    {
        Stopwatch watch = new Stopwatch();

        public int ExecuteStartCount { get; set; }
        public int ExecuteFinishCount { get; set; }
        public int ReaderFinishCount { get; set; }
        public int ErrorCount { get; set; }
        public string ErrorSql { get; set; }

        public bool CompleteStatementMeasured
        {
            get
            {
                return !watch.IsRunning && watch.ElapsedTicks > 0;
            }
        }

        void IDbProfiler.ExecuteStart(DbCommand profiledDbCommand, ExecuteType executeType)
        {
            watch.Start();
            ExecuteStartCount++;
            ErrorSql = null;
        }

        void IDbProfiler.ExecuteFinish(DbCommand profiledDbCommand, ExecuteType executeType, System.Data.Common.DbDataReader reader)
        {
            if (reader == null)
            {
                watch.Stop();
            }
            ExecuteFinishCount++;
        }

        void IDbProfiler.ReaderFinish(DbDataReader reader)
        {
            watch.Stop();
            ReaderFinishCount++;
        }

        void IDbProfiler.OnError(DbCommand profiledDbCommand, ExecuteType executeType, Exception exception)
        {
            ErrorCount++;
            ErrorSql = profiledDbCommand.CommandText;
        }

        bool IDbProfiler.IsActive
        {
            get { return true; }
        }

    }
}
