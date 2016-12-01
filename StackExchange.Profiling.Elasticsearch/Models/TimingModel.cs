using System;
using System.Collections.Generic;

namespace StackExchange.Profiling.Elasticsearch.Models
{
    class TimingModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public decimal StartMilliseconds { get; set; }
        public decimal? DurationMilliseconds { get; set; }

        public IEnumerable<TimingModel> Children { get; set; }
        public IDictionary<string, IEnumerable<CustomTimingModel>> CustomTimings { get; set; }
    }
}