using BeaconLib.Interfaces;
using BeaconLib.LocalMachine;
using BeaconLib.RemoteMachine;
using System;
using System.Collections.Generic;
using System.Text;

namespace BeaconLib
{
    public class ProbeFactory
    {
        public IProbe Get(bool IsLocal, string Chamber)
        {
            if (IsLocal == true)
            {
                return new LocalProbe(Chamber);
            }
            else
            {
                return new RemoteProbe(Chamber);
            }
        }
    }
}
