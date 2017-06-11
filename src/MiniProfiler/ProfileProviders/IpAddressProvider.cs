using StackExchange.Profiling.Helpers;
using System.Web;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Identifies users based on ip address.
    /// </summary>
    public class IpAddressIdentity
    {
        /// <summary>
        /// Returns the paramter HttpRequest's client ip address.
        /// We combine both the REMOTE_ADDR header (which is the connecting device's IP address),
        /// plus the HTTP_X_FORWARDED_FOR header if present (which is set by some proxy
        /// servers and load balancers). This allows us to have a unique per-user view, even
        /// when behind a proxy or load balancer.
        /// </summary>
        /// <param name="request">The request to get the client IP from.</param>
        public static string GetUser(HttpRequest request)
        {
            var xff = request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            var remoteAddr = request.ServerVariables["REMOTE_ADDR"] ?? string.Empty;

            // If there's no X_FORWARDED_FOR header, just return REMOTE_ADDR
            if (xff.IsNullOrWhiteSpace())
            {
                return remoteAddr;
            }
            // Otherwise return the concatenation of the REMOTE_ADDR and the X_FORWARDED_FOR header
            return $"{remoteAddr} - {xff}";
        }
    }
}
