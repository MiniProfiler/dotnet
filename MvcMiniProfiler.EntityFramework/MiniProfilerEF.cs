using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data;
using System.Reflection;

namespace MvcMiniProfiler
{
    public partial class MiniProfilerEF
    {
        /// <summary>
        /// Called exactly once, to setup DbProviderFactory interception, so SQL is profiled
        /// </summary>
        public static void Initialize()
        {
            InitializeDbProviderFactories(GetProfiledProviderFactoryType);
        }

        
        /// <summary>
        /// Called exactly once, to setup DbProviderFactory interception, so SQL is profiled
        /// Use this version when using EF 4.1 Update 1, EF 4.2 or later
        /// See http://weblogs.asp.net/fbouma/archive/2011/07/28/entity-framework-v4-1-update-1-the-kill-the-tool-eco-system-version.aspx?utm_source=feedburner&utm_medium=feed&utm_campaign=Feed%3A+FransBouma+%28Frans+Bouma%29
        /// </summary>
        public static void Initialize_EF42()
        {
            InitializeDbProviderFactories(GetEF42ProfiledProviderFactoryType);
        }

        private static void InitializeDbProviderFactories(Func<Type, Type> resolveProfilerTypeFunc)
        {
            try
            {
                // ensure all the factories are loaded 
                DbProviderFactories.GetFactory("...");
            }
            catch (ArgumentException)
            {
            }

            Type type = typeof(DbProviderFactories);

            DataTable table;
            object setOrTable = (type.GetField("_configTable", BindingFlags.NonPublic | BindingFlags.Static) ??
                            type.GetField("_providerTable", BindingFlags.NonPublic | BindingFlags.Static)).GetValue(null);
            if (setOrTable is DataSet)
            {
                table = ((DataSet)setOrTable).Tables["DbProviderFactories"];
            }

            table = (DataTable)setOrTable;

            foreach (DataRow row in table.Rows.Cast<DataRow>().ToList())
            {
                DbProviderFactory factory;
                try
                {
                    factory = DbProviderFactories.GetFactory(row);
                }
                catch (Exception)
                {
                    continue;
                }

                var profType = resolveProfilerTypeFunc(factory.GetType());
                if (profType != null)
                {
                    DataRow profiled = table.NewRow();
                    profiled["Name"] = row["Name"];
                    profiled["Description"] = row["Description"];
                    profiled["InvariantName"] = row["InvariantName"];
                    profiled["AssemblyQualifiedName"] = profType.AssemblyQualifiedName;
                    table.Rows.Remove(row);
                    table.Rows.Add(profiled);
                }
            }
        }

        private static Type GetProfiledProviderFactoryType(Type factoryType)
        {
            return typeof(Data.EFProfiledDbProviderFactory<>).MakeGenericType(factoryType);
        }

        private static Type GetEF42ProfiledProviderFactoryType(Type factoryType)
        {
            if (factoryType == typeof(System.Data.SqlClient.SqlClientFactory))
                return typeof(Data.EFProfiledSqlClientDbProviderFactory);
            else if (factoryType == typeof(System.Data.OleDb.OleDbFactory))
                return typeof(Data.EFProfiledOleDbProviderFactory);
            else if (factoryType == typeof(System.Data.Odbc.OdbcFactory))
                return typeof(Data.EFProfiledOdbcProviderFactory);

            return null;
        }
    }
}
