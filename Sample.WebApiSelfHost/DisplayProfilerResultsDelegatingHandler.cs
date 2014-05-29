using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Profiling;

namespace Sample.WebApiSelfHost
{
    /// <summary>
    /// This class is only used to render the profiler results to the console for this demo, it is
    /// not needed in a normal Profiled WebApi where the results should be persisted to a DB or file.
    /// </summary>
    internal class DisplayProfilerResultsDelegatingHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var task = base.SendAsync(request, cancellationToken);
            task.Wait(cancellationToken);

            Console.WriteLine(MiniProfiler.Current.RenderPlainText());

            return task;
        }
    }
}