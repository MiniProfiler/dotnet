namespace Sample.Wcf
{
    using System.Collections.Generic;
    using System.ServiceModel;

    /// <summary>
    /// The SampleService interface.
    /// NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    /// </summary>
    [ServiceContract]
    public interface ISampleService
    {
        /// <summary>
        /// The fetch route hits.
        /// </summary>
        /// <returns>the route hits.</returns>
        [OperationContract]
        IEnumerable<RouteHit> FetchRouteHits();

        /// <summary>
        /// The service method that is not profiled.
        /// </summary>
        /// <returns>The <see cref="string"/>.</returns>
        [OperationContract]
        string ServiceMethodThatIsNotProfiled();

        /// <summary>
        /// The massive nesting.
        /// </summary>
        /// <returns>The <see cref="string"/>.</returns>
        [OperationContract]
        string MassiveNesting();

        /// <summary>
        /// The massive nesting 2.
        /// </summary>
        /// <returns>The <see cref="string"/>.</returns>
        [OperationContract]
        string MassiveNesting2();

        /// <summary>
        /// The duplicated.
        /// </summary>
        /// <returns>The <see cref="string"/>.</returns>
        [OperationContract]
        string Duplicated();

    }
}
