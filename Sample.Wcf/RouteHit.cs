namespace Sample.Wcf
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// The route hit.
    /// </summary>
    [DataContract]
    public class RouteHit
    {
        /// <summary>
        /// Gets or sets the route name.
        /// </summary>
        [DataMember]
        public string RouteName { get; set; }

        /// <summary>
        /// Gets or sets the hit count.
        /// </summary>
        [DataMember]
        public long HitCount { get; set; }
    }
}