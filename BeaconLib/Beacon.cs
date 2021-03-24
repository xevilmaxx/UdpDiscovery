using BeaconLib.DTO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace BeaconLib
{
    /// <summary>
    /// Instances of this class can be autodiscovered on the local network through UDP broadcasts
    /// </summary>
    /// <remarks>
    /// The advertisement consists of the beacon's application type and a short beacon-specific string.
    /// </remarks>
    public class Beacon : IDisposable
    {

        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        internal const int DiscoveryPort = 35891;
        private readonly UdpClient udp;
        private IPEndPoint sender = new IPEndPoint(0, 0);

        private Dictionary<IPAddress, IPAddress> _localIPAddress;  //key:IP address   value:subnetmask

        /// <summary>
        /// Advertised Port value is currently unused
        /// </summary>
        /// <param name="beaconType"></param>
        /// <param name="advertisedPort"></param>
        public Beacon(string beaconType, ushort advertisedPort = 1234)
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
            var typeBytes = Encode(BeaconType);
            if (HasPrefix(bytes, typeBytes))
            {
                log.Trace("Has prefix");
                // If true, respond again with our type, port and payload
                var responseData = Encode(BeaconType)
                    .Concat(BitConverter.GetBytes((ushort)IPAddress.HostToNetworkOrder((short)AdvertisedPort)))
                    .Concat(Encode(BeaconData)).ToArray();

                Utils.BroadCastOnAllInterfaces(ref _localIPAddress, ref responseData, BroadcastWay.Server);
            }

            if (!Stopped) udp.BeginReceive(ProbeReceived, null);

        }

        internal static bool HasPrefix<T>(IEnumerable<T> haystack, IEnumerable<T> prefix)
        {
            return haystack.Count() >= prefix.Count() &&
                haystack.Zip(prefix, (a, b) => a.Equals(b)).All(_ => _);
        }

        /// <summary>
        /// Convert a string to network bytes
        /// </summary>
        internal static IEnumerable<byte> Encode(string data) 
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            var len = IPAddress.HostToNetworkOrder((short)bytes.Length);

            return BitConverter.GetBytes(len).Concat(bytes);
        }

        /// <summary>
        /// Convert network bytes to a string
        /// </summary>
        internal static string Decode(IEnumerable<byte> data)
        {
            var listData = data as IList<byte> ?? data.ToList();

            var len = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(listData.Take(2).ToArray(), 0));
            if (listData.Count() < 2 + len) throw new ArgumentException("Too few bytes in packet");

            return Encoding.UTF8.GetString(listData.Skip(2).Take(len).ToArray());
        }

        /// <summary>
        /// Return the machine's hostname (usually nice to mention in the beacon text)
        /// </summary>
        public static string HostName
        {
            get { return Dns.GetHostName(); }
        }

        public string BeaconType { get; private set; }
        public ushort AdvertisedPort { get; private set; }
        public bool Stopped { get; private set; }

        public string BeaconData { get; set; }

        public void Dispose()
        {
            Stop();
        }
    }
}
