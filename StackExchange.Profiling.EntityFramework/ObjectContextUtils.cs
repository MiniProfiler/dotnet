namespace StackExchange.Profiling.Data
{
    using System;
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Data.EntityClient;
    using System.Data.Metadata.Edm;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Web.Configuration;

    /// <summary>
    /// The object context utils.
    /// </summary>
    public static class ObjectContextUtils
    {
        /// <summary>
        /// Allow caching more than one workspace, depending upon assemblies and paths specified
        /// </summary>
        public class MetadataCacheKey
        {
            /// <summary>
            /// The assemblies.
            /// </summary>
            private readonly Assembly[] _assemblies;

            /// <summary>
            /// The paths.
            /// </summary>
            private readonly string[] _paths;

            /// <summary>
            /// The hash code.
            /// </summary>
            private readonly int _hashCode;

            /// <summary>
            /// Initialises a new instance of the <see cref="MetadataCacheKey"/> class. 
            /// Create a cache key with the assemblies and paths provided
            /// </summary>
            /// <param name="assemblies">
            /// Array of assemblies to search for EDMX resources
            /// </param>
            /// <param name="paths">
            /// Resource paths to search inside of assemblies
            /// </param>
            public MetadataCacheKey(Assembly[] assemblies, string[] paths)
            {
                if (assemblies == null)
                    throw new ArgumentNullException("assemblies");
                if (paths == null)
                    throw new ArgumentNullException("paths");

                _assemblies = assemblies;
                _paths = paths;
                _hashCode = CreateHashCode();
            }

            /// <summary>
            /// Initialises a new instance of the <see cref="MetadataCacheKey"/> class. 
            /// Create a cache key for one assembly and the name of the EDMX.  It will create the resource paths
            /// </summary>
            /// <param name="assembly">
            /// Assembly that EDMX is located in
            /// </param>
            /// <param name="edmxName">
            /// Name of EDMX file
            /// </param>
            public MetadataCacheKey(Assembly assembly, string edmxName)
            {
                var assemblyName = assembly.FullName;
                _assemblies = new[] { assembly };
                _paths = new string[3];
                const string Pattern = "res://{0}/{1}.{2}";
                _paths[0] = string.Format(Pattern, assemblyName, edmxName, "ssdl");
                _paths[1] = string.Format(Pattern, assemblyName, edmxName, "msl");
                _paths[2] = string.Format(Pattern, assemblyName, edmxName, "csdl");
                _hashCode = CreateHashCode();
            }

            /// <summary>
            /// Gets the assemblies.
            /// </summary>
            public Assembly[] Assemblies
            {
                get
                {
                    return _assemblies;
                }
            }

            /// <summary>
            /// Gets the paths.
            /// </summary>
            public string[] Paths
            {
                get
                {
                    return _paths;
                }
            }

            /// <summary>
            /// get the hash code.
            /// </summary>
            /// <returns>the hash code value.</returns>
            public override int GetHashCode()
            {
                return _hashCode;
            }

            /// <summary>
            /// The equals.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <returns>true if the supplied instance is equal to </returns>
            public override bool Equals(object value)
            {
                if (value == null)
                    return false;
                var cacheKey = value as MetadataCacheKey;
                if (cacheKey == null)
                    return false;
                if (_assemblies.Count() != cacheKey._assemblies.Count() || _paths.Count() != cacheKey._paths.Count())
                    return false;

                for (int i = 0; i < _assemblies.Count(); i++)
                {
                    if (_assemblies[i] != cacheKey._assemblies[i])
                        return false;
                }

                for (int i = 0; i < _paths.Count(); i++)
                {
                    if (_paths[i] != cacheKey._paths[i])
                        return false;
                }

                return true;
            }

            /// <summary>
            /// create the hash code.
            /// </summary>
            private int CreateHashCode()
            {
                var hashCode = 19;
                foreach (var assembly in _assemblies)
                    hashCode = (3 * hashCode) ^ assembly.GetHashCode();
                foreach (var path in _paths)
                    hashCode = (3 * hashCode) ^ path.GetHashCode();
            }
        }

        /// <summary>
        /// Method 1 for getting a profiled EF context uses reflection
        /// </summary>
        /// <typeparam name="T">the type of object context.</typeparam>
        /// <param name="connection">The connection.</param>
        /// <returns>the object context.</returns>
        public static T CreateObjectContext<T>(this DbConnection connection) where T : System.Data.Objects.ObjectContext
        {
            return CreateObjectContext<T>(connection, new MetadataCacheKey(new Assembly[] { typeof(T).Assembly }, new string[] { "res://*/" }));
        }

        /// <summary>
        /// Another method, can pass in the EDMX resource name.
        /// </summary>
        /// <typeparam name="T">the type of object context</typeparam>
        /// <param name="connection">The connection.</param>
        /// <param name="edmxName">The EDMX Name.</param>
        /// <returns>the context</returns>
        public static T CreateObjectContext<T>(this DbConnection connection, string edmxName) where T : System.Data.Objects.ObjectContext
        {
            return CreateObjectContext<T>(connection, new MetadataCacheKey(typeof(T).Assembly, edmxName));
        }

        /// <summary>
        /// Another method, can pass in resource paths to search.
        /// </summary>
        /// <typeparam name="T">the object type.</typeparam>
        /// <param name="connection">The connection.</param>
        /// <param name="paths">The paths.</param>
        /// <returns>the object context</returns>
        public static T CreateObjectContext<T>(this DbConnection connection, string[] paths) where T : System.Data.Objects.ObjectContext
        {
            return CreateObjectContext<T>(connection, new MetadataCacheKey(new Assembly[] { typeof(T).Assembly }, paths));
        }

        /// <summary>
        /// Another method, can pass in the MetadataCacheKey
        /// </summary>
        /// <typeparam name="T">the object context.</typeparam>
        /// <param name="connection">The connection.</param>
        /// <param name="cacheKey">The cache Key.</param>
        /// <returns>the context type</returns>
        public static T CreateObjectContext<T>(this DbConnection connection, MetadataCacheKey cacheKey) where T : System.Data.Objects.ObjectContext
        {
            var workspace = MetadataCache.GetWorkspace(cacheKey);
            var factory = DbProviderServices.GetProviderFactory(connection);

            var itemCollection = workspace.GetItemCollection(DataSpace.SSpace);
            itemCollection.GetType().GetField(
                "_providerFactory", // <==== big fat ugly hack
                BindingFlags.NonPublic | BindingFlags.Instance).SetValue(itemCollection, factory);

            var ec = new EntityConnection(workspace, connection);
            return CtorCache<T, EntityConnection>.Ctor(ec);
        }

        /// <summary>
        /// Second method for getting an EF context, does no reflection tricks
        /// </summary>
        /// <typeparam name="T">the object context.</typeparam>
        /// <returns>the context</returns>
        public static T GetProfiledContext<T>() where T : System.Data.Objects.ObjectContext
        {
            var conn = new EFProfiledDbConnection(GetStoreConnection<T>(), MiniProfiler.Current);
            return ObjectContextUtils.CreateObjectContext<T>(conn);
        }

        /// <summary>
        /// The get store connection.
        /// </summary>
        /// <param name="entityConnectionString">The entity connection string.</param>
        /// <returns>the database connection</returns>
        public static DbConnection GetStoreConnection(string entityConnectionString)
        {
            // Build the initial connection string.
            var builder = new EntityConnectionStringBuilder(entityConnectionString);

            // If the initial connection string refers to an entry in the configuration, load that as the builder.
            object configName;
            if (builder.TryGetValue("name", out configName))
            {
                //  As of EF 4.1, it appears that TryGetValue("name") returns a blank
                //  string if there is no name key.  Added test to confirm that 
                //  something has been returned.
                if (!String.IsNullOrEmpty(configName.ToString()))
                {
                    var configEntry = WebConfigurationManager.ConnectionStrings[configName.ToString()];
                    builder = new EntityConnectionStringBuilder(configEntry.ConnectionString);
                }
            }

            // Find the proper factory for the underlying connection.
            var factory = DbProviderFactories.GetFactory(builder.Provider);

            // Build the new connection.
            DbConnection tempConnection = null;
            try
            {
                tempConnection = factory.CreateConnection();
                tempConnection.ConnectionString = builder.ProviderConnectionString;

                var connection = tempConnection;
                tempConnection = null;
                return connection;
            }
            finally
            {
                // If creating of the connection failed, dispose the connection.
                if (tempConnection != null)
                {
                    tempConnection.Dispose();
                }
            }
        }

        /// <summary>
        /// The get store connection.
        /// </summary>
        /// <typeparam name="T">the type of object context.</typeparam>
        /// <returns>the database connection</returns>
        internal static DbConnection GetStoreConnection<T>() where T : System.Data.Objects.ObjectContext
        {
            return GetStoreConnection("name=" + typeof(T).Name);
        }

        /// <summary>
        /// The constructor cache.
        /// </summary>
        /// <typeparam name="TType">the type of context.</typeparam>
        /// <typeparam name="TArg">the argument type.</typeparam>
        internal static class CtorCache<TType, TArg> where TType : class
        {
            /// <summary>
            /// The constructor.
            /// </summary>
            public static readonly Func<TArg, TType> Ctor;

            /// <summary>
            /// Initialises static members of the <see cref="CtorCache"/> class.
            /// </summary>
            static CtorCache()
            {
                var argTypes = new[] { typeof(TArg) };
                var ctor = typeof(TType).GetConstructor(argTypes);
                if (ctor == null)
                {
                    Ctor = x => { throw new InvalidOperationException("No suitable constructor defined"); };
                }
                else
                {
                    var dm = new DynamicMethod("ctor", typeof(TType), argTypes);
                    var il = dm.GetILGenerator();
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Newobj, ctor);
                    il.Emit(OpCodes.Ret);
                    Ctor = (Func<TArg, TType>)dm.CreateDelegate(typeof(Func<TArg, TType>));
                }
            }
        }
        
        /// <summary>
        /// static class for caching MetadataWorkspaces.  Supports multiple workspaces
        /// </summary>
        private static class MetadataCache
        {
            /// <summary>
            /// The _workspaces.
            /// </summary>
            private static readonly Dictionary<MetadataCacheKey, MetadataWorkspace> Workspaces;

            /// <summary>
            /// Initialises static members of the <see cref="MetadataCache"/> class.
            /// </summary>
            static MetadataCache()
            {
                Workspaces = new Dictionary<MetadataCacheKey, MetadataWorkspace>();
            }

            /// <summary>
            /// get the workspace.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <returns>the meta data workspace.</returns>
            public static MetadataWorkspace GetWorkspace(MetadataCacheKey key)
            {
                if (Workspaces.ContainsKey(key))
                    return Workspaces[key];
                Workspaces[key] = new MetadataWorkspace(key.Paths, key.Assemblies);
                return Workspaces[key];
            }
        }
    }
}
