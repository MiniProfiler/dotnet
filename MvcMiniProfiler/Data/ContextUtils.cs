using System;
using System.Data.Common;
using System.Reflection;
using System.Reflection.Emit;
using MvcMiniProfiler.Data;

#if LINQ_TO_SQL
namespace System.Data.Linq
{
    public static class DataContextUtils
    {

        public static T CreateDataContext<T>(this IDbConnection connection) where T : System.Data.Linq.DataContext
        {
            return CtorCache<T, IDbConnection>.Ctor(connection);
        }
    }
}
#endif
#if ENTITY_FRAMEWORK
namespace System.Data.Objects
{
    public static class ObjectContextUtils
    {
        public static T CreateObjectContext<T>(this DbConnection connection) where T : System.Data.Objects.ObjectContext
        {
            var workspace = new System.Data.Metadata.Edm.MetadataWorkspace(
              new string[] { "res://*/" },
              new Assembly[] { Assembly.GetCallingAssembly() });

            var factory = DbProviderServices.GetProviderFactory(connection);
            var itemCollection = workspace.GetItemCollection(System.Data.Metadata.Edm.DataSpace.SSpace);
            itemCollection.GetType().GetField("_providerFactory", // <==== big fat ugly hack
                BindingFlags.NonPublic | BindingFlags.Instance).SetValue(itemCollection, factory);
            var ec = new System.Data.EntityClient.EntityConnection(workspace, connection);
            return CtorCache<T, System.Data.EntityClient.EntityConnection>.Ctor(ec);
        }
    }
}
#endif
#if LINQ_TO_SQL || ENTITY_FRAMEWORK
namespace MvcMiniProfiler.Data
{
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
#endif