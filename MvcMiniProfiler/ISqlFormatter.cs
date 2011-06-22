using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MvcMiniProfiler
{
    public interface ISqlFormatter
    {
        /// <summary>
        /// Return SQL the way you want it to look on the in the trace. Usually used to format parameters 
        /// </summary>
        /// <param name="timing"></param>
        /// <returns>Formatted SQL</returns>
        string FormatSql(SqlTiming timing);
    }
}
