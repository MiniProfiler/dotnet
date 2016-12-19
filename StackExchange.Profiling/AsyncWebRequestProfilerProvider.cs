using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace StackExchange.Profiling
{
    public class AsyncWebRequestProfilerProvider : WebRequestProfilerProvider
    {
        protected override MiniProfiler Current
        {
            get
            {
                return _currentProfiler.Value;
            }
            set
            {
                _currentProfiler.Value = value;
            }
        }

        protected override Timing CurrentHead {
            get
            {
                return _currentTiming.Value;
            }
            set
            {
                _currentTiming.Value = value;
            }
        }

        private AsyncLocal<MiniProfiler> _currentProfiler = new AsyncLocal<MiniProfiler>();
        private AsyncLocal<Timing> _currentTiming = new AsyncLocal<Timing>();
    }
}
