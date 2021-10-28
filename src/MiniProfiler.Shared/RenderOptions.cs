using StackExchange.Profiling.Internal;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Options for rendering a specific MiniProfiler instance.
    /// </summary>
    public class RenderOptions
    {
        /// <summary>
        /// The UI position to render the profiler in.
        /// Defaults to <see cref="MiniProfilerBaseOptions.PopupRenderPosition"/>.
        /// </summary>
        public RenderPosition? Position { get; set; }

        /// <summary>
        /// Whether to show trivial timings column initially or not.
        /// Defaults to <see cref="MiniProfilerBaseOptions.PopupShowTrivial"/>.
        /// </summary>
        public bool? ShowTrivial { get; set; }

        /// <summary>
        /// Whether to show time with children column initially or not.
        /// Defaults to <see cref="MiniProfilerBaseOptions.PopupShowTimeWithChildren"/>.
        /// </summary>
        public bool? ShowTimeWithChildren { get; set; }

        /// <summary>
        /// The maximum number of profilers to show (before the oldest is removed).
        /// Defaults to <see cref="MiniProfilerBaseOptions.PopupMaxTracesToShow"/>.
        /// </summary>
        public int? MaxTracesToShow { get; set; }

        /// <summary>
        /// Whether to show the controls.
        /// Defaults to <see cref="MiniProfilerBaseOptions.ShowControls"/>.
        /// </summary>
        public bool? ShowControls { get; set; }

        /// <summary>
        /// Whether to start hidden.
        /// Defaults to <see cref="MiniProfilerBaseOptions.PopupStartHidden"/>.
        /// </summary>
        public bool? StartHidden { get; set; }

        /// <summary>
        /// The keyboard key combination to use toggle profiler visibility.
        /// Defaults to <see cref="MiniProfilerBaseOptions.PopupToggleKeyboardShortcut"/>.
        /// </summary>
        public string PopupToggleKeyboardShortcut { get; set; }

        /// <summary>
        /// The amount of ms before a timing is considered non-trivial.
        /// Defaults to <see cref="MiniProfilerBaseOptions.TrivialDurationThresholdMilliseconds"/>.
        /// </summary>
        public int? TrivialDurationThresholdMilliseconds { get; set; }

        /// <summary>
        /// The colorscheme to use when rendering this MiniProfiler.
        /// Defaults to <see cref="MiniProfilerBaseOptions.ColorScheme"/>.
        /// </summary>
        public ColorScheme? ColorScheme { get; set; }

        /// <summary>
        /// The number of decimal places to show on timings (which are in miliseconds).
        /// Defaults to <see cref="MiniProfilerBaseOptions.PopupDecimalPlaces"/>.
        /// </summary>
        public int? DecimalPlaces { get; set; }

        /// <summary>
        /// A one-time-use nonce to render in the script tag.
        /// </summary>
        /// <remarks>https://developer.mozilla.org/en-US/docs/Web/HTML/Element/script</remarks>
        public string Nonce { get; set; }
    }
}
