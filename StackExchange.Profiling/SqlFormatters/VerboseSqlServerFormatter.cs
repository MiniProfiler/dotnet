using System.Collections.Generic;
using System.Data;
using System.Text;

namespace StackExchange.Profiling.SqlFormatters
{
    /// <summary>
    /// Formats SQL server queries with a DECLARE up top for parameter values
    /// </summary>
    public class VerboseSqlServerFormatter : SqlServerFormatter
    {
        /// <summary>
        /// Formats the SQL in a SQL-Server friendly way, with DECLARE statements for the parameters up top.
        /// </summary>
        public override string FormatSql(string commandText, List<SqlTimingParameter> parameters, IDbCommand command)
        {
            StringBuilder buffer = new StringBuilder();

            if (command != null)
            {
                buffer.AppendLine("-- Command Type: " + command.CommandType);
                buffer.AppendLine("-- Database: " + command.Connection.Database);
                if (command.Transaction != null)
                {
                    buffer.AppendLine("-- Transaction Iso Level: " + command.Transaction.IsolationLevel);
                }
                buffer.AppendLine();
            }

	        string baseOutput = base.FormatSql(commandText, parameters, command);

	        buffer.Append(baseOutput);

	        return buffer.ToString();
        }
    }
}
