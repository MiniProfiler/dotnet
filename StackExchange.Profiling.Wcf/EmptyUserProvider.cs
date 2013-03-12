namespace StackExchange.Profiling.Wcf
{
    /// <summary>
    /// The empty user provider.
    /// </summary>
    public class EmptyUserProvider : IWcfUserProvider
    {
        /// <summary>
        /// get the user.
        /// </summary>
        /// <returns>return the user</returns>
        public string GetUser()
        {
            return "Unknown";
        }
    }
}
