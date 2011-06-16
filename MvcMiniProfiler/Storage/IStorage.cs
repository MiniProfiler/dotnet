using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MvcMiniProfiler.Storage
{
    /// <summary>
    /// Provides saving and loading <see cref="MiniProfiler"/>s to a storage medium.
    /// </summary>
    public interface IStorage
    {
        /// <summary>
        /// Stores <paramref name="profiler"/> under <paramref name="id"/>, which is also its <see cref="MiniProfiler.Id"/>.
        /// </summary>
        /// <param name="id">
        /// The Guid that identifies the MiniProfiler; subsequent calls to <see cref="LoadMiniProfiler"/>
        /// will pass this Guid.
        /// </param>
        /// <param name="profiler">The results of a profiling session.</param>
        void SaveMiniProfiler(Guid id, MiniProfiler profiler);

        /// <summary>
        /// Returns a <see cref="MiniProfiler"/> from storage based on <paramref name="id"/>.
        /// </summary>
        MiniProfiler LoadMiniProfiler(Guid id);
    }
}
