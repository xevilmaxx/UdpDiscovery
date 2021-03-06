using BeaconLib.DTO;
using BeaconLib.Helpers;
using BeaconLib.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace BeaconLib.RemoteMachine
{
    /// <summary>
    /// Instances of this class can be autodiscovered on the local network through UDP broadcasts
    /// </summary>
    /// <remarks>
    /// The advertisement consists of the beacon's application type and a short beacon-specific string.
    /// </remarks>
    public class RemoteBeacon : IDisposable, IBeacon
    {

        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        internal const int DiscoveryPort = 35891;
        private readonly UdpClient udp;
        private IPEndPoint sender = new IPEndPoint(0, 0);

        private Dictionary<IPAddress, IPAddress> _localIPAddress;  //key:IP address   value:subnetmask

        public string BeaconType { get; private set; }
        public ushort AdvertisedPort { get; private set; }
        public bool Stopped { get; private set; }
        public string BeaconData { get; set; }

        /// <summary>
        /// Advertised Port value is currently unused
        /// </summary>
        /// <param name="beaconType"></param>
        /// <param name="advertisedPort"></param>
        public RemoteBeacon(string beaconType, ushort advertisedPort = 1234)
        {

            _localIPAddress = Utils.GetLocalIPAddress();

            BeaconType     = beaconType;
            //for now it seems not used
            AdvertisedPort = advertisedPort;
            BeaconData     = "";

            udp = new UdpClient();

            udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udp.Client.EnableBroadcast = true;
            udp.Client.ExclusiveAddressUse = false;

            udp.Client.Bind(new IPEndPoint(IPAddress.Any, DiscoveryPort));

            try 
            {
                udp.AllowNatTraversal(true);
            }
            catch (Exception ex)
            {
                log.Debug("Error switching on NAT traversal: " + ex.Message);
            }
        }

        public void Start()
        {
            Stopped = false;
            udp.BeginReceive(ProbeReceived, null);
        }

        public void Stop()
        {
            Stopped = true;
        }

        private void ProbeReceived(IAsyncResult ar)
        {

            log.Trace("ProbeReceived Invoked!");

            var bytes  = udp.EndReceive(ar, ref sender);

            // Compare beacon type to probe type
            var typeBytes = SharedMethods.Encode(BeaconType);
            if (SharedMethods.HasPrefix(bytes, typeBytes))
            {
                log.Trace("Has prefix");
                // If true, respond again with our type, port and payload
                var responseData = SharedMethods.Encode(BeaconType)
                    .Concat(BitConverter.GetBytes((ushort)IPAddress.HostToNetworkOrder((short)AdvertisedPort)))
                    .Concat(SharedMethods.Encode(BeaconData)).ToArray();

                Utils.BroadCastOnAllInterfaces(ref _localIPAddress, ref responseData, BroadcastWay.Server);
            }

            if (!Stopped) udp.BeginReceive(ProbeReceived, null);

        }

        public void Dispose()
        {
            Stop();
        }
    }
}
