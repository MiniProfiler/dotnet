using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace StackExchange.Profiling.SqlFormatters
{
    /// <summary>
    /// Oracle formatter for all your Oracle formatting needs 
    /// </summary>
    public class OracleFormatter : ISqlFormatter
    {        
        static readonly Dictionary<DbType, Func<SqlTimingParameter, string>> paramTranslator;

        static Func<SqlTimingParameter, string> GetWithLenFormatter(string native)
        {
            var capture = native;
            return p => {
                if (p.Size < 1) { return capture; } else { return capture + "(" + (p.Size > 8000 ? "max" : p.Size.ToString()) + ")"; }
            };
        }

        static OracleFormatter()
        {
            paramTranslator = new Dictionary<DbType, Func<SqlTimingParameter, string>>
            {
                {DbType.AnsiString, GetWithLenFormatter("varchar2")},
                {DbType.String, GetWithLenFormatter("nvarchar2")},
                {DbType.AnsiStringFixedLength, GetWithLenFormatter("char")},
                {DbType.StringFixedLength, GetWithLenFormatter("nchar")},
                {DbType.Binary, GetWithLenFormatter("raw") },
                {DbType.Byte, p => "byte"},
                {DbType.Double, p => "double"},
                {DbType.Decimal, p => "decimal"},
                {DbType.Int16,  GetWithLenFormatter("number")},
                {DbType.Int32,  GetWithLenFormatter("number")},
                {DbType.Int64,  GetWithLenFormatter("number")},                
                {DbType.DateTime, p => "date"},
                {DbType.Guid, GetWithLenFormatter("raw")},
                {DbType.Boolean, p => "char(1)"},                
                {DbType.Time, p => "TimeStamp"},                
                {DbType.Single, p => "single"},                
            };

        }        
        /// <summary>
        /// Does NOTHING, implement me!
        /// </summary>
        public string FormatSql(SqlTiming timing)
        {
            // It would be nice to have an oracle formatter, if anyone feel up to the challange a patch would be awesome
            throw new NotImplementedException();
            if (timing.Parameters == null || timing.Parameters.Count == 0)
            {
                return timing.CommandString;
            }

            StringBuilder buffer = new StringBuilder();

            buffer.Append("DECLARE ");

            bool first = true;
            foreach (var p in timing.Parameters)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    buffer.AppendLine(",").Append(new string(' ', 8));
                }

                DbType parsed;
                string resolvedType = null;
                if (!Enum.TryParse<DbType>(p.DbType, out parsed))
                {
                    resolvedType = p.DbType;
                }

                if (resolvedType == null)
                {
                    Func<SqlTimingParameter, string> translator;
                    if (paramTranslator.TryGetValue(parsed, out translator))
                    {
                        resolvedType = translator(p);
                    }
                    resolvedType = resolvedType ?? p.DbType;
                }

                var niceName = p.Name;

                buffer.Append(niceName).Append(" ").Append(resolvedType).Append(" = ").Append(PrepareValue(p));
            }

            return buffer
                .AppendLine()
                .AppendLine()
                .Append(timing.CommandString)
                .ToString();
        }
        
        static readonly string[] dontQuote = new string[] { "Int16", "Int32", "Int64", "Boolean" };
        private string PrepareValue(SqlTimingParameter p)
        {
            if (p.Value == null)
            {
                return "null";
            }

            if (dontQuote.Contains(p.DbType))
            {
                return p.Value;
            }

            return "'" + p.Value.Replace("'", "''") + "'";
         }
     }
 }

