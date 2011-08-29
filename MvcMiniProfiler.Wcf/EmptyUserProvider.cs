using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MvcMiniProfiler.Wcf
{
    public class EmptyUserProvider : IWcfUserProvider
    {
        public string GetUser()
        {
            return "Unknown";
        }
    }
}
