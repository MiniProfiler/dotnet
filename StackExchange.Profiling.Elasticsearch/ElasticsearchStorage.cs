using System;
using System.Collections.Generic;
using System.Linq;
using Elasticsearch.Net;
using Nest;
using StackExchange.Profiling.Elasticsearch.Models;
using StackExchange.Profiling.Storage;

namespace StackExchange.Profiling.Elasticsearch
{
    public class ElasticsearchStorage : DatabaseStorageBase
    {
        private readonly string _indexName;
        private readonly ElasticClient _client;

        public ElasticsearchStorage(string connectionString) : this(connectionString, null, null)
        {
        }

        public ElasticsearchStorage(string connectionString, string indexName) : this(connectionString, indexName, null)
        {
        }

        public ElasticsearchStorage(string connectionString, string indexName, string rollingFormat) : base(connectionString)
        {
            _indexName = string.IsNullOrWhiteSpace(indexName) ? "miniprofiler" : indexName.ToLower();

            var settings = new ConnectionSettings(new SniffingConnectionPool(connectionString.Split('|', ';', ',').Select(s => new Uri(s))));

            settings.DefaultIndex(GetIndexName(_indexName, rollingFormat));

            _client = new ElasticClient(settings);
        }

        private static readonly DateTime BaseDateTime = new DateTime(2015, 12, 26);
        private static string GetIndexName(string indexName, string rollingFormat)
        {
            if (string.IsNullOrWhiteSpace(rollingFormat))
                return $"{indexName}-{(DateTime.Now - BaseDateTime).TotalDays / 7:000}";
            return $"{indexName}-{DateTime.Now.ToString(rollingFormat)}";
        }

        public override List<Guid> GetUnviewedIds(string user) => _client
            .Search<MiniProfilerModel>(s => s
                .Index(_indexName + "-*")
                .FielddataFields(f => f.Fields(mp => new { mp.Id }))
                .Query(q => q
                    .Bool(qb => qb.Must(qbm => qbm
                        .Term(qbmt => qbmt
                            .Field(mp => mp.User)
                            .Value(user)),
                        qbm => qbm
                            .Term(qbmt => qbmt
                                .Field(mp => mp.HasUserViewed)
                                .Value(false)))))
                .Take(10000))
            .Documents
            .Select(doc => doc.Id)
            .ToList();

        public override IEnumerable<Guid> List(int maxResults, DateTime? start = null, DateTime? finish = null, ListResultsOrder orderBy = ListResultsOrder.Descending)
        {
            Func<DateRangeQueryDescriptor<MiniProfilerModel>, IDateRangeQuery> func = null;
            if (start != null)
            {
                if (finish == null)
                    func = qr => qr.Field(mp => mp.Started).GreaterThanOrEquals(start.Value);
                else
                    func = qr => qr.Field(mp => mp.Started).GreaterThanOrEquals(start).LessThanOrEquals(finish.Value);
            }

            return _client
                .Search<MiniProfilerModel>(s => s
                    .Index(_indexName + "-*")
                    .FielddataFields(f => f.Fields(mp => new { mp.Id }))
                    .Query(q => func == null ? q.MatchAll() : q.DateRange(func))
                    .Take(maxResults)
                    .Sort(ss => orderBy == ListResultsOrder.Descending ? ss.Descending(mp => mp.Started) : ss.Ascending(mp => mp.Started)))
                .Documents
                .Select(doc => doc.Id)
                .ToList();
        }

        public override MiniProfiler Load(Guid id)
        {
            string indexName;
            return Get(id, out indexName);
        }

        private MiniProfilerModel Get(Guid id, out string indexName)
        {
            var response = _client.Get<MiniProfilerModel>(id.ToString());

            indexName = _client.ConnectionSettings.DefaultIndex;

            if (response.Source == null)
            {
                var hit = _client
                       .Search<MiniProfilerModel>(s => s
                           .Index(_indexName + "-*")
                           .FielddataFields(f => f.Fields(mp => new { mp.Id }))
                           .Query(q => q.Match(qm => qm.Field(mp => mp.Id).Query(id.ToString())))
                           .Take(1))
                       .Hits
                       .FirstOrDefault();

                if (hit != null)
                {
                    indexName = hit.Index;
                    return hit.Source;
                }
            }

            return response.Source;
        }

        public override void Save(MiniProfiler profiler)
        {
            _client.Index<MiniProfilerModel>(profiler);
        }

        private void SetHasUserViewedValue(Guid id, bool value)
        {
            string indexName;
            var result = Get(id, out indexName);
            if (result != null && result.HasUserViewed != value)
                _client.Update<MiniProfilerModel, object>(id, ud => ud
                    .Index(indexName)
                    .Doc(new { HasUserViewed = value }));
        }

        public override void SetUnviewed(string user, Guid id)
        {
            SetHasUserViewedValue(id, false);
        }

        public override void SetViewed(string user, Guid id)
        {
            SetHasUserViewedValue(id, true);
        }
    }
}
