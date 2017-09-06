using System;
using System.Data;
using System.Data.Common;
using StackExchange.Profiling.Data;
using StackExchange.Profiling.Internal;
using System.Collections.Generic;

namespace StackExchange.Profiling
{
    public partial class MiniProfiler : IDbProfiler
    {
        // Is this more complicated than needed? Yes. But we're avoiding allocating Dictionaries (or ConcurrentDictionaries) up front.
        // They are by far the heaviest memory part of a profiler, so this allocates them when needed
        // Note that these operations almost certainly involve IO, so the critical section behavior is almost certainly insignificant on impact.
        private readonly object _dbLocker = new object();
        private Dictionary<Tuple<object, SqlExecuteType>, CustomTiming> _inProgress;
        private Dictionary<IDataReader, CustomTiming> _inProgressReaders;

        /// <summary>
        /// Tracks when 'command' is started.
        /// </summary>
        /// <param name="profiledDbCommand">The <see cref="IDbCommand"/> that started.</param>
        /// <param name="executeType">The execution type of the <paramref name="profiledDbCommand"/>.</param>
        void IDbProfiler.ExecuteStart(IDbCommand profiledDbCommand, SqlExecuteType executeType)
        {
            var id = Tuple.Create((object)profiledDbCommand, executeType);
            var timing = profiledDbCommand.GetTiming(executeType.ToString(), this);
            lock (_dbLocker)
            {
                _inProgress = _inProgress ?? new Dictionary<Tuple<object, SqlExecuteType>, CustomTiming>();
                _inProgress[id] = timing;
            }
        }

        /// <summary>
        /// Finishes profiling for 'command', recording durations.
        /// </summary>
        /// <param name="profiledDbCommand">The <see cref="IDbCommand"/> that finished.</param>
        /// <param name="executeType">The execution type of the <paramref name="profiledDbCommand"/>.</param>
        /// <param name="reader">(Optional) the reader piece of the <paramref name="profiledDbCommand"/>, if it exists.</param>
        void IDbProfiler.ExecuteFinish(IDbCommand profiledDbCommand, SqlExecuteType executeType, DbDataReader reader)
        {
            if (_inProgress == null)
            {
                return;
            }

            var id = Tuple.Create((object)profiledDbCommand, executeType);
            CustomTiming current;
            lock (_inProgress)
            {
                if (!_inProgress.TryRemove(id, out current))
                {
                    return;
                }
            }

            if (reader != null)
            {
                lock (_dbLocker)
                {
                    _inProgressReaders = _inProgressReaders ?? new Dictionary<IDataReader, CustomTiming>();
                    _inProgressReaders[reader] = current;
                }
                current.FirstFetchCompleted();
            }
            else
            {
                current.Stop();
            }
        }

        /// <summary>
        /// Called when 'reader' finishes its iterations and is closed.
        /// </summary>
        /// <param name="reader">The <see cref="IDataReader"/> that finished.</param>
        void IDbProfiler.ReaderFinish(IDataReader reader)
        {
            if (_inProgressReaders == null)
            {
                return;
            }

            CustomTiming timing;
            lock (_inProgressReaders)
            {
                _inProgressReaders.TryRemove(reader, out timing);
            }

            // This reader may have been disposed/closed by reader code, not by our using()
            timing?.Stop();
        }

        /// <summary>
        /// Called when a command errors.
        /// </summary>
        /// <param name="profiledDbCommand">The <see cref="IDbCommand"/> that finished.</param>
        /// <param name="executeType">The execution type of the <paramref name="profiledDbCommand"/>.</param>
        /// <param name="exception">The exception thrown.</param>
        void IDbProfiler.OnError(IDbCommand profiledDbCommand, SqlExecuteType executeType, Exception exception)
        {
            if (_inProgress == null)
            {
                return;
            }

            var id = Tuple.Create((object)profiledDbCommand, executeType);
            CustomTiming timing;
            lock (_inProgress)
            {
                _inProgress.TryRemove(id, out timing);
            }

            if (timing != null)
            {
                timing.Errored = true;
                timing.Stop();
            }
        }

        bool IDbProfiler.IsActive => IsActive;
    }
}
