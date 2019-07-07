using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace Samples.AspNetCore
{
    public static class Program
    {
        public static bool DisableProfilingResults { get; internal set; }

        public static void Main()
        {
            var host = WebHost.CreateDefaultBuilder()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
