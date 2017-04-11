using System.Collections.Generic;
using System.Data;
using System.Text;

namespace StackExchange.Profiling.SqlFormatters
{
    /// <summary>
    /// Formats SQL server queries with a DECLARE up top for parameter values
    /// </summary>
    public class VerboseSqlServerFormatter : SqlServerFormatter, IAdvancedSqlFormatter
    {
        /// <summary>
        /// Should meta data relating to the command type, database and transaction be included in sql output
        /// </summary>
        public bool IncludeMetaData { get; set; }

        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="includeMetaData"></param>
        public VerboseSqlServerFormatter(bool includeMetaData = false)
        {
            IncludeMetaData = includeMetaData;
        }

        /// <summary>
        /// Formats the SQL in a SQL-Server friendly way, with DECLARE statements for the parameters up top.
        /// </summary>
        /// <param name="commandText">The SQL command to format.</param>
        /// <param name="parameters">The parameters for the SQL command.</param>
        /// <param name="command">The <see cref="IDbCommand"/> being represented.</param>
        public override string FormatSql(string commandText, List<SqlTimingParameter> parameters, IDbCommand command = null)
        {
            var buffer = new StringBuilder();

            if (command != null && IncludeMetaData)
            {
                buffer.AppendLine("-- Command Type: " + command.CommandType);
                buffer.AppendLine("-- Database: " + command.Connection.Database);
#if !NETSTANDARD  // TODO: Revisit with .NET Standard 2.0
                if (command.Transaction != null)
                {
                    buffer.AppendLine("-- Command Transaction Iso Level: " + command.Transaction.IsolationLevel);
                }
				if (System.Transactions.Transaction.Current != null)
				{
					// transactions issued by TransactionScope are not bound to the database command but exists globally
					buffer.AppendLine("-- Transaction Scope Iso Level: " + System.Transactions.Transaction.Current.IsolationLevel);
				}
#endif
                buffer.AppendLine();
            }

	        string baseOutput = base.FormatSql(commandText, parameters, command);

	        buffer.Append(baseOutput);

	        return buffer.ToString();
        }
    }
}
