using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;


namespace BeaconLib.Helpers
{
    public static class LocalIP
    {

        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        //better way to improve performance for using correct algorythm to detect local Ip
        private static ushort CurGetIpMode { get; set; } = 0;

        /// <summary>
        /// Gives your ip on most public interface of all
        /// </summary>
        /// <returns></returns>
        public static string GetLocalIp()
        {

            string basicIp = "127.0.0.1";
            string localIP = basicIp;

            //select appropriate mode
            if (CurGetIpMode == 0)
            {

                if(IsNotWindowsOS() == true)
                {
                    //other 2 ways anyway wont work on linux, at least for now
                    //just to avoid useless errors spam
                    //and differentiate OS management
                    log.Debug("Linux detected, will perform only basic check");
                    localIP = BasicWayOfGettingLocalIp();
                    if (localIP.Equals(basicIp))
                    {
                        log.Warn("Wasnt able to get public IP, will use default: " + basicIp);
                        CurGetIpMode = 4;
                    }
                    else
                    {
                        CurGetIpMode = 2;
                    }
                }
                else
                {
                    log.Debug("Windows detected, more ways to obtain ip are available");
                    //we have windows so more tries can be done
                    localIP = GetMainLocalIpAddress();
                    if (localIP.Equals(basicIp))
                    {

                        //do second try
                        log.Debug("Performing Second Check");

                        localIP = BasicWayOfGettingLocalIp();
                        if (localIP.Equals(basicIp))
                        {

                            log.Debug("Performing Third Check");
                            localIP = GetMainLocalIpAddress(false);

                            if (!localIP.Equals(basicIp))
                            {
                                CurGetIpMode = 3;
                            }
                            else
                            {
                                //give default
                                CurGetIpMode = 4;
                            }

                        }
                        else
                        {
                            CurGetIpMode = 2;
                        }

                        if (localIP.Equals(basicIp))
                        {
                            log.Error("Wasnt able to find adequate Ip to use!!!");
                            log.Error("This is prerequisite!!!");
                            log.Info("Forcibly will use 127.0.0.1");
                        }

                    }
                    else
                    {
                        CurGetIpMode = 1;
                    }
                }

                log.Debug("CurGetIpMode: " + CurGetIpMode);

            }
            else if (CurGetIpMode == 1)
            {
                localIP = GetMainLocalIpAddress(ConsiderJustThoseWithGateway: true, IsCanLog: false);
            }
            else if (CurGetIpMode == 2)
            {
                localIP = BasicWayOfGettingLocalIp(IsCanLog: false);
            }
            else if (CurGetIpMode == 3)
            {
                localIP = GetMainLocalIpAddress(ConsiderJustThoseWithGateway: false, IsCanLog: false);
            }
            else if (CurGetIpMode == 4)
            {
                //leave at default
            }

            log.Debug($"SELF IP: {localIP}");

            return localIP;

        }

        private static bool IsNotWindowsOS()
        {
            bool res = true;
            var isWindows = Environment.Is64BitOperatingSystem == true && Environment.OSVersion.Platform == PlatformID.Win32NT;
            if (isWindows)
            {
                log.Error("This Peripheral Control is meant to be executed on Raspberry, with Linux OS");
                res = isWindows == true ? false : true;
            }
            else
            {
                log.Debug("OS is linux, ok.");
            }
            return res;
        }

        public static string BasicWayOfGettingLocalIp(bool IsCanLog = true)
        {

            string localIP = "127.0.0.1";

            try
            {

                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {

                    socket.Connect("8.8.8.8", 65530);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    localIP = endPoint.Address.ToString();

                    socket?.Close();
                    socket?.Dispose();

                }
                CustomLog("Local ip: " + localIP, IsCanLog);

            }
            catch (Exception ex)
            {
                log.Error(ex);
            }

            return localIP;

        }

        /// <summary>
        /// Seems to work only on windows for now, becouse you cant check property IsEligible
        /// </summary>
        /// <param name="ConsiderJustThoseWithGateway"></param>
        /// <param name="IsCanLog"></param>
        /// <returns></returns>
        public static string GetMainLocalIpAddress(bool ConsiderJustThoseWithGateway = true, bool IsCanLog = true)
        {

            try
            {


                UnicastIPAddressInformation mostSuitableIp = null;

                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

                foreach (var network in networkInterfaces)
                {

                    CustomLog("Analyzing Interface: " + network.Name, IsCanLog);

                    if (network.OperationalStatus != OperationalStatus.Up)
                    {
                        CustomLog("Interface is probably down, will skip", IsCanLog);
                        continue;
                    }

                    var properties = network.GetIPProperties();

                    if (ConsiderJustThoseWithGateway == true)
                    {
                        if (properties.GatewayAddresses.Count == 0)
                        {
                            CustomLog("No gateway addresses found for this interface, will skip", IsCanLog);
                            continue;
                        }
                    }

                    foreach (var address in properties.UnicastAddresses)
                    {
                        if (address.Address.AddressFamily != AddressFamily.InterNetwork)
                        {
                            CustomLog("Is Not InterNetwork", IsCanLog);
                            continue;
                        }

                        if (IPAddress.IsLoopback(address.Address))
                        {
                            CustomLog("Is Not Loopback", IsCanLog);
                            continue;
                        }

                        if (!address.IsDnsEligible)
                        {
                            CustomLog("Is not DNS eligible, will perform further check", IsCanLog);
                            if (mostSuitableIp == null)
                            {
                                mostSuitableIp = address;
                            }
                            continue;
                        }
                        else
                        {
                            CustomLog("Is DNS eligible", IsCanLog);
                        }

                        //Chose those who are NOT in DHCP
                        if (address.PrefixOrigin != PrefixOrigin.Dhcp)
                        {
                            CustomLog("Interface is Not in DHCP", IsCanLog);
                            if (mostSuitableIp == null || !mostSuitableIp.IsDnsEligible)
                            {
                                mostSuitableIp = address;
                            }
                            continue;
                        }
                        else
                        {
                            CustomLog("Interface is in DHCP", IsCanLog);
                        }

                        //seems if you got here, found address satisfies our requirements
                        var goodResult = address.Address.ToString();
                        CustomLog($"Found Ip: {goodResult}", IsCanLog);

                        return goodResult;

                    }
                }

                var answer = mostSuitableIp != null
                    ? mostSuitableIp.Address.ToString()
                    : "127.0.0.1";

                CustomLog($"IP: {answer}", IsCanLog);

                return answer;

            }
            catch (Exception ex)
            {
                log.Error(ex);
                return "127.0.0.1";
            }

        }

        private static void CustomLog(string Message, bool IsCanLog = true)
        {
            try
            {
                if (IsCanLog == true)
                {
                    log.Debug(Message);
                }
            }
            catch(Exception ex)
            {
                log.Error(ex);
            }
        }

    }
}
