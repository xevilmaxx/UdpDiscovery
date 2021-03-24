using BeaconLib.DTO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace BeaconLib
{
    /// <summary>
    /// Counterpart of the beacon, searches for beacons
    /// </summary>
    /// <remarks>
    /// The beacon list event will not be raised on your main thread!
    /// </remarks>
    public class Probe : IDisposable
    {

        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Remove beacons older than this
        /// </summary>
        private static readonly TimeSpan BeaconTimeout = new TimeSpan(0, 0, 0, 10); // seconds

        public event Action<IEnumerable<BeaconLocation>> BeaconsUpdated;

        private readonly Thread thread;
        private readonly EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
        private readonly UdpClient udp = new UdpClient();
        private IEnumerable<BeaconLocation> currentBeacons = Enumerable.Empty<BeaconLocation>();

        private Dictionary<IPAddress, IPAddress> _localIPAddress;  //key:IP address   value:subnetmask
        private IPEndPoint sender = new IPEndPoint(0, 0);

        private bool running = true;

        internal const int DiscoveryPort = 35890;

        public Probe(string beaconType)
        {

            _localIPAddress = Utils.GetLocalIPAddress();

            udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            BeaconType = beaconType;
            thread = new Thread(BackgroundLoop) { IsBackground = true };

            //udp.Client.EnableBroadcast = true;
            //udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            //udp.Client.ExclusiveAddressUse = false;

            udp.Client.Bind(new IPEndPoint(IPAddress.Any, DiscoveryPort));
            try 
            {
                log.Trace("Enabling NAT Traversal");
                udp.AllowNatTraversal(true);
            }
            catch (Exception ex)
            {
                log.Debug(ex, "Error switching on NAT traversal: ");
            }

            udp.BeginReceive(ResponseReceived, null);
        }

        public void Start()
        {
            thread.Start();
        }

        private void ResponseReceived(IAsyncResult ar)
        {

            log.Trace("ResponseReceived Invoked!");

            var bytes = udp.EndReceive(ar, ref sender);

            var typeBytes = Beacon.Encode(BeaconType).ToList();
            log.Debug(string.Join(", ", typeBytes.Select(_ => (char)_)));
            if (Beacon.HasPrefix(bytes, typeBytes))
            {
                log.Trace("Beacon has prefix");
                try
                {

                    var portBytes = bytes.Skip(typeBytes.Count()).Take(2).ToArray();
                    var port      = (ushort)IPAddress.NetworkToHostOrder((short)BitConverter.ToUInt16(portBytes, 0));
                    var payload   = Beacon.Decode(bytes.Skip(typeBytes.Count() + 2));

                    log.Trace($"Port: {port}, Payload: {payload}");
                    NewBeacon(new BeaconLocation(new IPEndPoint(sender.Address, port), payload, DateTime.Now));

                }
                catch (Exception ex)
                {
                    log.Debug(ex);
                }
            }

            udp.BeginReceive(ResponseReceived, null);
        }

        public string BeaconType { get; private set; }

        private void BackgroundLoop()
        {
            while (running)
            {
                try
                {
                    BroadcastProbe();
                }
                catch (Exception ex)
                {
                    log.Debug(ex);
                }

                waitHandle.WaitOne(2000);
                PruneBeacons();
            }
        }

        private void BroadcastProbe()
        {
            var probe = Beacon.Encode(BeaconType).ToArray();
            Utils.BroadCastOnAllInterfaces(ref _localIPAddress, ref probe, BroadcastWay.Client);
        }

        private void PruneBeacons()
        {
            var cutOff = DateTime.Now - BeaconTimeout;
            var oldBeacons = currentBeacons.ToList();
            var newBeacons = oldBeacons.Where(_ => _.LastAdvertised >= cutOff).ToList();
            if (EnumsEqual(oldBeacons, newBeacons)) 
            {
                log.Trace("new beacon is same as old, nothing to do");
                return;
            }
            var u = BeaconsUpdated;
            if (u != null) u(newBeacons);
            currentBeacons = newBeacons;
        }

        private void NewBeacon(BeaconLocation newBeacon)
        {
            var newBeacons = currentBeacons
                .Where(_ => !_.Equals(newBeacon))
                .Concat(new [] { newBeacon })
                .OrderBy(_ => _.Data)
                .ThenBy(_ => _.Address, IPEndPointComparer.Instance)
                .ToList();
            var u = BeaconsUpdated;
            if (u != null) u(newBeacons);
            currentBeacons = newBeacons;
        }

        private static bool EnumsEqual<T>(IEnumerable<T> xs, IEnumerable<T> ys)
        {
            return xs.Zip(ys, (x, y) => x.Equals(y)).Count() == xs.Count();
        }

        public void Stop()
        {
            running = false;
            waitHandle.Set();
            thread.Join();
        }

        public void Dispose()
        {
            try
            {
                Stop();
            }
            catch (Exception ex)
            {
                log.Debug(ex);
            }
        }
    }
}
