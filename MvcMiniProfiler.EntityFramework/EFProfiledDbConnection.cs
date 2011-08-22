using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MvcMiniProfiler.Data;
using System.Data.Common;
using System.Reflection;

namespace MvcMiniProfiler.Data
{
    public class EFProfiledDbConnection : ProfiledDbConnection
    {

        private DbProviderFactory _factory;
        private static readonly Func<DbConnection, DbProviderFactory> ripInnerProvider =
              (Func<DbConnection, DbProviderFactory>)Delegate.CreateDelegate(typeof(Func<DbConnection, DbProviderFactory>),
              typeof(DbConnection).GetProperty("DbProviderFactory", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
              .GetGetMethod(true));


        public EFProfiledDbConnection(DbConnection connection, IDbProfiler profiler) : base(connection, profiler)
        {
        
        }


        protected override DbProviderFactory DbProviderFactory
        {
            get
            {
                if (_factory != null) return _factory;
                DbProviderFactory tail = ripInnerProvider(_conn);
                _factory = DbProviderFactories.GetFactory("MvcMiniProfiler.Data.ProfiledDbProvider");
                ((ProfiledDbProviderFactory)_factory).InitProfiledDbProviderFactory(_profiler, tail);
                return _factory;
            }
        }



        protected override void Dispose(bool disposing)
        {
            _factory = null;
            base.Dispose(disposing);
        }

    }
}
