namespace StackExchange.Profiling.Wcf
{
    using System;
    using System.ServiceModel.Configuration;
    using System.ServiceModel.Description;

    /// <summary>
    /// The WCF mini profiler behaviour.
    /// </summary>
    public class WcfMiniProfilerBehavior : BehaviorExtensionElement, IEndpointBehavior
    {
        /// <summary>
        /// Gets the behaviour type.
        /// </summary>
        public override Type BehaviorType
        {
            get { return this.GetType(); }
        }

        /// <summary>
        /// add the binding parameters.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <param name="bindingParameters">The binding parameters.</param>
        public void AddBindingParameters(ServiceEndpoint endpoint, System.ServiceModel.Channels.BindingParameterCollection bindingParameters)
        {
        }

        /// <summary>
        /// apply the client behaviour.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <param name="clientRuntime">The client runtime.</param>
        public void ApplyClientBehavior(ServiceEndpoint endpoint, System.ServiceModel.Dispatcher.ClientRuntime clientRuntime)
        {
            var inspector = new WcfMiniProfilerClientInspector();
            clientRuntime.MessageInspectors.Add(inspector);
        }

        /// <summary>
        /// apply the dispatch behaviour.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <param name="endpointDispatcher">The endpoint dispatcher.</param>
        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, System.ServiceModel.Dispatcher.EndpointDispatcher endpointDispatcher)
        {
            var inspector = new WcfMiniProfilerDispatchInspector();
            endpointDispatcher.DispatchRuntime.MessageInspectors.Add(inspector);
        }

        /// <summary>
        /// validate the service end point.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        public void Validate(ServiceEndpoint endpoint)
        {
        }

        /// <summary>
        /// create the behaviour.
        /// </summary>
        /// <returns><c>this</c></returns>
        protected override object CreateBehavior()
        {
            return this;
        }
    }
}
