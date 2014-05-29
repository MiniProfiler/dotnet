using System.Net.Http;
using System.Threading;

namespace StackExchange.Profiling.WebApi
{
    internal sealed class WebApiContext
    {
        private static ThreadLocal<WebApiContext> current = new ThreadLocal<WebApiContext>(() =>
        {
            return new WebApiContext();
        });

        internal static WebApiContext Current
        {
            get
            {
                return current.Value;
            }
        }

        internal HttpRequestMessage Request
        {
            get;
            set;
        }
    }
}