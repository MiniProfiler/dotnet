using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StackExchange.Profiling.Wcf
{
    public interface IWcfUserProvider
    {
        /// <summary>
        /// Returns a string to identify the user profiling the current 'request'.
        /// </summary>
        /// <param name="request">The current HttpRequest being profiled.</param>
        string GetUser(/*HttpRequest request*/);
    }
}
