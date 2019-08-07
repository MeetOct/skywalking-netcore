using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace SkyWalking.Transport.Grpc.DiscoveryService
{
    public class ServiceResponse<T>
    {
        public T Body { get; set; }
        public HttpStatusCode HttpStatus { get; set; }
    }
}
