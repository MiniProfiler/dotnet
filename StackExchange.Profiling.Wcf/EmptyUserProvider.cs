using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StackExchange.Profiling.Wcf
{
    public class EmptyUserProvider : IWcfUserProvider
    {
        public string GetUser()
        {
            return "Unknown";
        }
    }
}
