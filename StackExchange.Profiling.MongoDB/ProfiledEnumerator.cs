using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace StackExchange.Profiling.MongoDB
{
    public class ProfiledEnumerable<TDocument> : IEnumerable<TDocument>
    {
        private readonly IEnumerable<TDocument> _underlyingEnumerable;
        private IEnumerator<TDocument> _profiledEnumerator;

        public ProfiledEnumerable(IEnumerable<TDocument> underlyingEnumerable)
        {
            _underlyingEnumerable = underlyingEnumerable;
        }

        public IEnumerator<TDocument> GetEnumerator()
        {
            lock (this)
            {
                if (_profiledEnumerator == null)
                {
                    _profiledEnumerator = new ProfiledEnumerator<TDocument>(_underlyingEnumerable.GetEnumerator());
                }
            }

            return _profiledEnumerator;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class ProfiledEnumerator<TDocument> : IEnumerator<TDocument>
    {
        public class EnumerationEndedEventArgs : EventArgs
        {
            public TimeSpan Elapsed { get; set; }
        }

        private readonly Stopwatch _sw;
        private bool _enumStarted;

        public event EventHandler EnumerationStarted;
        public event EventHandler<EnumerationEndedEventArgs> EnumerationEnded;

        protected virtual void OnEnumerationStarted()
        {
            EventHandler handler = EnumerationStarted;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        protected virtual void OnEnumerationEnded(EnumerationEndedEventArgs e)
        {
            EventHandler<EnumerationEndedEventArgs> handler = EnumerationEnded;
            if (handler != null) handler(this, e);
        }

        private IEnumerator<TDocument> _underlyingEnumerator;

        public ProfiledEnumerator(IEnumerator<TDocument> underlyingEnumerator)
        {
            _underlyingEnumerator = underlyingEnumerator;

            _sw = new Stopwatch();
        }

        public void Dispose()
        {
            _underlyingEnumerator.Dispose();
            _underlyingEnumerator = null;
        }

        public bool MoveNext()
        {
            if (!_enumStarted)
            {
                _enumStarted = true;
                _sw.Start();

                OnEnumerationStarted();
            }

            var result = _underlyingEnumerator.MoveNext();

            if (!result)
            {
                _sw.Stop();

                OnEnumerationEnded(new EnumerationEndedEventArgs {Elapsed = _sw.Elapsed});
            }

            return result;
        }

        public void Reset()
        {
            // will throw NotSupportedException
            _underlyingEnumerator.Reset();
        }

        public TDocument Current
        {
            get { return _underlyingEnumerator.Current; }
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }
    }
}
