using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace Samples.AspNetCore
{
    public class Program
    {
        public static bool DisableProfilingResults { get; internal set; }

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
