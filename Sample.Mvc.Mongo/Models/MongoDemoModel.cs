using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SampleWeb.Models
{
    public class MongoDemoModel
    {
        // count() w/ and w/o query
        public int FooCount { get; set; }
        public int FooCountQuery { get; set; }

        // string representation of aggregation result
        public string AggregateResult { get; set; }
    }
}