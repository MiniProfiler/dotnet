using System;
using System.Linq;
using Nest;

namespace StackExchange.Profiling.Elasticsearch.Models
{
    [ElasticsearchType(IdProperty = "Id", Name = "MiniProfiler")]
    class MiniProfilerModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateTime Started { get; set; }
        public string MachineName { get; set; }
        public string User { get; set; }
        public decimal DurationMilliseconds { get; set; }
        public string CustomLinksJson { get; set; }
        public int? ClientTimingsRedirectCount { get; set; }
        public bool HasUserViewed { get; set; }

        public TimingModel Root { get; set; }

        public ClientTimingsModel ClientTimings { get; set; }

        #region MiniProfilerModel To MiniProfiler
        public static implicit operator MiniProfiler(MiniProfilerModel model)
        {
            if (model == null)
                return null;

            var profiler = new MiniProfiler(model.Name)
            {
                Id = model.Id,
                Name = model.Name,
                Started = model.Started,
                MachineName = model.MachineName,
                User = model.User,
                DurationMilliseconds = model.DurationMilliseconds,
                CustomLinksJson = model.CustomLinksJson,
                ClientTimingsRedirectCount = model.ClientTimingsRedirectCount,
                HasUserViewed = model.HasUserViewed
            };

            if (model.ClientTimings != null)
                profiler.ClientTimings = new ClientTimings
                {
                    RedirectCount = model.ClientTimings.RedirectCount,
                    Timings = model.ClientTimings.Timings == null ? null : model.ClientTimings.Timings.Select(ct => new ClientTimings.ClientTiming
                    {
                        Name = ct.Name,
                        Duration = ct.Duration,
                        Id = ct.Id,
                        MiniProfilerId = ct.MiniProfilerId,
                        Start = ct.Start
                    }).ToList()
                };

            if (model.Root != null)
            {
                profiler.Root = Convert(profiler, null, model.Root);
                profiler.RootTimingId = profiler.Root.Id;
            }

            return profiler;
        }

        public static Timing Convert(MiniProfiler profiler, Timing parent, TimingModel timing)
        {
            if (timing == null)
                return null;

            var model = new Timing(profiler, parent, timing.Name)
            {
                DurationMilliseconds = timing.DurationMilliseconds,
                Id = timing.Id,
                StartMilliseconds = timing.StartMilliseconds,
                ParentTiming = parent,
                ParentTimingId = parent == null ? Guid.Empty : parent.Id,
                MiniProfilerId = profiler.Id
            };

            if (timing.CustomTimings != null && timing.CustomTimings.Count > 0)
                model.CustomTimings = timing.CustomTimings
                    .ToDictionary(t => t.Key, t => t.Value
                        .Select(ct => new CustomTiming(profiler, ct.CommandString)
                        {
                            Id = ct.Id,
                            DurationMilliseconds = ct.DurationMilliseconds,
                            ExecuteType = ct.ExecuteType,
                            FirstFetchDurationMilliseconds = ct.FirstFetchDurationMilliseconds,
                            StackTraceSnippet = ct.StackTraceSnippet,
                            StartMilliseconds = ct.StartMilliseconds
                        }).ToList());

            if (timing.Children != null && timing.Children.Any())
                model.Children = timing.Children.Select(t => Convert(profiler, model, t)).ToList();

            return model;
        }
        #endregion

        #region MiniProfiler To MiniProfilerModel
        public static implicit operator MiniProfilerModel(MiniProfiler profiler)
        {
            var model = new MiniProfilerModel
            {
                Id = profiler.Id,
                Name = profiler.Name,
                Started = profiler.Started,
                MachineName = profiler.MachineName,
                User = profiler.User,
                DurationMilliseconds = profiler.DurationMilliseconds,
                CustomLinksJson = profiler.CustomLinksJson,
                ClientTimingsRedirectCount = profiler.ClientTimingsRedirectCount,
                HasUserViewed = profiler.HasUserViewed
            };

            if (profiler.ClientTimings != null)
                model.ClientTimings = new ClientTimingsModel
                {
                    RedirectCount = profiler.ClientTimings.RedirectCount,
                    Timings = profiler.ClientTimings.Timings == null ? null : profiler.ClientTimings.Timings.Select(ct => new ClientTimingModel
                    {
                        Name = ct.Name,
                        Duration = ct.Duration,
                        Id = ct.Id,
                        MiniProfilerId = ct.MiniProfilerId,
                        Start = ct.Start
                    })
                };

            model.Root = Convert(profiler.Root);

            return model;
        }

        public static TimingModel Convert(Timing timing)
        {
            var model = new TimingModel
            {
                DurationMilliseconds = timing.DurationMilliseconds,
                Id = timing.Id,
                Name = timing.Name,
                StartMilliseconds = timing.StartMilliseconds
            };

            if (timing.HasCustomTimings)
                model.CustomTimings = timing.CustomTimings
                    .ToDictionary(t => t.Key, t => t.Value
                        .Select(ct => new CustomTimingModel
                        {
                            Id = ct.Id,
                            CommandString = ct.CommandString,
                            DurationMilliseconds = ct.DurationMilliseconds,
                            ExecuteType = ct.ExecuteType,
                            FirstFetchDurationMilliseconds = ct.FirstFetchDurationMilliseconds,
                            StackTraceSnippet = ct.StackTraceSnippet,
                            StartMilliseconds = ct.StartMilliseconds
                        }));

            if (timing.HasChildren)
                model.Children = timing.Children.Select(Convert);

            return model;
        }
        #endregion
    }
}