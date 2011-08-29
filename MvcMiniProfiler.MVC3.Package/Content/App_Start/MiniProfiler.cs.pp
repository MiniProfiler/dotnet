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
//using MvcMiniProfiler.Data.EntityFramework;
//using MvcMiniProfiler.Data.Linq2Sql;

[assembly: WebActivator.PreApplicationStartMethod(
	typeof($rootnamespace$.App_Start.MiniProfilerPackage), "PreStart")]

[assembly: WebActivator.PostApplicationStartMethod(
	typeof($rootnamespace$.App_Start.MiniProfilerPackage), "PostStart")]


namespace $rootnamespace$.App_Start {
	public static class MiniProfilerPackage {
		public static void PreStart() {

			// Be sure to restart you ASP.NET Developement server, this code will not run until you do that. 

			//TODO: See - _MINIPROFILER UPDATED Layout.cshtml
			//      For profiling to display in the UI you will have to include the line @MvcMiniProfiler.MiniProfiler.RenderIncludes() 
			//      in your master layout

			//TODO: Non SQL Server based installs can use other formatters like: new MvcMiniProfiler.SqlFormatters.InlineFormatter()
			MiniProfiler.Settings.SqlFormatter = new MvcMiniProfiler.SqlFormatters.SqlServerFormatter();

			//TODO: If you are using SqlServer, rather than SqlCe,
			// You'll want to change this line to use your connection string.
			// var factory = new SqlConnectionFactory("Data Source=.\SQLEXPRESS;Initial Catalog=YourDatabase;Integrated Security=True");

			//TODO: You will need the System.Data.Entity, 
			//      and System.Data.Entity.Infrastructure namspace
			//	    and to umcomment these if you are using SQLCe
            //      Make sure you pull the MiniProfiler.EF package for EF integration
			//var factory = new SqlCeConnectionFactory("System.Data.SqlServerCe.4.0");
			//var profiled = new MvcMiniProfiler.Data.ProfiledDbConnectionFactory(factory);
			//Database.DefaultConnectionFactory = profiled;
			
			//Make sure the MiniProfiler handles BeginRequest and EndRequest
			DynamicModuleUtility.RegisterModule(typeof(MiniProfilerStartupModule));
			
			//Setup profiler for Controllers via a Global ActionFilter
			GlobalFilters.Filters.Add(new ProfilingActionFilter());
		}
		
		public static void PostStart() {
			// Intercept ViewEngines to profile all partial views and regular views.
			// If you prefer to insert your profiling blocks manually you can comment this out
			var copy = ViewEngines.Engines.ToList();
			ViewEngines.Engines.Clear();
			foreach (var item in copy) {
				ViewEngines.Engines.Add(new ProfilingViewEngine(item));
			}
		}
	}

        public class MiniProfilerStartupModule : IHttpModule
        {
            public void Init(HttpApplication context)
            {
                context.BeginRequest += (sender, e) =>
                {
                    var request = ((HttpApplication)sender).Request;
                    //TODO: By default only local requests are profiled, optionally you can set it up
                    //  so authenticated users are always profiled
                    if (request.IsLocal) { MiniProfiler.Start(); }
                };


                // TODO: You can control who sees the profiling information
                /*
                context.AuthenticateRequest += (sender, e) =>
                {
                    if (!CurrentUserIsAllowedToSeeProfiler())
                    {
                        MvcMiniProfiler.MiniProfiler.Stop(discardResults: true);
                    }
                };
                */

                context.EndRequest += (sender, e) =>
                {
                    MiniProfiler.Stop();
                };
            }

            public void Dispose() { }
        }
	}
}
