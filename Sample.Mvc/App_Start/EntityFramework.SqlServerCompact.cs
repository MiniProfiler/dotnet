using System.Data.Entity;
using System.Data.Entity.Infrastructure;

[assembly: WebActivator.PreApplicationStartMethod(typeof(SampleWeb.App_Start.EntityFrameworkSqlServerCompact), "Start")]

namespace SampleWeb.App_Start 
{
    /// <summary>
    /// web server activation point where the connection factory is configured.
    /// </summary>
    public static class EntityFrameworkSqlServerCompact 
    {
        /// <summary>
        /// set the default connection factory.
        /// </summary>
        public static void Start() 
        {
            Database.DefaultConnectionFactory = new SqlCeConnectionFactory("System.Data.SqlServerCe.4.0");
        }
    }
}
