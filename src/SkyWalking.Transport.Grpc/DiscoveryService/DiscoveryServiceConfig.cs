using SkyWalking.Config;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkyWalking.Transport.Grpc.DiscoveryService
{
    [Config("SkyWalking", "Transport", "DiscoveryService")]
    public class DiscoveryServiceConfig
    {
        public string ServiceAddress { get; set; }

        public int Timeout { get; set; }

        public int RefreshInterval { get; set; }
    }
}
