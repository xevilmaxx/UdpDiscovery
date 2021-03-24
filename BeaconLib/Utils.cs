using BeaconLib.DTO;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace BeaconLib
{
    public static class Utils
    {

        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        //for each network interface
        public static Dictionary<IPAddress, IPAddress> GetLocalIPAddress()
        {
            Dictionary<IPAddress, IPAddress> localIp = new Dictionary<IPAddress, IPAddress>();
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            localIp.Add(ip.Address, ip.IPv4Mask);
                        }
                    }
                }
            }
            return localIp;
        }

        public static void BroadCastOnAllInterfaces(ref Dictionary<IPAddress, IPAddress> interfaces, ref byte[] data, BroadcastWay broadcastWay)
        {
            try
            {
                IPEndPoint BroadcastEndpoint = null;

                log.Trace($"BroadCastOnAllInterfaces with way: {broadcastWay}");

                //port is important, dont set it to 0,
                //must match between exposer and consumer
                if(broadcastWay == BroadcastWay.Client)
                {
                    BroadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, Beacon.DiscoveryPort);
                    log.Trace($"Will broadcast on port: {Beacon.DiscoveryPort}");
                }
                else
                {
                    BroadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, Probe.DiscoveryPort);
                    log.Trace($"Will broadcast on port: {Probe.DiscoveryPort}");
                }

                foreach (var ip in interfaces.Keys) // send the message to each network adapters
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        UdpClient clientInterface = null;
                        try
                        {
                            log.Trace($"BroadcastProbe Invoked! To: {ip}");
                            clientInterface = new UdpClient(new IPEndPoint(ip, 0));
                            //if you dont dont enable broadcast effects will be only visible in local machine
                            clientInterface.EnableBroadcast = true;

                            //when setted to false discovery not working strangely
                            //by default its already true
                            //clientInterface.ExclusiveAddressUse = true;

                            clientInterface.Send(data, data.Length, BroadcastEndpoint);
                        }
                        catch (Exception e)
                        {
                            log.Error("Unable to send");
                        }
                        finally
                        {
                            clientInterface?.Close();
                        }
                    }
                }

            }
            catch(Exception ex)
            {
                log.Error(ex);
            }
        }

    }
}
