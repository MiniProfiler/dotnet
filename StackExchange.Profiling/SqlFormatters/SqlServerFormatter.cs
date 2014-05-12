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
        protected static readonly Dictionary<DbType, Func<SqlTimingParameter, string>> ParamTranslator;
        /// <summary>
        /// What data types should not be quoted when used in parameters
        /// </summary>
        protected static readonly string[] DontQuote = { "Int16", "Int32", "Int64", "Boolean", "Byte[]" };

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
        /// Initialises static members of the <see cref="SqlServerFormatter"/> class.
        /// </summary>
        static SqlServerFormatter()
        {   
            ParamTranslator = new Dictionary<DbType, Func<SqlTimingParameter, string>>
            {
                { DbType.AnsiString, GetWithLenFormatter("varchar") },
                { DbType.String, GetWithLenFormatter("nvarchar") },
                { DbType.AnsiStringFixedLength, GetWithLenFormatter("char") },
                { DbType.StringFixedLength, GetWithLenFormatter("nchar") },
                { DbType.Byte, p => "tinyint" },
                { DbType.Int16, p => "smallint" },
                { DbType.Int32, p => "int" },
                { DbType.Int64, p => "bigint" },
                { DbType.DateTime, p => "datetime" },
                { DbType.Guid, p => "uniqueidentifier" },
                { DbType.Boolean, p => "bit" },
                { DbType.Binary, GetWithLenFormatter("varbinary") },
            };
        }

        /// <summary>
        /// Formats the SQL in a SQL-Server friendly way, with DECLARE statements for the parameters up top.
        /// </summary>
        public virtual string FormatSql(string commandText, List<SqlTimingParameter> parameters, IDbCommand command)
        {
            StringBuilder buffer = new StringBuilder();
            
            if (parameters != null && parameters.Any())
            {
                GenerateParamText(buffer, parameters);
                // finish the parameter declaration
                buffer.Append(";")
                    .AppendLine()
                    .AppendLine();
            }

            // only treat 'StoredProcedure' differently since 'Text' may contain 'TableDirect' and 'StoredProcedure'
            if (command.CommandType == CommandType.StoredProcedure)
            {
                GenerateStoreProcedureCall(commandText, parameters, buffer);
            }
            else
            {
                buffer.Append(commandText);
            }

            string formattedText = TerminateSqlStatement(buffer.ToString());
            return formattedText;
        }

        private void GenerateStoreProcedureCall(string commandText, List<SqlTimingParameter> parameters, StringBuilder buffer)
        {
            buffer.Append("EXEC ");

            SqlTimingParameter returnValueParameter = GetReturnValueParameter(parameters);
            if (returnValueParameter != null)
            {
                buffer.Append("@").Append(returnValueParameter.Name).Append(" = ");
            }

            buffer.Append(commandText);

            GenerateStoredProcedureParameters(buffer, parameters);
            buffer.Append(";");

            if (returnValueParameter != null)
            {
                buffer.AppendLine().Append("SELECT @").Append(returnValueParameter.Name).Append(" AS ReturnValue;");
            }
        }

        private static SqlTimingParameter GetReturnValueParameter(List<SqlTimingParameter> parameters)
        {
            if (parameters == null || !parameters.Any()) return null;
            return parameters.FirstOrDefault(x => x.Direction == ParameterDirection.ReturnValue.ToString());
        }

        /// <summary>
        /// This function is necessary to always return the sql statement terminated with a semicolon.
        /// Since we're using semicolons, we should also add it to the end.
        /// </summary>
        private string TerminateSqlStatement(string sqlStatement)
        {
            if (sqlStatement[sqlStatement.Length - 1] != ';')
            {
                return sqlStatement + ";";
            }
            return sqlStatement;
        }

        private void GenerateStoredProcedureParameters(StringBuilder buffer, List<SqlTimingParameter> parameters)
        {
            if (parameters == null || !parameters.Any()) return;

            bool firstParameter = true;
            foreach (var parameter in parameters)
            {
                if (parameter.Direction == ParameterDirection.ReturnValue.ToString())
                {
                    continue;
                }
                
                if (!firstParameter)
                {
                    buffer.Append(",");
                }

                firstParameter = false;
                buffer.Append(" @").Append(parameter.Name).Append(" = ").Append("@").Append(parameter.Name);

                // Output and InputOutput directions treated equally on the database side.
                if (parameter.Direction == ParameterDirection.Output.ToString() ||
                    parameter.Direction == ParameterDirection.InputOutput.ToString())
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
        /// <returns></returns>
        protected void GenerateParamText(StringBuilder buffer, List<SqlTimingParameter> parameters)
        {
            if (parameters != null && parameters.Count > 0)
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

                    DbType parsed;
                    string resolvedType = null;
                    if (!Enum.TryParse(parameter.DbType, out parsed))
                    {
                        resolvedType = parameter.DbType;
                    }

                    if (resolvedType == null)
                    {
                        Func<SqlTimingParameter, string> translator;
                        if (ParamTranslator.TryGetValue(parsed, out translator))
                        {
                            resolvedType = translator(parameter);
                        }
                        resolvedType = resolvedType ?? parameter.DbType;
                    }

                    var niceName = parameter.Name;
                    if (!niceName.StartsWith("@"))
                    {
                        niceName = "@" + niceName;
                    }

                    buffer.Append(niceName).Append(" ").Append(resolvedType);

                    // return values don't have a value assignment
                    if (parameter.Direction != ParameterDirection.ReturnValue.ToString())
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
