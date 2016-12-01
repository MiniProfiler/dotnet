using System;

namespace StackExchange.Profiling.Elasticsearch.Models
{
    class CustomTimingModel
    {
        public Guid Id { get; set; }
        public string CommandString { get; set; }
        public string ExecuteType { get; set; }
        public string StackTraceSnippet { get; set; }
        public decimal StartMilliseconds { get; set; }
        public decimal? DurationMilliseconds { get; set; }
        public decimal? FirstFetchDurationMilliseconds { get; set; }
    }
}