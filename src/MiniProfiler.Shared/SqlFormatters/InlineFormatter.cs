using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace StackExchange.Profiling.SqlFormatters
{
    /// <summary>
    /// Formats any SQL query with inline parameters, optionally including the value type
    /// </summary>
    public class InlineFormatter : ISqlFormatter
    {
        private static readonly Regex CommandSpacing = new Regex(@",([^\s])", RegexOptions.Compiled);
        private static bool includeTypeInfo;

        /// <summary>
        /// Whether to modify the output query by adding spaces after commas.
        /// </summary>
        public bool InsertSpacesAfterCommas { get; set; } = true;

        /// <summary>
        /// Creates a new <see cref="InlineFormatter"/>, optionally including the parameter type info 
        /// in comments beside the replaced value
        /// </summary>
        /// <param name="includeTypeInfo">Whether to include a comment after the value, indicating the type, e.g. <c>/* @myParam DbType.Int32 */</c></param>
        public InlineFormatter(bool includeTypeInfo = false)
        {
            InlineFormatter.includeTypeInfo = includeTypeInfo;
        }

        /// <summary>
        /// Formats the SQL in a generic friendly format, including the parameter type information 
        /// in a comment if it was specified in the InlineFormatter constructor
        /// </summary>
        /// <param name="commandText">The SQL command to format.</param>
        /// <param name="parameters">The parameters for the SQL command.</param>
        public string FormatSql(string commandText, List<SqlTimingParameter> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                return commandText;
            }

            if (InsertSpacesAfterCommas)
            {
                commandText = CommandSpacing.Replace(commandText, ", $1");
            }
            
            var paramValuesByName = new Dictionary<string, string>(parameters.Count);
            foreach (var p in parameters)
            {
                var trimmedName = p.Name.TrimStart('@', ':', '?').ToLower();
                paramValuesByName[trimmedName] = GetParameterValue(p);
            }

            var regexPattern = "[@:?](?:" + string.Join("|", paramValuesByName.Keys.Select(Regex.Escape)) + ")(?![0-9a-z])";

            return Regex.Replace(
                commandText,
                regexPattern,
                m => paramValuesByName[m.Value.Substring(1).ToLower()],
                RegexOptions.IgnoreCase
            );
        }

        /// <summary>
        /// Returns a string representation of the parameter's value, including the type
        /// </summary>
        /// <param name="param">The timing parameter to get the value for.</param>
        public string GetParameterValue(SqlTimingParameter param)
        {
            // TODO: ugh, figure out how to allow different db providers to specify how values are represented (e.g. bit in oracle)
            var result = param.Value;
            var type = param.DbType ?? string.Empty;

            if (result != null)
            {
                switch (type.ToLower())
                {
                    case "string":
                    case "datetime":
                        result = string.Format("'{0}'", result);
                        break;
                    case "boolean":
                        result = result switch
                        {
                            "True" => "1",
                            "False" => "0",
                            _ => null,
                        };
                        break;
                }
            }

            result ??= "null";
            if (includeTypeInfo)
            {
                result += " /* " + param.Name + " DbType." + param.DbType + " */";
            }
            return result;
        }
    }
}
