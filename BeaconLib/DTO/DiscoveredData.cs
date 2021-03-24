using System;

namespace BeaconLib.DTO
{
    public class DiscoveredData
    {
        public string Ip { get; set; }
        public int Port { get; set; }
        public string GrpcEndpoint { get; set; }
    }
}
