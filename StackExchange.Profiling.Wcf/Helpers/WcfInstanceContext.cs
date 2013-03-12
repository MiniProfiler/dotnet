namespace StackExchange.Profiling.Wcf.Helpers
{
    using System.Collections;
    using System.Diagnostics.Contracts;
    using System.ServiceModel;

    /// <summary>
    /// <c>Taken from http://blog.caraulean.com/2008/02/13/httpcontext-idiom-for-windows-communication-foundation/</c>
    /// </summary>
    internal class WcfInstanceContext : IExtension<InstanceContext>
    {
        /// <summary>
        /// The items.
        /// </summary>
        private readonly IDictionary _items;

        /// <summary>
        /// Prevents a default instance of the <see cref="WcfInstanceContext"/> class from being created.
        /// </summary>
        private WcfInstanceContext()
        {
            _items = new Hashtable();
        }

        /// <summary>
        /// Gets the current <see cref="WcfInstanceContext"/>. Note that if
        /// this is only going to be used to read items, it is more <c>performant</c> to use
        /// <see cref="GetCurrentWithoutInstantiating"/> as this does not create and
        /// attach a new extension.
        /// </summary>
        public static WcfInstanceContext Current
        {
            get
            {
                var wcfContext = OperationContext.Current;
                if (wcfContext == null)
                    return null;

                var context = wcfContext.InstanceContext.Extensions.Find<WcfInstanceContext>();
                if (context == null)
                {
                    context = new WcfInstanceContext();
                    wcfContext.InstanceContext.Extensions.Add(context);
                }

                return context;
            }
        }

        /// <summary>
        /// Gets the items.
        /// </summary>
        public IDictionary Items
        {
            get
            {
                Contract.Ensures(Contract.Result<IDictionary>() != null);

                return _items;
            }
        }

        /// <summary>
        /// Returns true if we are currently inside a WCF call and the <see cref="WcfInstanceContext"/> object
        /// has been instantiated through a call to <see cref="Current"/>.
        /// </summary>
        /// <returns>the WCF instance context.</returns>
        public static WcfInstanceContext GetCurrentWithoutInstantiating()
        {
            var wcfContext = OperationContext.Current;
            if (wcfContext == null)
                return null;

            return wcfContext.InstanceContext.Extensions.Find<WcfInstanceContext>();
        }

        /// <summary>
        /// attach an instance.
        /// </summary>
        /// <param name="owner">The owner.</param>
        public void Attach(InstanceContext owner)
        {
        }

        /// <summary>
        /// detach an instance.
        /// </summary>
        /// <param name="owner">The owner.</param>
        public void Detach(InstanceContext owner)
        {
        }
    }
}
