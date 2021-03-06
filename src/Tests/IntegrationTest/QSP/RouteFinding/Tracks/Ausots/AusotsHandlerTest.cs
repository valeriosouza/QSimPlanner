﻿using IntegrationTest.QSP.RouteFinding.TestSetup;
using NUnit.Framework;
using QSP.RouteFinding.Airports;
using QSP.RouteFinding.AirwayStructure;
using QSP.RouteFinding.Containers;
using QSP.RouteFinding.Data.Interfaces;
using QSP.RouteFinding.Routes.TrackInUse;
using QSP.RouteFinding.Tracks.Ausots;
using QSP.RouteFinding.Tracks.Common;
using QSP.RouteFinding.Tracks.Interaction;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IntegrationTest.QSP.RouteFinding.Tracks.Ausots
{
    [TestFixture]
    public class AusotsHandlerTest
    {
        [Test]
        public void GetAllTracksAndAddToWptListTest()
        {
            // Arrange
            var wptList = WptListFactory.GetWptList(wptIdents);
            AddAirways(wptList);

            var recorder = new StatusRecorder();

            var handler = new TrackHandler<AusTrack>(
                wptList,
                wptList.GetEditor(),
                GetAirportList(),
                new TrackInUseCollection());

            // Act
            handler.GetAllTracks(DownloaderStub(), recorder);
            handler.AddToWaypointList(recorder);

            // Assert
            Assert.AreEqual(0, recorder.Records.Count);

            // Verify all tracks are added.
            AssertAllTracks(wptList);

            // Check the tracks.
            AssertTrackMY14(wptList);
            AssertTrackBP14(wptList);
        }

        private static void AssertTrackMY14(WaypointList wptList)
        {
            var edge = wptList.GetEdge(GetEdgeIndex("AUSOTMY14", "JAMOR", wptList));

            // Distance
            var expectedDistance = new[]
            {
                "JAMOR",
                "IBABI",
                "LEC",
                "OOD",
                "ARNTU",
                "KEXIM",
                "CIN",
                "ATMAP"
            }.Select(id => wptList[wptList.FindById(id)]).TotalDistance();

            Assert.AreEqual(expectedDistance, edge.Value.Distance, 0.01);

            // Start, end waypoints are correct
            Assert.IsTrue(wptList[edge.FromNodeIndex].ID == "JAMOR");
            Assert.IsTrue(wptList[edge.ToNodeIndex].ID == "ATMAP");

            // Start, end waypoints are connected
            Assert.IsTrue(wptList.EdgesFromCount(edge.FromNodeIndex) > 0);
            Assert.IsTrue(wptList.EdgesToCount(edge.ToNodeIndex) > 0);

            // Airway is correct
            Assert.IsTrue(edge.Value.Airway == "AUSOTMY14");
        }

        private static void AssertTrackBP14(WaypointList wptList)
        {
            var edge = wptList.GetEdge(GetEdgeIndex("AUSOTBP14", "TAXEG", wptList));

            var expectedDistance = new[]
            {
                "TAXEG",
                "PASTA",
                "TAROR",
                "WR",
                "ENTRE",
                "MALLY",
                "NSM"
            }.Select(id => wptList[wptList.FindById(id)]).TotalDistance();

            // Distance
            Assert.AreEqual(expectedDistance, edge.Value.Distance, 0.01);

            // Start, end waypoints are correct
            Assert.IsTrue(wptList[edge.FromNodeIndex].ID == "TAXEG");
            Assert.IsTrue(wptList[edge.ToNodeIndex].ID == "NSM");

            // Start, end waypoints are connected
            Assert.IsTrue(wptList.EdgesFromCount(edge.FromNodeIndex) > 0);
            Assert.IsTrue(wptList.EdgesToCount(edge.ToNodeIndex) > 0);

            // Airway is correct
            Assert.IsTrue(edge.Value.Airway == "AUSOTBP14");
        }

        private static int GetEdgeIndex(string ID, string firstWpt, WaypointList wptList)
        {
            foreach (var i in wptList.EdgesFrom(wptList.FindById(firstWpt)))
            {
                if (wptList.GetEdge(i).Value.Airway == ID)
                {
                    return i;
                }
            }
            return -1;
        }

        private static void AssertAllTracks(WaypointList wptList)
        {
            var id = new[]
            {
                // Lines commented out are unavailable tracks.

                "MY14",
                "SY14",
             // "SK14",             
             // "SX14",                
                "BP14"
            };

            var firstWpt = new[]
            {
                "JAMOR",
                "PKS",
                "TAXEG"
            };

            for (int i = 0; i < id.Length; i++)
            {
                AssertTrack("AUSOT" + id[i], firstWpt[i], wptList);
            }
        }

        private static void AssertTrack(string ID, string firstWpt, WaypointList wptList)
        {
            // check the track is added
            if (GetEdgeIndex(ID, firstWpt, wptList) < 0)
            {
                Assert.Fail("Track not found.");
            }
        }

        private static readonly IReadOnlyList<string> wptIdents = new[]
        {
            "JAMOR",
            "IBABI",
            "LEC",
            "OOD",
            "ARNTU",
            "KEXIM",
            "CIN",
            "ATMAP",
            "PKS",
            "KADUV",
            "TAROR",
            "DANAL",
            "AS",
            "DEENO",
            "SANDY",
            "ATMAP",
            "TAXEG",
            "PASTA",
            "WR",
            "ENTRE",
            "MALLY",
            "NSM"
        }.Distinct().ToList();

        private static readonly IReadOnlyList<string> airports = new[]
        {
            "YMML",
            "YSSY",
            "YBBN",
            "YPPH"
        };

        private static AirportManager GetAirportList()
        {
            var collection = airports.Select(i => Mocks.GetAirport(i));
            return new AirportManager(collection);
        }

        private static readonly IReadOnlyList<airwayEntry> airwayEntries = new[]
        {
            new airwayEntry("ML", "H164", "JAMOR"),
            new airwayEntry("TESAT", "H44", "KAT"),
            new airwayEntry("KAT", "A576", "PKS"),
            new airwayEntry("BN", "H62", "LAV"),
            new airwayEntry("LAV", "Q116", "TAXEG"),
            new airwayEntry("NSM", "Q10", "HAMTN"),
            new airwayEntry("HAMTN", "Q158", "PH")
        };

        private static int TryAddWpt(WaypointList wptList, string id)
        {
            int x = wptList.FindById(id);

            if (x < 0)
            {
                var rd = new Random(123);
                return wptList.AddWaypoint(new Waypoint(id, rd.Next(-90, 91), rd.Next(-180, 181)));
            }

            return x;
        }

        private static void AddAirways(WaypointList wptList)
        {
            foreach (var i in airwayEntries)
            {
                int x = TryAddWpt(wptList, i.StartWpt);
                int y = TryAddWpt(wptList, i.EndWpt);
                var neighbor = new Neighbor(i.Airway, wptList.Distance(x, y));

                wptList.AddNeighbor(x, y, neighbor);
            }
        }

        private class airwayEntry
        {
            public readonly string StartWpt;
            public readonly string Airway;
            public readonly string EndWpt;

            public airwayEntry(string StartWpt, string Airway, string EndWpt)
            {
                this.StartWpt = StartWpt;
                this.Airway = Airway;
                this.EndWpt = EndWpt;
            }
        }

        private ITrackMessageProvider DownloaderStub()
        {
            var directory = AppDomain.CurrentDomain.BaseDirectory;

            var msg = new AusotsMessage(
                File.ReadAllText(directory + "/QSP/RouteFinding/Tracks/Ausots/text.asp.html"));
            return new TrackMessageProvider(msg);
        }
    }
}
