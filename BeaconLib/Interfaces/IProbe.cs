using BeaconLib.DTO;
using System;
using System.Collections.Generic;
using System.Text;

namespace BeaconLib.Interfaces
{
    public interface IProbe
    {

        void Start();
        void Stop();

        event Action<IEnumerable<BeaconLocation>> BeaconsUpdated;

    }
}
