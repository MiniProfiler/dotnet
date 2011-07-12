using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MvcMiniProfiler.Data;

namespace MvcMiniProfiler
{
    partial class MiniProfiler : IDbProfiler
    {

        void IDbProfiler.ExecuteStart(System.Data.Common.DbCommand profiledDbCommand, ExecuteType executeType)
        {
            SqlProfiler.ExecuteStart(profiledDbCommand, executeType);
        }

        void IDbProfiler.ExecuteFinish(System.Data.Common.DbCommand profiledDbCommand, ExecuteType executeType, System.Data.Common.DbDataReader reader)
        {
            SqlProfiler.ExecuteFinish(profiledDbCommand, executeType, reader);
        }

        void IDbProfiler.ExecuteFinish(System.Data.Common.DbCommand profiledDbCommand, ExecuteType executeType)
        {
            SqlProfiler.ExecuteFinish(profiledDbCommand, executeType);
        }

        void IDbProfiler.ReaderFinish(System.Data.Common.DbDataReader reader)
        {
            SqlProfiler.ReaderFinish(reader);
        }
 
    }
}
