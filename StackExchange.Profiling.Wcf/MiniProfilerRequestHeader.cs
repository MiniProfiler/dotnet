namespace StackExchange.Profiling.Wcf
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// The mini profiler request header.
    /// </summary>
    [DataContract]
    public class MiniProfilerRequestHeader
    {
        /// <summary>
        /// The header name.
        /// </summary>
        public const string HeaderName = "MiniProfilerRequestHeader";

        /// <summary>
        /// The header namespace.
        /// </summary>
        public const string HeaderNamespace = "StackExchange.Profiling.Wcf";

        /// <summary>
        /// Gets or sets the parent profiler id.
        /// </summary>
        [DataMember]
        public Guid ParentProfilerId { get; set; }

        /// <summary>
        /// Gets or sets the user.
        /// The name of the user as provided 
        /// </summary>
        [DataMember]
        public string User { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether exclude trivial methods.
        /// </summary>
        [DataMember]
        public bool ExcludeTrivialMethods { get; set; }

        /// <summary>
        /// Gets or sets the trivial duration threshold milliseconds.
        /// </summary>
        [DataMember]
        public decimal? TrivialDurationThresholdMilliseconds { get; set; }

        /// <summary>
        /// parse the header text, and return the resulting profiler.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>the mini request header</returns>
        public static MiniProfilerRequestHeader FromHeaderText(string text)
        {
            var parts = text.Split('&');
            var header = new MiniProfilerRequestHeader
                             {
                                 ParentProfilerId = Guid.Parse(parts[0]),
                                 User = parts[1],
                                 ExcludeTrivialMethods = parts[2] == "y"
                             };

            if (parts.Length > 3)
                header.TrivialDurationThresholdMilliseconds = decimal.Parse(parts[3]);

            return header;
        }

        /// <summary>
        /// return the header text.
        /// </summary>
        /// <returns>a string containing the header text.</returns>
        public string ToHeaderText()
        {
            var text = 
                Convert.ToString(this.ParentProfilerId) + "&" + 
                this.User + "&" 
                + (this.ExcludeTrivialMethods ? "y" : "n") 
                + (this.TrivialDurationThresholdMilliseconds.HasValue ? "&" + Convert.ToString(this.TrivialDurationThresholdMilliseconds.Value) : string.Empty);

            return text;
        }
    }
}
