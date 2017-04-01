using System;
using System.Collections.Generic;
using System.Diagnostics;
using StackExchange.Profiling.Internal;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Class used to initialize MiniProfiler diagnostic listeners.
    /// </summary>
    internal class DiagnosticInitializer : IObserver<DiagnosticListener>, IDisposable
    {
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private readonly IEnumerable<IMiniProfilerDiagnosticListener> _diagnosticListeners;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiagnosticInitializer"/> class.
        /// </summary>
        /// <param name="diagnosticListeners">The diagnostic listeners to register</param>
        public DiagnosticInitializer(IEnumerable<IMiniProfilerDiagnosticListener> diagnosticListeners)
        {
            _diagnosticListeners = diagnosticListeners;
        }

        /// <summary>
        /// Subscribes diagnostic listeners to all current (and future) sources.
        /// </summary>
        public void Start()
        {
            DiagnosticListener.AllListeners.Subscribe(this);
        }

        void IObserver<DiagnosticListener>.OnNext(DiagnosticListener value)
        {
            foreach (var applicationInsightDiagnosticListener in _diagnosticListeners)
            {
                if (applicationInsightDiagnosticListener.ListenerName == value.Name)
                {
                    _subscriptions.Add(value.SubscribeWithAdapter(applicationInsightDiagnosticListener));
                }
            }
        }

        void IObserver<DiagnosticListener>.OnError(Exception error) { }
        void IObserver<DiagnosticListener>.OnCompleted() { }

        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            foreach (var subscription in _subscriptions)
            {
                subscription.Dispose();
            }
        }
    }
}