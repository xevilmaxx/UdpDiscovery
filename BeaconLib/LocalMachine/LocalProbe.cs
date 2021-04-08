using BeaconLib.DTO;
using BeaconLib.Helpers;
using BeaconLib.Interfaces;
using BeaconLib.LocalMachine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace BeaconLib.LocalMachine
{
    /// <summary>
    /// Counterpart of the beacon, searches for beacons
    /// </summary>
    /// <remarks>
    /// The beacon list event will not be raised on your main thread!
    /// </remarks>
    public class LocalProbe : IDisposable, IProbe
    {

        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Remove beacons older than this
        /// </summary>
        private static readonly TimeSpan BeaconTimeout = new TimeSpan(0, 0, 0, 5); // seconds

        public event Action<IEnumerable<BeaconLocation>> BeaconsUpdated;

        private System.Timers.Timer ProbeTimer { get; set; }
        private static object _locker = new object();

        private readonly UdpClient udp = new UdpClient();
        private IEnumerable<BeaconLocation> currentBeacons = Enumerable.Empty<BeaconLocation>();

        private bool running = true;

        public LocalProbe(string beaconType)
        {
            BeaconType = beaconType;
            ProbeTimer = new System.Timers.Timer(2000);
            ProbeTimer.Elapsed += BackgroundLoop;
            ProbeTimer.AutoReset = true;
        }

        private void Init()
        {
            udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            udp.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
            try
            {
                udp.AllowNatTraversal(true);
            }
            catch (Exception ex)
            {
                log.Debug("Error switching on NAT traversal: " + ex.Message);
            }

            udp.BeginReceive(ResponseReceived, null);
        }

        public void Start()
        {
            running = true;
            Init();
            ProbeTimer?.Start();
            //start asap first time without waiting timer reset
            BackgroundLoop(null, null);
        }

        private void ResponseReceived(IAsyncResult ar)
        {

            log.Trace("ResponseReceived Invoked!");

            var remote = new IPEndPoint(IPAddress.Any, 0);
            var bytes = udp.EndReceive(ar, ref remote);

            var typeBytes = SharedMethods.Encode(BeaconType).ToList();
            log.Debug(string.Join(", ", typeBytes.Select(_ => (char)_)));
            if (SharedMethods.HasPrefix(bytes, typeBytes))
            {
                try
                {
                    log.Trace("Beacon has prefix");
                    var portBytes = bytes.Skip(typeBytes.Count()).Take(2).ToArray();
                    var port = (ushort)IPAddress.NetworkToHostOrder((short)BitConverter.ToUInt16(portBytes, 0));
                    var payload = SharedMethods.Decode(bytes.Skip(typeBytes.Count() + 2));
                    NewBeacon(new BeaconLocation(new IPEndPoint(remote.Address, port), payload, DateTime.Now));
                }
                catch (Exception ex)
                {
                    log.Debug(ex);
                }
            }

            if (running == true)
            {
                udp.BeginReceive(ResponseReceived, null);
            }

        }

        public string BeaconType { get; private set; }

        private void BackgroundLoop(object sender, System.Timers.ElapsedEventArgs e)
        {
            var hasLock = false;

            try
            {

                Monitor.TryEnter(_locker, ref hasLock);
                if (!hasLock)
                {
                    log.Trace("Concurrent Thread isnt finished, wait next time");
                    return;
                }

                log.Trace("Lock Acquired!");

                PruneBeacons();
                BroadcastProbe();

            }
            catch (Exception ex)
            {
                log.Debug(ex);
            }
            finally
            {
                if (hasLock)
                {
                    Monitor.Exit(_locker);
                    log.Trace("Lock Released");
                }
            }
        }

        private void BroadcastProbe()
        {
            var probe = SharedMethods.Encode(BeaconType).ToArray();
            udp.Send(probe, probe.Length, new IPEndPoint(IPAddress.Broadcast, LocalBeacon.DiscoveryPort));
        }

        private void PruneBeacons()
        {
            var cutOff = DateTime.Now - BeaconTimeout;
            var oldBeacons = currentBeacons.ToList();
            var newBeacons = oldBeacons.Where(_ => _.LastAdvertised >= cutOff).ToList();
            if (EnumsEqual(oldBeacons, newBeacons)) return;

            var u = BeaconsUpdated;
            if (u != null) u(newBeacons);
            currentBeacons = newBeacons;
        }

        private void NewBeacon(BeaconLocation newBeacon)
        {
            var newBeacons = currentBeacons
                .Where(_ => !_.Equals(newBeacon))
                .Concat(new[] { newBeacon })
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
            ProbeTimer?.Stop();
            udp?.Close();
            udp?.Dispose();
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
