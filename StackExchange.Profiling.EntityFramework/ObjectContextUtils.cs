using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Data.Common;
using System.Reflection.Emit;
using System.Data.EntityClient;
using System.Web.Configuration;
using System.Data.Metadata.Edm;


namespace StackExchange.Profiling.Data
{
    public static class ObjectContextUtils
    {
			  
			  /// <summary>
			  /// Allow caching more than one workspace, depending upon assemblies and paths specified
			  /// </summary>
			  public class MetadataCacheKey
				{
					protected Assembly[] _assemblies;
					protected string [] _paths;
					protected int _hashCode;

					/// <summary>
					/// Create a cache key with the assemblies and paths provided
					/// </summary>
					/// <param name="assemblies">Array of assemblies to search for edmx resources</param>
					/// <param name="paths">Resource paths to search inside of assemblies</param>
					public MetadataCacheKey(Assembly[] assemblies, string[] paths)
					{
						if(assemblies == null)
							throw new ArgumentNullException("assemblies");
						if(paths == null)
							throw new ArgumentNullException("paths");
						
						_assemblies = assemblies;
 						_paths = paths;
						CreateHashCode();
					}

					/// <summary>
					/// Create a cache key for one assembly and the name of the edmx.  It will create the resource paths
					/// </summary>
					/// <param name="assembly">Assembly that edmx is located in</param>
					/// <param name="edmxName">Name of edmx file</param>
					public MetadataCacheKey(Assembly assembly, string edmxName)
					{
						var assemblyName = assembly.FullName;
						_assemblies = new Assembly[] { assembly};
						_paths = new string[3];
						string pattern = "res://{0}/{1}.{2}";
						_paths[0] = string.Format(pattern, assemblyName, edmxName, "ssdl");
						_paths[1] = string.Format(pattern, assemblyName, edmxName, "msl");
						_paths[2] = string.Format(pattern, assemblyName, edmxName, "csdl");
						CreateHashCode();
					}

					private void CreateHashCode() {
						_hashCode = 19;
						foreach(var assembly in _assemblies)
							_hashCode = (3* _hashCode) ^ assembly.GetHashCode();
						foreach(var path in _paths)
							_hashCode = (3 * _hashCode) ^ path.GetHashCode();
					}

					public Assembly[] Assemblies { get { return _assemblies; }	}
					public string[] Paths { get { return _paths; } }

					public override int  GetHashCode()
					{
 						 return _hashCode;
					}

					public override bool  Equals(object obj)
					{	
						
						if(obj == null)
								return false;
						var cacheKey = obj as MetadataCacheKey;
						if(cacheKey == null)
							return false;
							if(_assemblies.Count() != cacheKey._assemblies.Count() || _paths.Count() != cacheKey._paths.Count())
								return false;
						int i = 0;
						for(i = 0; i < _assemblies.Count(); i++){
							if(_assemblies[i] != cacheKey._assemblies[i])
								return false;
						}
						for(i = 0; i < _paths.Count(); i++){
							if(_paths[i] != cacheKey._paths[i])
								return false;
						}
						return true;
					}

				}

			  
        /// <summary>
        /// static class for caching MetadataWorkspaces.  Supports multiple workspaces
        /// </summary>
			  static class MetadataCache
        {
						private static Dictionary<MetadataCacheKey, MetadataWorkspace> _workspaces;
            static MetadataCache()
            {
							_workspaces = new Dictionary<MetadataCacheKey, MetadataWorkspace>();
            }

						public static MetadataWorkspace GetWorkspace(MetadataCacheKey key)
						{
							if (_workspaces.ContainsKey(key))
								return _workspaces[key];
							_workspaces[key] = new MetadataWorkspace(key.Paths, key.Assemblies);
							return _workspaces[key];
						}
        }

        /// <summary>
        /// Method 1 for getting a profiled EF context uses reflection
        /// </summary>
        public static T CreateObjectContext<T>(this DbConnection connection) where T : System.Data.Objects.ObjectContext
        {
            return CreateObjectContext<T>(connection,new MetadataCacheKey( new Assembly[] { typeof(T).Assembly}, new string[] { "res://*/"}));
        }
        /// <summary>
        /// Another method, can pass in the edmx resource name.
        /// </summary>
				public static T CreateObjectContext<T>(this DbConnection connection, string edmxName) where T : System.Data.Objects.ObjectContext
				{
					return CreateObjectContext<T>(connection, new MetadataCacheKey(typeof(T).Assembly,edmxName));
				}
				/// <summary>
				/// Another method, can pass in resource paths to search.
				/// </summary>
				public static T CreateObjectContext<T>(this DbConnection connection, string[] paths) where T : System.Data.Objects.ObjectContext
				{
					return CreateObjectContext<T>(connection,new MetadataCacheKey( new Assembly[] { typeof(T).Assembly }, paths));
				}
			  /// <summary>
			  /// Another method, can pass in the MetadataCacheKey
			  /// </summary>
				public static T CreateObjectContext<T>(this DbConnection connection, MetadataCacheKey cacheKey) where T : System.Data.Objects.ObjectContext
				{
					var workspace = MetadataCache.GetWorkspace(cacheKey);
					var factory = DbProviderServices.GetProviderFactory(connection);

					var itemCollection = workspace.GetItemCollection(System.Data.Metadata.Edm.DataSpace.SSpace);
					itemCollection.GetType().GetField("_providerFactory", // <==== big fat ugly hack
									BindingFlags.NonPublic | BindingFlags.Instance).SetValue(itemCollection, factory);

					var ec = new System.Data.EntityClient.EntityConnection(workspace, connection);
					return CtorCache<T, System.Data.EntityClient.EntityConnection>.Ctor(ec);

				}



        /// <summary>
        /// Second method for getting an EF context, does no reflection tricks
        /// </summary>
        public static T GetProfiledContext<T>() where T : System.Data.Objects.ObjectContext
        {
            var conn = new EFProfiledDbConnection(GetStoreConnection<T>(), MiniProfiler.Current);
            return ObjectContextUtils.CreateObjectContext<T>(conn);
        }

        public static DbConnection GetStoreConnection<T>() where T : System.Data.Objects.ObjectContext
        {
            return GetStoreConnection("name=" + typeof(T).Name);
        }

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

        internal static class CtorCache<TType, TArg> where TType : class
        {
            public static readonly Func<TArg, TType> Ctor;
            static CtorCache()
            {
                Type[] argTypes = new Type[] { typeof(TArg) };
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
    }
}
