# FORK OF:
https://github.com/rix0rrr/beacon


# Why this fork:
---
# Discovery of Beacon on remote PC, assuming that both Probe and Beacons may have more than 1 interface available.
---
# Not meant to be used to discover services on same PC, and may not give you desired result as broadcasts on multiple interfaces may result in multiple captures.

---
# Features
- [x] Multiple adapters support (Multi Network)
- [x] **Helping function to get your Machine public IP**
- [x] Simplified communication by chamber concept (so you cannot process beacons and probes arriving from other chambers)
- [x] Easy samples
- [x] Net 5.0
- [x] Cross platform (**Windows / Linux / RaspberryPI**) maybe even MAC 
- [x] UDP broadcasting
- [x] Probes and Beacons are listening on fixed ports (35890 [Probe], 35891 [Beacon])
---

# Beacon: automatic network discovery

*Beacon* is a small C# library that helps remove some of the user annoyances involved in writing client-server applications: finding the address of the server to connect to (because messing around putting in IP addresses is so much fun, eh?)

In that sense, it fills the same niche as other tools like Apple's *Bonjour*, but with minimal dependencies.

## How it works

UDP broadcasts, what else did you expect? :) It doesn't work outside your own network range without an external server (support for something like this could be added...!) but inside one network it works quite well.

The current Beacon implementation has support for local advertising based on application type (so different programs both using Beacon don't interfere with each other), and advertising server-specific data, which can be used to distinguish different servers from each other (for example, by presenting a display name to the user).

## Starting a Beacon server

Starting a beacon (to make a server discoverable) is simple:

    var beacon = new Beacon("myApp", 1234);
    beacon.BeaconData = "My Application Server on " + Dns.GetHostName();
    beacon.Start();

    // ...

    beacon.Stop();

`1234` is the port number that you want to advertise to clients. Typically, this is the port clients should connect to for your actual network service. You can fill in a bogus value if you don't need it.

## Scanning for servers using a Probe

Clients can find beacons using a `Probe`:

    var probe = new Probe("myApp");
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

        }
        catch (Exception ex)
        {
            log.Error(ex);
        }
    };

    probe.Start();
    
    // ...

    probe.Stop();
