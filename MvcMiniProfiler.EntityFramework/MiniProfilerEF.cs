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

                var profType = typeof(MvcMiniProfiler.Data.EFProfiledDbProviderFactory<>).MakeGenericType(factory.GetType());


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
}
