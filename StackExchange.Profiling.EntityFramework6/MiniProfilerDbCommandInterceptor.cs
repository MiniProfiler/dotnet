using System;
using System.Data;
using System.Data.Common;
using System.Data.Entity.Infrastructure.Interception;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StackExchange.Profiling.EntityFramework6
{
    public class MiniProfilerDbCommandInterceptor : IDbCommandInterceptor
    {
        private readonly Stopwatch _stopwatch = new Stopwatch();

        protected internal Stopwatch Stopwatch
        {
            get { return _stopwatch; }
        }

        public virtual void NonQueryExecuting(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
            Executing(command, interceptionContext);
            Stopwatch.Restart();
        }

        public virtual void NonQueryExecuted(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
            Stopwatch.Stop();
            Executed(command, interceptionContext);
        }

        public virtual void ReaderExecuting(DbCommand command,
            DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
            Executing(command, interceptionContext);
            Stopwatch.Restart();
        }

        public virtual void ReaderExecuted(DbCommand command,
            DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
            Stopwatch.Stop();
            Executed(command, interceptionContext);
        }

        public virtual void ScalarExecuting(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
            Executing(command, interceptionContext);
            Stopwatch.Restart();
        }

        public virtual void ScalarExecuted(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
            Stopwatch.Stop();
            Executed(command, interceptionContext);
        }

        public virtual void Executing<TResult>(DbCommand command,
            DbCommandInterceptionContext<TResult> interceptionContext)
        {
        }

        public virtual void Executed<TResult>(DbCommand command,
            DbCommandInterceptionContext<TResult> interceptionContext)
        {
            LogResult(command, interceptionContext);
        }

        public virtual void LogResult<TResult>(DbCommand command,
            DbCommandInterceptionContext<TResult> interceptionContext)
        {
            if (MiniProfiler.Current == null || MiniProfiler.Current.Head == null)
                return;

            string commandText;

            if (interceptionContext.Exception != null)
            {
                commandText = "Error" + Environment.NewLine + Environment.NewLine + interceptionContext.Exception;
            }
            else if (interceptionContext.TaskStatus.HasFlag(TaskStatus.Canceled))
            {
                commandText = "Canceled" + Environment.NewLine;
            }
            else
            {
                string parameters = LogParameters(command, interceptionContext);
                commandText = command.CommandText + Environment.NewLine + Environment.NewLine + parameters;
            }
            MiniProfiler.Current.Head.AddCustomTiming("EF6", new CustomTiming(MiniProfiler.Current, commandText)
            {
                Id = Guid.NewGuid(),
                DurationMilliseconds = Stopwatch.ElapsedMilliseconds,
                ExecuteType = "Query"
            });
        }

        public virtual string LogParameters<TResult>(
            DbCommand command, DbCommandInterceptionContext<TResult> interceptionContext)
        {
            // -- Name: [Value] (Type = {}, Direction = {}, IsNullable = {}, Size = {}, Precision = {} Scale = {})
            var builder = new StringBuilder();

            foreach (var parameter in command.Parameters.OfType<DbParameter>())
            {

                builder.Append("-- ")
                    .Append(parameter.ParameterName)
                    .Append(": '")
                    .Append((parameter.Value == null || parameter.Value == DBNull.Value) ? "null" : parameter.Value)
                    .Append("' (Type = ")
                    .Append(parameter.DbType);

                if (parameter.Direction != ParameterDirection.Input)
                {
                    builder.Append(", Direction = ").Append(parameter.Direction);
                }

                if (!parameter.IsNullable)
                {
                    builder.Append(", IsNullable = false");
                }

                if (parameter.Size != 0)
                {
                    builder.Append(", Size = ").Append(parameter.Size);
                }

                if (((IDbDataParameter) parameter).Precision != 0)
                {
                    builder.Append(", Precision = ").Append(((IDbDataParameter) parameter).Precision);
                }

                if (((IDbDataParameter) parameter).Scale != 0)
                {
                    builder.Append(", Scale = ").Append(((IDbDataParameter) parameter).Scale);
                }

                builder.Append(")").Append(Environment.NewLine);
            }

            return builder.ToString();
        }

    }
}
