namespace StackExchange.Profiling
{
    using System;
    using System.Data;
    using System.Data.Common;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Security;

    /// <summary>
    /// The mini profiler for entity framework.
    /// </summary>
    public partial class MiniProfilerEF
    {
        /// <summary>
        /// Called exactly once, to setup <c>DbProviderFactory</c> interception, so SQL is profiled
        /// </summary>
        /// <param name="supportExplicitConnectionStrings">
        /// Temporary API. Related to the EF 4.1 hack, set this to false if you are wishing
        /// to profile SQLCE and are not using any explicit connection strings for EF. Otherwise, leave this set to true (default)
        /// </param>
        public static void Initialize(bool supportExplicitConnectionStrings = true)
        {
            Initialize(false, supportExplicitConnectionStrings);
        }

        /// <summary>
        /// Called exactly once, to setup DbProviderFactory interception, so SQL is profiled
        /// Use this version when using EF 4.1 Update 1, EF 4.2 or later
        /// See http://weblogs.asp.net/fbouma/archive/2011/07/28/entity-framework-v4-1-update-1-the-kill-the-tool-eco-system-version.aspx?utm_source=feedburner&utm_medium=feed&utm_campaign=Feed%3A+FransBouma+%28Frans+Bouma%29
        /// </summary>
        /// <param name="supportExplicitConnectionStrings">
        /// Temporary API. Related to the EF 4.1 hack, set this to false if you are wishing
        /// to profile SqlCE and are not using any explicit connection strings for EF. Otherwise, leave this set to true (default)
        /// </param>
        public static void InitializeEF42(bool supportExplicitConnectionStrings = true)
        {
            Initialize(true, supportExplicitConnectionStrings);
        }

        /// <summary>
        /// The initialize.
        /// </summary>
        /// <param name="applyEFHack">
        /// apply the EF hack.
        /// </param>
        /// <param name="supportExplicitConnectionStrings">
        /// The support explicit connection strings.
        /// </param>
        private static void Initialize(bool applyEFHack, bool supportExplicitConnectionStrings)
        {
            if (supportExplicitConnectionStrings && (applyEFHack || IsEF41HackRequired()))
            {
                Data.EFProviderUtilities.UseEF41Hack();
            }

            InitializeDbProviderFactories();
        }

        /// <summary>
        /// The initialize database provider factories.
        /// </summary>
        private static void InitializeDbProviderFactories()
        {
            try
            {
                // ensure all the factories are loaded 
                DbProviderFactories.GetFactoryClasses();
            }
            catch (ArgumentException)
            {
            }

            Type type = typeof(DbProviderFactories);

            DataTable table = null;
            object setOrTable = (type.GetField("_configTable", BindingFlags.NonPublic | BindingFlags.Static) ??
                            type.GetField("_providerTable", BindingFlags.NonPublic | BindingFlags.Static)).GetValue(null);

            var set = setOrTable as DataSet;
            if (set != null)
                table = set.Tables["DbProviderFactories"];

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

                var profType = Data.EFProviderUtilities.ResolveFactoryType(factory.GetType());
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

        /// <summary>
        /// Returns true if the EF version is between 4.1.10331.0 and 4.2
        /// </summary>
        /// <returns>whether or not the hack is required</returns>
        private static bool IsEF41HackRequired()
        {
            try
            {
                var assembly = typeof(System.Data.Entity.DbContext).Assembly;
                FileVersionInfo fileVersion = FileVersionInfo.GetVersionInfo(assembly.Location);
                if (fileVersion.FileMajorPart == 4
                    && fileVersion.FileMinorPart == 1
                    && fileVersion.FileBuildPart >= 10331)
                {
                    return true;
                }
            }
            catch (SecurityException)
            {
                // As this method requires full trust
                throw new ApplicationException("Could not read file version number of apply EF41 hack. Please try by calling Initialize_EF42() explicitly");
            }

            return false;
        }
    }
}
