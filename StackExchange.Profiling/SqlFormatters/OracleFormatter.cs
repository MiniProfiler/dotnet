namespace StackExchange.Profiling.SqlFormatters
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.Linq;

    /// <summary>
    /// Oracle formatter for all your Oracle formatting needs 
    /// </summary>
    public class OracleFormatter : ISqlFormatter
    {
        /// <summary>
        /// The parameter translator.
        /// </summary>
        private static readonly Dictionary<DbType, Func<SqlTimingParameter, string>> ParamTranslator;

        /// <summary>
        /// don't quote.
        /// </summary>
        private static readonly string[] DontQuote = new[] { "Int16", "Int32", "Int64", "Boolean" };

        /// <summary>
        /// The get with len formatter.
        /// </summary>
        /// <param name="native">The native string</param>
        /// <returns>the SQL timing function</returns>
        private static Func<SqlTimingParameter, string> GetWithLenFormatter(string native)
        {
            var capture = native;
            return p =>
                {
                    if (p.Size < 1)
                    {
                        return capture;
                    }
                    return capture + "(" + (p.Size > 8000 ? "max" : p.Size.ToString(CultureInfo.InvariantCulture)) + ")";
                };
        }

        /// <summary>
        /// Initialises static members of the <see cref="OracleFormatter"/> class.
        /// </summary>
        static OracleFormatter()
        {
            ParamTranslator = new Dictionary<DbType, Func<SqlTimingParameter, string>>
            {
                { DbType.AnsiString, GetWithLenFormatter("varchar2") },
                { DbType.String, GetWithLenFormatter("nvarchar2") },
                { DbType.AnsiStringFixedLength, GetWithLenFormatter("char") },
                { DbType.StringFixedLength, GetWithLenFormatter("nchar") },
                { DbType.Binary, GetWithLenFormatter("raw") },
                { DbType.Byte, p => "byte" },
                { DbType.Double, p => "double" },
                { DbType.Decimal, p => "decimal" },
                { DbType.Int16,  GetWithLenFormatter("number") },
                { DbType.Int32,  GetWithLenFormatter("number") },
                { DbType.Int64,  GetWithLenFormatter("number") },                
                { DbType.DateTime, p => "date" },
                { DbType.Guid, GetWithLenFormatter("raw") },
                { DbType.Boolean, p => "char(1)" },                
                { DbType.Time, p => "TimeStamp" },                
                { DbType.Single, p => "single" },                
            };

        }

        /// <summary>
        /// unimplemented at the moment TODO: Oracle Formatter.
        /// </summary>
        /// <param name="timing">
        /// The timing.
        /// </param>
        /// <returns>the formatted SQL</returns>
        public string FormatSql(SqlTiming timing)
        {
            // It would be nice to have an oracle formatter, if anyone feel up to the challange a patch would be awesome
            throw new NotImplementedException();
        }

        /// <summary>
        /// prepare the value.
        /// </summary>
        /// <param name="p">The p.</param>
        /// <returns>the prepared string.</returns>
        private string PrepareValue(SqlTimingParameter p)
        {
            if (p.Value == null)
            {
                return "null";
            }

            if (DontQuote.Contains(p.DbType))
            {
                return p.Value;
            }

            return "'" + p.Value.Replace("'", "''") + "'";
         }
     }
 }

