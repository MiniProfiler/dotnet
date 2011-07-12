using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MvcMiniProfiler.Data;

namespace MvcMiniProfiler.Tests
{
    class TestDbProfiler : IDbProfiler
    {
        public int ExecuteStartCount { get; set; }
        public int ExecuteFinishCount { get; set; }
        public int ReaderFinishCount { get; set; }

        public void ExecuteStart(System.Data.Common.DbCommand profiledDbCommand, ExecuteType executeType)
        {
            ExecuteStartCount++;
        }

        public void ExecuteFinish(System.Data.Common.DbCommand profiledDbCommand, ExecuteType executeType, System.Data.Common.DbDataReader reader)
        {
            ExecuteFinishCount++;
        }

        public void ExecuteFinish(System.Data.Common.DbCommand profiledDbCommand, ExecuteType executeType)
        {
            ExecuteFinishCount++;
        }

        public void ReaderFinish(System.Data.Common.DbDataReader reader)
        {
            ReaderFinishCount++;
        }
    }
}
