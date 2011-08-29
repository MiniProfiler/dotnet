using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.ServiceModel;
using System.Diagnostics.Contracts;

namespace MvcMiniProfiler.Wcf.Helpers
{
    /// <summary>
    /// Taken from http://blog.caraulean.com/2008/02/13/httpcontext-idiom-for-windows-communication-foundation/
    /// </summary>
    internal class WcfInstanceContext : IExtension<InstanceContext>
    {
        private readonly IDictionary _items;

        private WcfInstanceContext()
        {
            _items = new Hashtable();
        }

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
        public static WcfInstanceContext GetCurrentWithoutInstantiating()
        {
            var wcfContext = OperationContext.Current;
            if (wcfContext == null)
                return null;

            return wcfContext.InstanceContext.Extensions.Find<WcfInstanceContext>();
        }

        /// <summary>
        /// Gets or sets the current <see cref="WcfInstanceContext"/>.  Note that if
        /// this is only going to be used to read items, it is more performant to use
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

        public void Attach(InstanceContext owner) { }

        public void Detach(InstanceContext owner) { }
    }
}
