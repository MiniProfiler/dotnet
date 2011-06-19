using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace MvcMiniProfiler.Storage
{
    public class SqlServerStorage : DatabaseStorageBase
    {
        public SqlServerStorage(string connectionString)
            : base(connectionString)
        {
        }

        protected override System.Data.Common.DbConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }
    }
}
