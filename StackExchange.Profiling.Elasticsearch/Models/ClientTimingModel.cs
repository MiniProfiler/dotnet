using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StackExchange.Profiling.Elasticsearch.Models
{
    class ClientTimingsModel
    {
        public IEnumerable<ClientTimingModel> Timings { get; set; }

        public int RedirectCount { get; set; }
    }

    class ClientTimingModel
    {
        public Guid Id { get; set; }
        public Guid MiniProfilerId { get; set; }
        public string Name { get; set; }
        public decimal Start { get; set; }
        public decimal Duration { get; set; }
    }
}
