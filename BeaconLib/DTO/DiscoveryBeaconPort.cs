using System;
using System.Collections.Generic;
using System.Text;

namespace BeaconLib.DTO
{
    public static class DiscoveryBeaconPort
    {
        public static ushort ParkO_RTS { get; set; } = 4567;
        public static ushort LiteGate { get; set; } = 4567;
        public static ushort CorpiBridge { get; set; } = 4567;
    }
}
