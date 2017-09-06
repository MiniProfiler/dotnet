using StackExchange.Profiling.Internal;
using System.Web;

namespace StackExchange.Profiling.Helpers
{
    /// <summary>
    /// Identifies users based on IP Address.
    /// </summary>
    internal static class IpAddressIdentity
    {
        /// <summary>
        /// Returns the <paramref name="request"/>'s client IP address.
        /// We combine both the REMOTE_ADDR header (which is the connecting device's IP address),
        /// plus the X-Forwarded-For header if present (which is set by some proxy
        /// servers and load balancers). This allows us to have a unique per-user view, even
        /// when behind a proxy or load balancer.
        /// </summary>
        /// <param name="request">The request to get the client IP from.</param>
        public static string GetUser(HttpRequest request)
        {
            var remoteAddr = request.ServerVariables["REMOTE_ADDR"] ?? string.Empty;
            var xff = request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            // If there's no X_FORWARDED_FOR header, just return REMOTE_ADDR
            // Otherwise return the concatenation of the REMOTE_ADDR and the X_FORWARDED_FOR header
            return xff.IsNullOrWhiteSpace() ? remoteAddr : remoteAddr + " - " + xff;
        }
    }
}
