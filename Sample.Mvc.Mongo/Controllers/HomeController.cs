using System.Linq;
using System.Threading;
using System.Web.Mvc;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using SampleWeb.Models;
using StackExchange.Profiling;

namespace SampleWeb.Controllers
{
    public class HomeController : BaseController
    {
        public ActionResult EnableProfilingUI()
        {
            MvcApplication.DisableProfilingResults = false;
            return Redirect("/");
        }

        public ActionResult DisableProfilingUI() 
        {
            MvcApplication.DisableProfilingResults = true;
            return Redirect("/");
        }

        public ActionResult Index()
        {
            var profiler = MiniProfiler.Current;

            using (profiler.Step("Set page title"))
            {
                ViewBag.Title = "Home Page";
            }

            using (profiler.Step("Doing complex stuff"))
            {
                using (profiler.Step("Step A"))
                {
                    Thread.Sleep(100);
                }
                using (profiler.Step("Step B"))
                {
                    Thread.Sleep(250);
                }
            }

            // create couple of indexes

            Repository.FooCollection.EnsureIndex(IndexKeys.Ascending("i"), IndexOptions.SetBackground(true));

            var model = new MongoDemoModel
            {
                FooCount = (int) Repository.FooCollection.Count(),
                FooCountQuery = (int) Repository.FooCollection.Count(Query.LT("r", 0.5)),
                AggregateResult = Repository.FooCollection.Aggregate(
                    new BsonDocument
                    {
                        {
                            "$match", new BsonDocument
                            {
                                {
                                    "r", new BsonDocument
                                    {
                                        {"$gt", 0.2}
                                    }
                                }
                            }
                        }
                    },
                    new BsonDocument
                    {
                        {
                            "$group", new BsonDocument
                            {
                                {
                                    "_id", new BsonDocument
                                    {
                                        {
                                            "$cond", new BsonArray
                                            {
                                                new BsonDocument
                                                {
                                                    {"$lt", new BsonArray {"$r", 0.6}}
                                                },
                                                "lessthen0.6",
                                                "morethan0.6"
                                            }
                                        }
                                    }
                                },
                                {
                                    "count", new BsonDocument
                                    {
                                        {"$sum", 1}
                                    }
                                }
                            }
                        }
                    }
                    ).Response.ToString(),
                ExplainResult =
                    Repository.FooCollection.FindAs<BsonDocument>(Query.GT("r", 0.5))
                        .SetLimit(10)
                        .SetSortOrder(SortBy.Descending("i"))
                        .Explain()
                        .ToString(),
                FiveDocs =
                    string.Join("\n",
                        Repository.FooCollection.FindAs<BsonDocument>(Query.GTE("r", 0.8))
                            .SetLimit(5)
                            .SetSortOrder(SortBy.Descending("r"))
                            .ToList())
            };
            
            // drop all indexes
            Repository.FooCollection.DropAllIndexes();

            return View(model);
        }
    }
}
