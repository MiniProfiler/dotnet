using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Profiling.Internal;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace StackExchange.Profiling.Storage
{
    // <summary>
    /// The SQLITE mini profiler storage.
    /// </summary>
    public class SqliteMiniProfilerStorage : DatabaseStorageBase
    {
        public IDataContextFactory ContextFactory { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteMiniProfilerStorage"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        public SqliteMiniProfilerStorage(string connectionString, bool recreate = false) : base(connectionString)
        {
            ContextFactory = new ProfilerDbContextFactory(connectionString);

            using (var dbContext = GetDbContext())
            {
                dbContext.Init(recreate);
            }
        }

        private ProfilerDbContext GetDbContext()
        {
            return ContextFactory.Create();
        }

        /// The list of results.
        /// </summary>
        /// <param name="maxResults">The max results.</param>
        /// <param name="start">The start</param>
        /// <param name="finish">The finish</param>
        /// <param name="orderBy">The order by.</param>
        /// <returns>The result set</returns>
        public override IEnumerable<Guid> List(int maxResults, DateTime? start = null, DateTime? finish = null, ListResultsOrder orderBy = ListResultsOrder.Descending)
        {
            var task = ListAsync(maxResults, start, finish, orderBy);
            task.Wait();
            return task.Result;
        }

        public async override Task<IEnumerable<Guid>> ListAsync(int maxResults, DateTime? start = null, DateTime? finish = null, ListResultsOrder orderBy = ListResultsOrder.Descending)
        {
            using (var DbContext = GetDbContext())
            {
                var query = DbContext.MiniProfilers
                    .Where(w => (finish == null || w.Started < finish.Value) && (start == null || w.Started > start.Value));

                query = orderBy == ListResultsOrder.Ascending ? query.OrderBy(o => o.Started) : query.OrderByDescending(o => o.Started);

                return await query.Take(maxResults).Select(s => s.Id).ToListAsync();
            }
        }

        /// <summary>
        /// Stores to <c>dbo.MiniProfilers</c> under its <see cref="MiniProfiler.Id"/>;
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to save.</param>
        public override void Save(MiniProfiler profiler)
        {
            SaveAsync(profiler).Wait();
        }

        /// <summary>
        /// Asynchronously stores to <c>dbo.MiniProfilers</c> under its <see cref="MiniProfiler.Id"/>.
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to save.</param>
        public async override Task SaveAsync(MiniProfiler profiler)
        {
            using (var DbContext = GetDbContext())
            {
                if (!DbContext.MiniProfilers.Any(a => a.Id == profiler.Id))
                {
                    await DbContext.MiniProfilers.AddAsync(GetMiniProfilers(profiler));
                }

                var timings = new List<Timing>();
                if (profiler.Root != null)
                {
                    profiler.Root.MiniProfilerId = profiler.Id;
                    FlattenTimings(profiler.Root, timings);
                }

                if (timings.Any())
                {
                    var newtimings = timings.Where(a => !DbContext.MiniProfilerTimings.Any(b => b.Id == a.Id)).ToList();
                    await DbContext.MiniProfilerTimings.AddRangeAsync(newtimings.Select(timing => GetMiniProfilerTimings(timing)));
                }

                if (profiler.ClientTimings?.Timings?.Any() ?? false)
                {
                    var newtimings = profiler.ClientTimings.Timings.Where(a => !DbContext.MiniProfilerClientTimings.Any(b => b.Id == a.Id)).ToList();
                    await DbContext.MiniProfilerClientTimings.AddRangeAsync(newtimings.Select(timing => GetMiniProfilerClientTimings(profiler.Id, timing)));
                }

                await DbContext.SaveChangesAsync();
            }
        }

        public async override Task<MiniProfiler> LoadAsync(Guid id)
        {
            using (var DbContext = GetDbContext())
            {
                var result = GetMiniProfiler(await DbContext.MiniProfilers.FirstOrDefaultAsync(w => w.Id == id));
                var timings = await DbContext.MiniProfilerTimings.Where(w => w.MiniProfilerId == id).OrderBy(o => o.StartMilliseconds).Select(s => GetTiming(s)).ToListAsync();
                var clientTimings = await DbContext.MiniProfilerClientTimings.Where(w => w.MiniProfilerId == id).OrderBy(o => o.Start).Select(s => GetClientTimings(s)).ToListAsync();

                ConnectTimings(result, timings, clientTimings);

                if (result != null)
                {
                    // HACK: stored dates are UTC, but are pulled out as local time
                    result.Started = new DateTime(result.Started.Ticks, DateTimeKind.Utc);
                }
                return MapOptions(result);
            }
        }

        /// <summary>
        /// Loads the <c>MiniProfiler</c> identified by 'id' from the database.
        /// </summary>
        /// <param name="id">The profiler ID to load.</param>
        /// <returns>The loaded <see cref="MiniProfiler"/>.</returns>
        public override MiniProfiler Load(Guid id)
        {
            var task = LoadAsync(id);
            task.Wait();
            return task.Result;
        }

        private MiniProfiler MapOptions(MiniProfiler miniProfiler)
        {
            return miniProfiler == null ? null : new MiniProfiler(miniProfiler.Name, new MiniProfilerBaseOptions())
            {
                Id = miniProfiler.Id,
                HasUserViewed = miniProfiler.HasUserViewed,
                Head = miniProfiler.Head,
                MachineName = miniProfiler.MachineName,
                Root = miniProfiler.Root,
                RootTimingId = miniProfiler.RootTimingId,
                Started = miniProfiler.Started,
                User = miniProfiler.User
            };
        }

        /// <summary>
        /// Sets a particular profiler session so it is considered "unviewed"  
        /// </summary>
        /// <param name="user">The user to set this profiler ID as unviewed for.</param>
        /// <param name="id">The profiler ID to set unviewed.</param>
        public override void SetUnviewed(string user, Guid id) => ToggleViewed(user, id, false);
        /// <summary>
        /// Asynchronously sets a particular profiler session so it is considered "unviewed"  
        /// </summary>
        /// <param name="user">The user to set this profiler ID as unviewed for.</param>
        /// <param name="id">The profiler ID to set unviewed.</param>
        public override Task SetUnviewedAsync(string user, Guid id) => ToggleViewedAsync(user, id, false);
        /// <summary>
        /// Sets a particular profiler session to "viewed"
        /// </summary>
        /// <param name="user">The user to set this profiler ID as viewed for.</param>
        /// <param name="id">The profiler ID to set viewed.</param>
        public override void SetViewed(string user, Guid id) => ToggleViewed(user, id, true);
        /// <summary>
        /// Asynchronously sets a particular profiler session to "viewed"
        /// </summary>
        /// <param name="user">The user to set this profiler ID as viewed for.</param>
        /// <param name="id">The profiler ID to set viewed.</param>
        public override Task SetViewedAsync(string user, Guid id) => ToggleViewedAsync(user, id, true);

        public override List<Guid> GetUnviewedIds(string user)
        {
            using (var DbContext = GetDbContext())
            {
                return DbContext.MiniProfilers.Where(w => w.User == user && w.HasUserViewed == false).Select(s => s.Id).ToList();
            }
        }

        public async override Task<List<Guid>> GetUnviewedIdsAsync(string user)
        {
            using (var DbContext = GetDbContext())
            {
                return await DbContext.MiniProfilers.Where(w => w.User == user && w.HasUserViewed == false).Select(s => s.Id).ToListAsync();
            }
        }

        protected override DbConnection GetConnection()
        {
            throw new NotImplementedException();
        }

        #region Private Helper Methods...

        private MiniProfiler GetMiniProfiler(MiniProfilers profiler)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return profiler == null ? null : new MiniProfiler
            {
                Id = profiler.Id,
                RootTimingId = profiler.RootTimingId,
                Name = profiler.Name.Truncate(200),
                Started = profiler.Started,
                DurationMilliseconds = profiler.DurationMilliseconds,
                User = profiler.User.Truncate(100),
                HasUserViewed = profiler.HasUserViewed,
                MachineName = profiler.MachineName.Truncate(100),
                CustomLinksJson = profiler.CustomLinksJson,
                //ClientTimingsRedirectCount = profiler.
            };
#pragma warning restore CS0618 // Type or member is obsolete
        }

        private Timing GetTiming(MiniProfilerTimings timing)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return timing == null ? null : new Timing
            {
                Id = timing.Id,
                MiniProfilerId = timing.MiniProfilerId,
                ParentTimingId = timing.ParentTimingId,
                Name = timing.Name.Truncate(200),
                DurationMilliseconds = timing.DurationMilliseconds,
                StartMilliseconds = timing.StartMilliseconds,
                CustomTimingsJson = timing.CustomTimingsJson
            };
#pragma warning restore CS0618 // Type or member is obsolete
        }

        private ClientTiming GetClientTimings(MiniProfilerClientTimings timing)
        {
            return timing == null ? null : new ClientTiming
            {
                Id = timing.Id,
                MiniProfilerId = timing.MiniProfilerId,
                Name = timing.Name,
                Start = timing.Start,
                Duration = timing.Duration
            };
        }

        private MiniProfilers GetMiniProfilers(MiniProfiler profiler)
        {
            return new MiniProfilers
            {
                Id = profiler.Id,
                RootTimingId = profiler.Root?.Id,
                Name = profiler.Name.Truncate(200),
                Started = profiler.Started,
                DurationMilliseconds = profiler.DurationMilliseconds,
                User = profiler.User.Truncate(100),
                HasUserViewed = profiler.HasUserViewed,
                MachineName = profiler.MachineName.Truncate(100),
                CustomLinksJson = profiler.CustomLinksJson,
                ClientTimingsRedirectCount = profiler.ClientTimings?.RedirectCount
            };
        }

        private MiniProfilerTimings GetMiniProfilerTimings(Timing timing)
        {
            return new MiniProfilerTimings
            {
                Id = timing.Id,
                MiniProfilerId = timing.MiniProfilerId,
                ParentTimingId = timing.ParentTimingId,
                Name = timing.Name.Truncate(200),
                DurationMilliseconds = timing.DurationMilliseconds,
                StartMilliseconds = timing.StartMilliseconds,
                IsRoot = timing.IsRoot,
                Depth = timing.Depth,
                CustomTimingsJson = timing.CustomTimingsJson
            };
        }
        private MiniProfilerClientTimings GetMiniProfilerClientTimings(Guid profilerId, ClientTiming timing)
        {
            return new MiniProfilerClientTimings
            {
                Id = Guid.NewGuid(),
                MiniProfilerId = profilerId,
                Name = timing.Name.Truncate(200),
                Start = timing.Start,
                Duration = timing.Duration
            };
        }

        private void ToggleViewed(string user, Guid id, bool hasUserViewed)
        {
            //var miniprofile = DbContext.MiniProfilers.FirstOrDefault(w => w.Id == id && w.User == user);
            //if (miniprofile == null)
            //{
            //    return;
            //}
            //miniprofile.HasUserViewed = hasUserViewed;
            //DbContext.Entry(miniprofile).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            //DbContext.SaveChanges();
        }
        private Task ToggleViewedAsync(string user, Guid id, bool hasUserViewed)
        {
            return Task.CompletedTask;
            //var miniprofile = DbContext.MiniProfilers.FirstOrDefault(w => w.Id == id && w.User == user);
            //if (miniprofile == null)
            //{
            //    return;
            //}
            //miniprofile.HasUserViewed = hasUserViewed;
            //DbContext.Entry(miniprofile).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            //await DbContext.SaveChangesAsync();
        }
        #endregion Private Helper Methods.
    }

}
