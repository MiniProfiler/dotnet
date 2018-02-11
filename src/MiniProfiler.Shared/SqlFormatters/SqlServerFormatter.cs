using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;

namespace StackExchange.Profiling.SqlFormatters
{
    /// <summary>
    /// Formats SQL server queries with a DECLARE up top for parameter values
    /// </summary>
    public class SqlServerFormatter : ISqlFormatter
    {
        /// <summary>
        /// Lookup a function for translating a parameter by parameter type
        /// </summary>
        protected static readonly Dictionary<DbType, Func<SqlTimingParameter, string>> ParamTranslator = new Dictionary<DbType, Func<SqlTimingParameter, string>>
        {
            [DbType.AnsiString] = GetWithLenFormatter("varchar"),
            [DbType.String] = GetWithLenFormatter("nvarchar"),
            [DbType.AnsiStringFixedLength] = GetWithLenFormatter("char"),
            [DbType.StringFixedLength] = GetWithLenFormatter("nchar"),
            [DbType.Byte] = _ => "tinyint",
            [DbType.Int16] = _ => "smallint",
            [DbType.Int32] = _ => "int",
            [DbType.Int64] = _ => "bigint",
            [DbType.DateTime] = _ => "datetime",
            [DbType.Guid] = _ => "uniqueidentifier",
            [DbType.Boolean] = _ => "bit",
            [DbType.Binary] = GetWithLenFormatter("varbinary"),
        };

        /// <summary>
        /// What data types should not be quoted when used in parameters
        /// </summary>
        protected static readonly string[] DontQuote = { "Int16", "Int32", "Int64", "Boolean", "Byte[]" };

        private static Func<SqlTimingParameter, string> GetWithLenFormatter(string native)
        {
            var capture = native;
            return p =>
                {
                    if (p.Size < 0)
                        return capture + "(max)";
                    if (p.Size == 0)
                        return capture;
                    return capture + "(" + (p.Size > 8000 ? "max" : p.Size.ToString(CultureInfo.InvariantCulture)) + ")";
                };
        }

        /// <summary>
        /// Formats the SQL in a SQL-Server friendly way, with DECLARE statements for the parameters up top.
        /// </summary>
        /// <param name="commandText">The SQL command to format.</param>
        /// <param name="parameters">The parameters for the SQL command.</param>
        public virtual string FormatSql(string commandText, List<SqlTimingParameter> parameters)
        {
            return FormatSql(commandText, parameters, null);
        }

        /// <summary>
        /// Formats the SQL in a SQL-Server friendly way, with DECLARE statements for the parameters up top.
        /// </summary>
        /// <param name="commandText">The SQL command to format.</param>
        /// <param name="parameters">The parameters for the SQL command.</param>
        /// <param name="command">The <see cref="IDbCommand"/> being represented.</param>
        public virtual string FormatSql(string commandText, List<SqlTimingParameter> parameters, IDbCommand command)
        {
            var buffer = new StringBuilder();

            if (parameters?.Count > 0)
            {
                GenerateParamText(buffer, parameters);
                // finish the parameter declaration
                buffer.Append(';')
                    .AppendLine()
                    .AppendLine();
            }

            // only treat 'StoredProcedure' differently since 'Text' may contain 'TableDirect' or 'StoredProcedure'
            if (command?.CommandType == CommandType.StoredProcedure)
            {
                GenerateStoreProcedureCall(commandText, parameters, buffer);
            }
            else
            {
                buffer.Append(commandText);
            }

            TerminateSqlStatement(buffer);
            return buffer.ToString();
        }

        private string EnsureParameterPrefix(string name) =>
            !name.StartsWith("@", StringComparison.Ordinal) ? "@" + name : name;

        private string RemoveParameterPrefix(string name) =>
            name.StartsWith("@", StringComparison.Ordinal) ? name.Substring(1) : name;

        private void GenerateStoreProcedureCall(string commandText, List<SqlTimingParameter> parameters, StringBuilder buffer)
        {
            buffer.Append("EXEC ");

            SqlTimingParameter returnValueParameter = GetReturnValueParameter(parameters);
            if (returnValueParameter != null)
            {
                buffer.Append(EnsureParameterPrefix(returnValueParameter.Name)).Append(" = ");
            }

            buffer.Append(commandText);

            GenerateStoredProcedureParameters(buffer, parameters);
            buffer.Append(';');

	        GenerateSelectStatement(buffer, parameters);
        }

	    private void GenerateSelectStatement(StringBuilder buffer, List<SqlTimingParameter> parameters)
	    {
		    if (parameters == null) return;

		    var parametersToSelect = parameters.Where(
			    x => x.Direction == nameof(ParameterDirection.InputOutput)
                  || x.Direction == nameof(ParameterDirection.Output))
					 .Select(x => EnsureParameterPrefix(x.Name) + " AS " + RemoveParameterPrefix(x.Name))
					 .ToList();

		    var returnValueParameter = parameters.SingleOrDefault(x => x.Direction == nameof(ParameterDirection.ReturnValue));
			if (returnValueParameter != null)
			{
				parametersToSelect.Insert(0, EnsureParameterPrefix(returnValueParameter.Name) + " AS ReturnValue");
			}

		    if (parametersToSelect.Count == 0) return;

			buffer.AppendLine().Append("SELECT ").Append(string.Join(", ", parametersToSelect)).Append(';');
	    }

	    private static SqlTimingParameter GetReturnValueParameter(List<SqlTimingParameter> parameters)
        {
            if (parameters == null || parameters.Count == 0) return null;
            return parameters.Find(x => x.Direction == nameof(ParameterDirection.ReturnValue));
        }

        /// <summary>
        /// This function is necessary to always return the sql statement terminated with a semicolon.
        /// Since we're using semicolons, we should also add it to the end.
        /// </summary>
        /// <param name="sqlStatement">The SQL statement to terminate if necessary</param>
        private void TerminateSqlStatement(StringBuilder sqlStatement)
        {
            if (sqlStatement[sqlStatement.Length - 1] != ';')
            {
                sqlStatement.Append(';');
            }
        }

        private void GenerateStoredProcedureParameters(StringBuilder buffer, List<SqlTimingParameter> parameters)
        {
            if (parameters == null || parameters.Count == 0) return;

            bool firstParameter = true;
            foreach (var parameter in parameters)
            {
                if (parameter.Direction == nameof(ParameterDirection.ReturnValue))
                {
                    continue;
                }

                if (!firstParameter)
                {
                    buffer.Append(',');
                }

                firstParameter = false;
                buffer.Append(' ').Append(EnsureParameterPrefix(parameter.Name)).Append(" = ").Append(EnsureParameterPrefix(parameter.Name));

                // Output and InputOutput directions treated equally on the database side.
                if (parameter.Direction == nameof(ParameterDirection.Output)
                 || parameter.Direction == nameof(ParameterDirection.InputOutput))
                {
                    buffer.Append(" OUTPUT");
                }
            }
        }

        /// <summary>
        /// Generate formatter output text for all <paramref name="parameters"/>.
        /// </summary>
        /// <param name="buffer"><see cref="StringBuilder"/> to use</param>
        /// <param name="parameters">Parameters to evaluate</param>
        protected void GenerateParamText(StringBuilder buffer, List<SqlTimingParameter> parameters)
        {
            if (parameters?.Count > 0)
            {
                buffer.Append("DECLARE ");
                var first = true;

                foreach (var parameter in parameters)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        buffer.AppendLine(",").Append(new string(' ', 8));
                    }

                    string resolvedType = null;
                    if (!Enum.TryParse(parameter.DbType, out DbType parsed))
                    {
                        resolvedType = parameter.DbType;
                    }

                    if (resolvedType == null)
                    {
                        if (ParamTranslator.TryGetValue(parsed, out var translator))
                        {
                            resolvedType = translator(parameter);
                        }
                        resolvedType = resolvedType ?? parameter.DbType;
                    }

                    var niceName = EnsureParameterPrefix(parameter.Name);

                    buffer.Append(niceName).Append(' ').Append(resolvedType);

                    // return values don't have a value assignment
                    if (parameter.Direction != nameof(ParameterDirection.ReturnValue))
                    {
                        buffer.Append(" = ").Append(PrepareValue(parameter));
                    }
                }
            }
        }

        /// <summary>
        /// Prepare the parameter value for use in SqlFormatter output
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        protected string PrepareValue(SqlTimingParameter parameter)
        {
            if (parameter.Value == null)
            {
                return "null";
            }

            if (DontQuote.Contains(parameter.DbType))
            {
                if (parameter.DbType == "Boolean")
                {
                    return parameter.Value == "True" ? "1" : "0";
                }

                return parameter.Value;
            }

            var prefix = string.Empty;
            if (parameter.DbType == "String" || parameter.DbType == "StringFixedLength")
            {
                prefix = "N";
            }

            return prefix + "'" + parameter.Value.Replace("'", "''") + "'";
        }
    }
}
