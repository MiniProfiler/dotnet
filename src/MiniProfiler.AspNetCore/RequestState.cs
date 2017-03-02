using System;
using System.Collections.Generic;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Stores the request state
    /// </summary>
    public class RequestState
    {
        /// <summary>
        /// Is the user authorized to see this MiniProfiler?
        /// </summary>
        public bool IsAuthroized { get; set; }

        /// <summary>
        /// Store this as a string so we generate it once
        /// </summary>
        public List<Guid> RequestIDs { get; set; }
    }
}
