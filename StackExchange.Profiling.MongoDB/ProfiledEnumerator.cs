using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace StackExchange.Profiling.MongoDB
{
    public class ProfiledEnumerator<TDocument> : IEnumerator<TDocument>
    {
        public class EnumerationEndedEventArgs : EventArgs
        {
            public long ElapsedMilliseconds { get; set; }
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

                OnEnumerationEnded(new EnumerationEndedEventArgs {ElapsedMilliseconds = _sw.ElapsedMilliseconds});
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
