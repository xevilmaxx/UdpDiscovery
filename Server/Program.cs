using BeaconLib;
using BeaconLib.DTO;
using BeaconLib.Helpers;
using BeaconLib.Interfaces;
using BeaconLib.LocalMachine;
using BeaconLib.RemoteMachine;
using Newtonsoft.Json;
using System;

namespace Server
{
    class Program
    {

        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            log.Info("Server");

            try
            {

                bool isLocal = false;

                //discover remote server
                var ownBeacon = new BeaconFactory().Get(isLocal, DiscoveryChamber.CustomChamber);

                ownBeacon.BeaconData = JsonConvert.SerializeObject(new DiscoveredData()
                {
                    Ip = LocalIP.GetLocalIp(),
                    Port = 50001,
                    GrpcEndpoint = LocalIP.GetLocalIp() + ":" + 50001
                });

                ownBeacon.Start();

                log.Debug("We are waiting for probes");

            }
            catch(Exception ex)
            {
                log.Error(ex);
            }

            log.Info("Press any key to exit");
            Console.ReadLine();
        }
    }
}
