using BeaconLib.Interfaces;
using BeaconLib.LocalMachine;
using BeaconLib.RemoteMachine;
using System;
using System.Collections.Generic;
using System.Text;

namespace BeaconLib
{
    public class BeaconFactory
    {
        public IBeacon Get(bool IsLocal, string Chamber)
        {
            if (IsLocal == true)
            {
                return new LocalBeacon(Chamber);
            }
            else
            {
                return new RemoteBeacon(Chamber);
            }
        }
    }
}
