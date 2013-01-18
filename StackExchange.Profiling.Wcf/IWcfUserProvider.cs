namespace StackExchange.Profiling.Wcf
{
    /// <summary>
    /// The <c>WcfUserProvider</c> interface.
    /// </summary>
    public interface IWcfUserProvider
    {
        /// <summary>
        /// Returns a string to identify the user profiling the current 'request'.
        /// </summary>
        /// <returns>a string containing the user</returns>
        string GetUser(/*HttpRequest request*/);
    }
}
