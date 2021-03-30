using System;
using System.Collections.Generic;
using System.Text;

namespace BeaconLib.Interfaces
{
    public interface IBeacon
    {
        void Start();
        void Stop();

        string BeaconType { get; }
        string BeaconData { get; set; }

    }
}
