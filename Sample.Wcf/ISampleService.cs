using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace Sample.Wcf
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    [ServiceContract]
    public interface ISampleService
    {
        [OperationContract]
        IEnumerable<RouteHit> FetchRouteHits();

        [OperationContract]
        string ServiceMethodThatIsNotProfiled();
        [OperationContract]
        string MassiveNesting();
        [OperationContract]
        string MassiveNesting2();
        [OperationContract]
        string Duplicated();

    }

    [DataContract]
    public class RouteHit
    {
        [DataMember]
        public string RouteName { get; set; }
        [DataMember]
        public Int64 HitCount { get; set; }
    }
}
