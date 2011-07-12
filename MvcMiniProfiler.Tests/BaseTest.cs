using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MvcMiniProfiler.Tests
{
    public abstract class BaseTest
    {
        public IDisposable SimulateRequest(string url)
        {
            var result = new Subtext.TestLibrary.HttpSimulator();

            result.SimulateRequest(new Uri(url));

            return result;
        }
    }
}
