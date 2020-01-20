namespace StackExchange.Profiling
{
    /// <summary>
    /// The color scheme to use when rendering MiniProfiler.
    /// This is used both in the render popup and on the standalone pages.
    /// Ultimately, used to set classes for CSS variables.
    /// </summary>
    public enum ColorScheme
    {
        /// <summary>
        /// "Light" mode (the default), white background, etc.
        /// </summary>
        Light,

        /// <summary>
        /// "Dark" mode, near-black background, etc.
        /// </summary>
        Dark,

        /// <summary>
        /// <para>"Auto" mode, respecting what the user prefers via <c>prefers-color-scheme</c> in CSS.</para>
        /// <para>This is opt-in because it'd be really odd for only MiniProfiler to respect it as part of a larger page.</para>
        /// </summary>
        Auto,
    }
}
