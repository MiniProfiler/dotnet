using System;
using System.Linq;
using System.Threading;
using System.Web.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using SampleWeb.Data;
using SampleWeb.Models;
using StackExchange.Profiling;

namespace SampleWeb.Controllers
{
    public class HomeController : BaseController
    {
        private static Random _random = new Random();

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

            // update docs just to update docs (meaningless activity)
            Repository.FooCollection.FindAndModify(Query.EQ("r", 0.12345), SortBy.Ascending("i"),
                Update.Set("updated", true));

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

            // add couple of records
            // single record
            Repository.BarCollection.Insert(new BsonDocument {{"timestamp", DateTime.Now}});

            // 2 records at once
            Repository.BarCollection.InsertBatch(new[]
            {
                new BsonDocument {{"timestamp", DateTime.Now}},
                new BsonDocument {{"timestamp", DateTime.Now.AddSeconds(1)}}
            });

            // update couple of records
            Repository.FooCollection.Update(Query.LT("r", 0.01), Update.Inc("up", 1), UpdateFlags.Multi);

            // find one record
            var oneRecord = Repository.FooCollection.FindOneAs<BsonDocument>(Query.GT("r", _random.NextDouble()));
            oneRecord.Set("meta", "updated");

            Repository.FooCollection.Save(oneRecord);

            // testing typed collections

            Repository.BazzCollection.Insert(new BazzItem
            {
                CurrentTimestamp = DateTime.Now,
                SomeRandomInt = _random.Next(0, 256),
                SomeRandomDouble = _random.NextDouble()
            });

            return View(model);
        }
    }
}
