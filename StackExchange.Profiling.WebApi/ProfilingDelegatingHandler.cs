using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace StackExchange.Profiling.WebApi
{
    /// <summary>
    /// A <see cref="DelegatingHandler" /> which starts and stops MiniProfiler for a request.
    /// </summary>
    public sealed class ProfilingDelegatingHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            WebApiContext.Current.Request = request;

            MiniProfiler.Start();

            var task = base.SendAsync(request, cancellationToken);
            task.Wait(cancellationToken);

            MiniProfiler.Stop();

            return task;
        }
    }
}