using System;
using System.Web.Http;
using System.Web.Http.SelfHost;
using StackExchange.Profiling;

namespace Sample.WebApiSelfHost
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var baseAddress = "http://localhost:8082";
            var config = new HttpSelfHostConfiguration(baseAddress);

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new
                {
                    id = RouteParameter.Optional
                });

            // Add the ProfilingActionFilterAttribute globally to profile all ApiController actions.
            config.Filters.Add(new StackExchange.Profiling.WebApi.ProfilingActionFilterAttribute());

            // Add the ProfilingDelegatingHandler so that MiniProfiler is started and stopped per request.
            config.MessageHandlers.Add(new StackExchange.Profiling.WebApi.ProfilingDelegatingHandler());

            // Set up MiniProfiler
            MiniProfiler.Settings.ProfilerProvider =
                new StackExchange.Profiling.WebApi.WebApiRequestProfilerProvider();

            // This is only required to render the profiler results to the console for this demo.
            config.MessageHandlers.Add(new DisplayProfilerResultsDelegatingHandler());

            using (var server = new HttpSelfHostServer(config))
            {
                server.OpenAsync().Wait();

                Console.WriteLine("Server running on {0}", baseAddress);
                Console.WriteLine("---------------------------------------");
                Console.WriteLine("Browse to {0}/api/products", baseAddress);
                Console.WriteLine("       or {0}/api/products/id", baseAddress);
                Console.WriteLine();
                Console.WriteLine("Press Enter to quit.");
                Console.ReadLine();
            }
        }
    }
}