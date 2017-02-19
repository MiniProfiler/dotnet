namespace StackExchange.Profiling
{
    /// <summary>
    /// Dictates on which side of the page the profiler popup button is displayed; defaults to top left.
    /// </summary>
    public enum RenderPosition
    {
        /// <summary>
        /// Profiler popup button is displayed on the top left.
        /// </summary>
        Left = 0,

        /// <summary>
        /// Profiler popup button is displayed on the top right.
        /// </summary>
        Right = 1,

        /// <summary>
        /// Profiler popup button is displayed on the bottom left.
        /// </summary>
        BottomLeft = 2,

        /// <summary>
        /// Profiler popup button is displayed on the bottom right.
        /// </summary>
        BottomRight = 3
    }
}