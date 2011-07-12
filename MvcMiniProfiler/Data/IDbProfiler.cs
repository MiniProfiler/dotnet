using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace MvcMiniProfiler.Data
{
    interface IDbProfiler
    {
        void ExecuteStart(DbCommand profiledDbCommand, ExecuteType executeType);
        void ExecuteFinish(DbCommand profiledDbCommand, ExecuteType executeType, DbDataReader reader);
        void ExecuteFinish(DbCommand profiledDbCommand, ExecuteType executeType);
        void ReaderFinish(DbDataReader reader);
    }
}
