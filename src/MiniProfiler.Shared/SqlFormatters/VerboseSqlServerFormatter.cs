using System.Collections.Generic;
using System.Data;
using System.Text;

// TODO: Revisit with .NET Standard 2.0
#if NET46
using System.Transactions;
#endif

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
        public override string FormatSql(string commandText, List<SqlTimingParameter> parameters, IDbCommand command = null)
        {
            StringBuilder buffer = new StringBuilder();

            if (command != null && IncludeMetaData)
            {
                buffer.AppendLine("-- Command Type: " + command.CommandType);
                buffer.AppendLine("-- Database: " + command.Connection.Database);
#if NET46
                if (command.Transaction != null)
                {
                    buffer.AppendLine("-- Command Transaction Iso Level: " + command.Transaction.IsolationLevel);
                }
				if (Transaction.Current != null)
				{
					// transactions issued by TransactionScope are not bound to the database command but exists globally
					buffer.AppendLine("-- Transaction Scope Iso Level: " + Transaction.Current.IsolationLevel);
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
