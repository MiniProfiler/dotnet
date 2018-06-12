using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.Extensions.Logging;

namespace StackExchange.Profiling.Internal
{
    /// <summary>
    /// A MiniProfiler-wrapped <see cref="IViewComponentInvokerFactory"/>.
    /// </summary>
    public class ProfilingViewComponentInvokerFactory : IViewComponentInvokerFactory
    {
        private readonly IViewComponentFactory _viewComponentFactory;
        private readonly ViewComponentInvokerCache _viewComponentInvokerCache;
        private readonly DiagnosticSource _diagnosticSource;
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a new <see cref="ProfilingViewComponentInvokerFactory"/>.
        /// </summary>
        /// <param name="viewComponentFactory">The <see cref="IViewComponentFactory"/>.</param>
        /// <param name="viewComponentInvokerCache">The <see cref="ViewComponentInvokerCache"/>.</param>
        /// <param name="diagnosticSource">The <see cref="DiagnosticSource"/>.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public ProfilingViewComponentInvokerFactory(
            IViewComponentFactory viewComponentFactory,
            ViewComponentInvokerCache viewComponentInvokerCache,
            DiagnosticSource diagnosticSource,
            ILoggerFactory loggerFactory)
        {
            _viewComponentFactory = viewComponentFactory ?? throw new ArgumentNullException(nameof(viewComponentFactory));
            _viewComponentInvokerCache = viewComponentInvokerCache ?? throw new ArgumentNullException(nameof(viewComponentInvokerCache));
            _diagnosticSource = diagnosticSource ?? throw new ArgumentNullException(nameof(diagnosticSource));
            _logger = (loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory))).CreateLogger<ProfilingViewComponentInvoker>();
        }

        /// <summary>
        /// Creates an instance of a <see cref="ProfilingViewComponentInvoker"/>.
        /// </summary>
        /// <param name="context">Te context to create a <see cref="ProfilingViewComponentInvoker"/> from.</param>
        public IViewComponentInvoker CreateInstance(ViewComponentContext context) =>
            new ProfilingViewComponentInvoker(
                new DefaultViewComponentInvoker(_viewComponentFactory, _viewComponentInvokerCache, _diagnosticSource, _logger)
            );
    }
}
