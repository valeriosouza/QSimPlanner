﻿using QSP.RouteFinding.Airports;
using QSP.RouteFinding.AirwayStructure;
using QSP.RouteFinding.Communication;
using QSP.RouteFinding.Routes.TrackInUse;
using QSP.RouteFinding.Tracks.Ausots;
using QSP.RouteFinding.Tracks.Interaction;
using QSP.RouteFinding.Tracks.Nats;
using QSP.RouteFinding.Tracks.Pacots;
using static QSP.RouteFinding.RouteFindingCore;

namespace QSP.RouteFinding.Tracks.Common
{
    public static class ServiceInitializer
    {
        public static void Initailize(
            AirportManager airportList,
            WaypointList wptList)
        {
            TrackStatusRecorder = new StatusRecorder();
            TracksInUse = new TrackInUseCollection();
            RTCommunicator = new RouteTrackCommunicator(TracksInUse);

            NatsManager = new NatsHandler(new NatsDownloader(),
                                          wptList,
                                          wptList.GetEditor(),
                                          TrackStatusRecorder,
                                          airportList,
                                          TracksInUse);

            PacotsManager = new PacotsHandler(new PacotsDownloader(),
                                              wptList,
                                              wptList.GetEditor(),
                                              TrackStatusRecorder,
                                              airportList,
                                              TracksInUse);

            AusotsManager = new AusotsHandler(new AusotsDownloader(),
                                              wptList,
                                              wptList.GetEditor(),
                                              TrackStatusRecorder,
                                              airportList,
                                              TracksInUse);
            
        }        
    }
}
