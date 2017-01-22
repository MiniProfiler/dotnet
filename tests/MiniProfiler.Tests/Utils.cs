using System.Data.Common;
using System.Data.SqlServerCe;
using System.IO;

using Dapper;
using StackExchange.Profiling.Data;

namespace StackExchange.Profiling.Tests
{
    public static class Utils
    {
        private static string GetSqlCeFileNameFor<T>() => typeof(T).FullName + ".sdf";
        private static string GetSqlCeConnectionStringFor<T>() => "Data Source = " + GetSqlCeFileNameFor<T>();

        /// <summary>
        /// Creates a <c>SqlCe</c> file database named after <typeparamref name="T"/>, returning the connection string to the database.
        /// </summary>
        public static string CreateSqlCeDatabase<T>(bool deleteIfExists = false, string[] sqlToExecute = null)
        {
            var filename = GetSqlCeFileNameFor<T>();
            var connString = GetSqlCeConnectionStringFor<T>();

            if (File.Exists(filename))
            {
                if (deleteIfExists)
                {
                    File.Delete(filename);
                }
                else
                {
                    return connString;
                }
            }

            var engine = new SqlCeEngine(connString);
            engine.CreateDatabase();

            if (sqlToExecute != null)
            {
                foreach (var statement in sqlToExecute)
                {
                    using (var conn = GetOpenSqlCeConnection<T>())
                    {
                        conn.Execute(statement);
                    }
                }
            }

            return connString;
        }

        /// <summary>
        /// Returns an open connection to the <c>SqlCe</c> database identified by <typeparamref name="T"/>. This database should have been
        /// created in <see cref="CreateSqlCeDatabase{T}"/>.
        /// </summary>
        /// <typeparam name="T">the connection type</typeparam>
        /// <returns>the connection</returns>
        public static SqlCeConnection GetOpenSqlCeConnection<T>()
        {
            var result = new SqlCeConnection(GetSqlCeConnectionStringFor<T>());
            result.Open();
            return result;
        }

        /// <summary>
        /// Returns an open connection that will have its queries profiled.
        /// </summary>
        public static DbConnection GetSqliteConnection()
        {
            DbConnection cnn = new System.Data.SQLite.SQLiteConnection("Data Source=:memory:");

            // to get profiling times, we have to wrap whatever connection we're using in a ProfiledDbConnection
            // when MiniProfiler.Current is null, this connection will not record any database timings
            if (MiniProfiler.Current != null)
            {
                cnn = new ProfiledDbConnection(cnn, MiniProfiler.Current);
            }

            cnn.Open();
            return cnn;
        }
    }
}
