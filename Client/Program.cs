using BeaconLib;
using BeaconLib.DTO;
using BeaconLib.Helpers;
using Newtonsoft.Json;
using System;
using System.Linq;

namespace Client
{
    class Program
    {

        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            log.Info("Server");

            try
            {

                //discover remote server
                var probe = new Probe(DiscoveryChamber.CustomChamber);
                // Event is raised on separate thread so need synchronization
                probe.BeaconsUpdated += beacons =>
                {

                    try
                    {

                        //normally this should be only 1
                        foreach (var beacon in beacons)
                        {
                            log.Debug(beacon.Address + ": " + beacon.Data);
                        }

                        //stop after finding first set of beacons
                        probe.Stop();

                        var data = JsonConvert.DeserializeObject<DiscoveredData>(beacons.First().Data);

                        //fire event
                        //OnServerFound?.Invoke(this, data);

                    }
                    catch (Exception ex)
                    {
                        log.Error(ex);
                    }

                };

                probe.Start();

            }
            catch (Exception ex)
            {
                log.Error(ex);
            }

            log.Info("Press any key to exit");
            Console.ReadLine();
        }
    }
}
