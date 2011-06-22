using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MvcMiniProfiler.SqlFormatters
{
    public class OracleFormatter : ISqlFormatter
    {
        public string FormatSql(SqlTiming timing)
        {
            // It would be nice to have an oracle formatter, if anyone feel up to the challange a patch would be awesome
            throw new NotImplementedException();
        }
    }
}
