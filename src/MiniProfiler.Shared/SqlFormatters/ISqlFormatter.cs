using System.Collections.Generic;

namespace StackExchange.Profiling.SqlFormatters
{
    /// <summary>
    /// Takes a <c>SqlTiming</c> and returns a formatted SQL string, for parameter replacement, etc.
    /// </summary>
    public interface ISqlFormatter
    {
        /// <summary>
        /// Return SQL the way you want it to look on the in the trace. Usually used to format parameters.
        /// </summary>
        /// <param name="commandText">The SQL command to format.</param>
        /// <param name="parameters">The parameters for the SQL command.</param>
        string FormatSql(string commandText, List<SqlTimingParameter> parameters);
    }
}
