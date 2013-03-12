namespace StackExchange.Profiling
{
    /// <summary>
    /// Categorizes individual <see cref="Timing"/> steps to allow filtering.
    /// </summary>
    public enum ProfileLevel : byte
    {
        /// <summary>
        /// Default level given to Timings.
        /// </summary>
        Info = 0,

        /// <summary>
        /// Useful when profiling many items in a loop, but you don't wish to always see this detail.
        /// </summary>
        Verbose = 1
    }
}