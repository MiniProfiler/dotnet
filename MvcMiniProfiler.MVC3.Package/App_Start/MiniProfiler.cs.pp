using System;
using System.Web;
using System.Web.Mvc;
using System.Linq;
using MvcMiniProfiler;
using MvcMiniProfiler.MVCHelpers;
using Microsoft.Web.Infrastructure;
using Microsoft.Web.Infrastructure.DynamicModuleHelper;
//using System.Data;
//using System.Data.Entity;
//using System.Data.Entity.Infrastructure;

[assembly: WebActivator.PreApplicationStartMethod(
    typeof($rootnamespace$.App_Start.MiniProfilerPackage), "PreStart")]

[assembly: WebActivator.PostApplicationStartMethod(
    typeof($rootnamespace$.App_Start.MiniProfilerPackage), "PostStart")]


namespace $rootnamespace$.App_Start {
    public static class MiniProfilerPackage {
        public static void PreStart() {
            MiniProfiler.Settings.SqlFormatter = new MvcMiniProfiler.SqlFormatters.SqlServerFormatter();

			//TODO: If you are using SqlServer, rather than SqlCe,
			// You'll want to change this line to use your connection string.
			// var factory = new SqlConnectionFactory("Data Source=.\SQLEXPRESS;Initial Catalog=YourDatabase;Integrated Security=True");

            //TODO: You will need the System.Data.Entity, 
            //      and System.Data.Entity.Infrastructure namspace
            //	    and to umcomment these if you are using SQLCe
            //var factory = new SqlCeConnectionFactory("System.Data.SqlServerCe.4.0");
            //var profiled = new MvcMiniProfiler.Data.ProfiledDbConnectionFactory(factory);
            //Database.DefaultConnectionFactory = profiled;
            
			//Make sure the MiniProfiler handles BeginRequest and EndRequest
			DynamicModuleUtility.RegisterModule(typeof(MiniProfilerStartupModule));
			
			//Setup profiler for Controllers via a Global ActionFilter
            GlobalFilters.Filters.Add(new ProfilingActionFilter());
        }
        
        public static void PostStart() {
            var copy = ViewEngines.Engines.ToList();
            ViewEngines.Engines.Clear();
            foreach (var item in copy) {
                ViewEngines.Engines.Add(new ProfilingViewEngine(item));
            }
        }
    }

	public class MiniProfilerStartupModule : IHttpModule {
		public void Init(HttpApplication context) {
			context.BeginRequest += (sender, e) => {
				var request = ((HttpApplication)sender).Request;
				if (request.IsLocal) { MiniProfiler.Start(); }  
			};
		
			context.EndRequest += (sender, e) => {
				MiniProfiler.Stop();  
			};
		}
 
		public void Dispose() { }
	}
}