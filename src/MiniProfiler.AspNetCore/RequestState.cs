using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Stores the request state, passed down in the <see cref="HttpContext"/>
    /// </summary>
    internal class RequestState
    {
        private const string HttpContextKey = "__MiniProfiler.RequestState";

        public void Store(HttpContext context) => context.Items[HttpContextKey] = this;
        public static RequestState Get(HttpContext context) => context.Items[HttpContextKey] as RequestState;

        /// <summary>
        /// Is the user authorized to see this MiniProfiler?
        /// </summary>
        public bool IsAuthorized { get; set; }

        /// <summary>
        /// Store this as a string so we generate it once
        /// </summary>
        public List<Guid> RequestIDs { get; set; }
    }
}
