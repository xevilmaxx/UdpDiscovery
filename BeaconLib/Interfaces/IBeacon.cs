using System;
using System.Collections.Generic;
using System.Text;

namespace BeaconLib.Interfaces
{
    public interface IBeacon
    {
        /// <summary>
        /// Start Listening for Probes
        /// </summary>
        void Start();

        /// <summary>
        /// Stop Listening for Probes
        /// </summary>
        void Stop();

        string BeaconType { get; }
        string BeaconData { get; set; }

    }
}
