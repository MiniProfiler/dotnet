namespace StackExchange.Profiling.SqlFormatters
{
    using System.Text.RegularExpressions;

    /// <summary>
    /// Formats any SQL query with inline parameters, optionally including the value type
    /// </summary>
    public class InlineFormatter : ISqlFormatter
    {
        /// <summary>
        /// The parameter prefixes.
        /// </summary>
        private static readonly Regex ParamPrefixes = new Regex(@"[@:?].+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// The include type info.
        /// </summary>
        private static bool includeTypeInfo;

        /// <summary>
        /// Initialises a new instance of the <see cref="InlineFormatter"/> class. 
        /// Creates a new Inline SQL Formatter, optionally including the parameter type info in comments beside the replaced value
        /// </summary>
        /// <param name="includeTypeInfo">whether to include a comment after the value, indicating the type<c>, e.g. /* @myParam DbType.Int32 */</c></param>
        public InlineFormatter(bool includeTypeInfo = false)
        {
            InlineFormatter.includeTypeInfo = includeTypeInfo;
        }

        /// <summary>
        /// Formats the SQL in a generic friendly format, including the parameter type information in a comment if it was specified in the InlineFormatter constructor
        /// </summary>
        /// <param name="timing">The <c>SqlTiming</c> to format</param>
        /// <returns>A formatted SQL string</returns>
        public string FormatSql(SqlTiming timing)
        {
            var sql = timing.CommandString;

            if (timing.Parameters == null || timing.Parameters.Count == 0)
            {
                return sql;
            }

            foreach (var p in timing.Parameters)
            {
                // If the parameter doesn't have a prefix (@,:,etc), append one
                var name = ParamPrefixes.IsMatch(p.Name) ? p.Name : Regex.Match(sql, "([@:?])" + p.Name, RegexOptions.IgnoreCase).Value;
                var value = GetParameterValue(p);
                sql = Regex.Replace(sql, "(" + name + ")([^0-9A-z]|$)", m => value + m.Groups[2], RegexOptions.IgnoreCase);
            }

            return sql;
        }

        /// <summary>
        /// Returns a string representation of the parameter's value, including the type
        /// </summary>
        /// <param name="p">The parameter to get a value for</param>
        /// <returns>a string containing the parameter value.</returns>
        public string GetParameterValue(SqlTimingParameter p)
        {
            // TODO: ugh, figure out how to allow different db providers to specify how values are represented (e.g. bit in oracle)
            var result = p.Value;
            var type = p.DbType ?? string.Empty;

            switch (type.ToLower())
            {
                case "string":
                case "datetime":
                    result = string.Format("'{0}'", result);
                    break;
                case "boolean":
                    switch (result)
                    {
                        case "True":
                            result = "1";
                            break;
                        case "False":
                            result = "0";
                            break;
                        default:
                            result = null;
                            break;
                    }
                    break;
            }

            if (result == null)
            {
                result = "null";
            }
            if (includeTypeInfo)
            {
                result += " /* " + p.Name + " DbType." + p.DbType + " */";
            }
            return result;
        }
    }
}
