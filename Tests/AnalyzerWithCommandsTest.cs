using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QSP.RouteFinding;
using QSP.RouteFinding.Containers;
using static Tests.Common.Utilities;
using QSP.RouteFinding.TerminalProcedures.Sid;
using QSP.RouteFinding.TerminalProcedures.Star;
using QSP.RouteFinding.Routes;

namespace Tests
{
    [TestClass()]
    public class AnalyzerWithCommandsTest
    {
        // TODO: Disabled currently.

        //#region "Auto command test"

        //[TestMethod()]
        //public void AutoCommandMiddleTest()
        //{
        //    PrepareTest();

        //    const string orig = "RCTP";
        //    const string dest = "RJAA";

        //    string origRwy = RouteFindingCore.AirportList.RwyIdentList(orig).First();
        //    string destRwy = RouteFindingCore.AirportList.RwyIdentList(dest).Last();

        //    var genRoute = new RouteFinder().FindRoute(orig, origRwy, new SidHandler(orig).GetSidList(origRwy), dest, destRwy, new StarHandler(dest).GetStarList(destRwy));

        //    string rte = genRoute.ToString(false, false);
        //    Debug.WriteLine("Generated route: " + rte);

        //    genRoute.Via[Math.Min(4, genRoute.Via.Count - 1)] = "AUTO";

        //    rte = genRoute.ToString(false, false);
        //    Debug.WriteLine("Route with \"AUTO\": " + rte);
        //    AnalyzerWithCommands commandAnalyzer = new AnalyzerWithCommands(orig, origRwy, dest, destRwy, genRoute.ToString(false, false));

        //    var result = commandAnalyzer.Parse();

        //    Debug.WriteLine("Parsed route: " + result.ToString());

        //}

        //[TestMethod()]
        //public void AutoCommandFirstTest_SidCase2()
        //{
        //    PrepareTest();

        //    const string orig = "RCTP";
        //    const string dest = "RJAA";

        //    string origRwy = RouteFindingCore.AirportList.RwyIdentList(orig).First();
        //    string destRwy = RouteFindingCore.AirportList.RwyIdentList(dest).Last();

        //    var genRoute = new RouteFinder().FindRoute(orig, origRwy, new SidHandler(orig).GetSidList(origRwy), dest, destRwy, new StarHandler(dest).GetStarList(destRwy));

        //    string rte = genRoute.ToString(false, false);
        //    Debug.WriteLine("Generated route: " + rte);

        //    genRoute.Via[0] = "AUTO";

        //    rte = genRoute.ToString(false, false);
        //    Debug.WriteLine("Route with \"AUTO\": " + rte);
        //    AnalyzerWithCommands commandAnalyzer = new AnalyzerWithCommands(orig, origRwy, dest, destRwy, genRoute.ToString(false, false));

        //    var result = commandAnalyzer.Parse();

        //    Debug.WriteLine("Parsed route: " + result.ToString());

        //}

        //[TestMethod()]
        //public void AutoCommandFirstTest_SidCase1()
        //{
        //    PrepareTest();

        //    const string orig = "KORD";
        //    const string dest = "KSEA";

        //    string origRwy = RouteFindingCore.AirportList.RwyIdentList(orig).First();
        //    string destRwy = RouteFindingCore.AirportList.RwyIdentList(dest).Last();

        //    var genRoute = new RouteFinder().FindRoute(orig, origRwy, new SidHandler(orig).GetSidList(origRwy), dest, destRwy, new StarHandler(dest).GetStarList(destRwy));

        //    string rte = genRoute.ToString(false, false);
        //    Debug.WriteLine("Generated route: " + rte);

        //    genRoute.Via[0] = "AUTO";

        //    rte = genRoute.ToString(false, false);
        //    Debug.WriteLine("Route with \"AUTO\": " + rte);
        //    AnalyzerWithCommands commandAnalyzer = new AnalyzerWithCommands(orig, origRwy, dest, destRwy, genRoute.ToString(false, false));

        //    var result = commandAnalyzer.Parse();

        //    Debug.WriteLine("Parsed route: " + result.ToString());

        //}

        //[TestMethod()]
        //public void AutoCommandLastTest_StarCase3()
        //{
        //    PrepareTest();

        //    const string orig = "ZSPD";
        //    const string dest = "VHHH";

        //    string origRwy = RouteFindingCore.AirportList.RwyIdentList(orig).First();
        //    string destRwy = RouteFindingCore.AirportList.RwyIdentList(dest).Last();

        //    var genRoute = new RouteFinder().FindRoute(orig, origRwy, new SidHandler(orig).GetSidList(origRwy), dest, destRwy, new StarHandler(dest).GetStarList(destRwy));

        //    string rte = genRoute.ToString(false, false);
        //    Debug.WriteLine("Generated route: " + rte);

        //    genRoute.Via[genRoute.Via.Count - 1] = "AUTO";

        //    rte = genRoute.ToString(false, false);
        //    Debug.WriteLine("Route with \"AUTO\": " + rte);
        //    AnalyzerWithCommands commandAnalyzer = new AnalyzerWithCommands(orig, origRwy, dest, destRwy, genRoute.ToString(false, false));

        //    var result = commandAnalyzer.Parse();

        //    Debug.WriteLine("Parsed route: " + result.ToString());

        //}

        //[TestMethod()]
        //public void AutoCommandFirstLastTest_SidCase4_StarCase1()
        //{
        //    PrepareTest();

        //    const string orig = "VHHH";
        //    const string dest = "RCYU";

        //    string origRwy = RouteFindingCore.AirportList.RwyIdentList(orig).First();
        //    string destRwy = RouteFindingCore.AirportList.RwyIdentList(dest).First();

        //    var genRoute = new RouteFinder().FindRoute(orig, origRwy, new SidHandler(orig).GetSidList(origRwy), dest, destRwy, new StarHandler(dest).GetStarList(destRwy));

        //    string rte = genRoute.ToString(false, false);
        //    Debug.WriteLine("Generated route: " + rte);

        //    genRoute.Via[0] = "AUTO";
        //    genRoute.Via[genRoute.Via.Count - 1] = "AUTO";

        //    rte = genRoute.ToString(false, false);
        //    Debug.WriteLine("Route with \"AUTO\": " + rte);
        //    AnalyzerWithCommands commandAnalyzer = new AnalyzerWithCommands(orig, origRwy, dest, destRwy, genRoute.ToString(false, false));

        //    var result = commandAnalyzer.Parse();

        //    Debug.WriteLine("Parsed route: " + result.ToString());

        //}

        //[TestMethod()]
        //public void AutoCommandFirstLastTest_SidCase3_StarCase2()
        //{
        //    PrepareTest();

        //    const string orig = "KBOS";
        //    const string dest = "KSFO";

        //    string origRwy = RouteFindingCore.AirportList.RwyIdentList(orig)[6];
        //    string destRwy = RouteFindingCore.AirportList.RwyIdentList(dest).First();
        //    var sidList = new List<string>();
        //    sidList.Add(new SidHandler(orig).GetSidList(origRwy)[0]);

        //    var genRoute = new RouteFinder().FindRoute(orig, origRwy, sidList, dest, destRwy, new StarHandler(dest).GetStarList(destRwy));

        //    string rte = genRoute.ToString(false, false);
        //    Debug.WriteLine("Generated route: " + rte);

        //    genRoute.Via[0] = "AUTO";
        //    genRoute.Via[genRoute.Via.Count - 1] = "AUTO";

        //    rte = genRoute.ToString(false, false);
        //    Debug.WriteLine("Route with \"AUTO\": " + rte);
        //    AnalyzerWithCommands commandAnalyzer = new AnalyzerWithCommands(orig, origRwy, dest, destRwy, genRoute.ToString(false, false));

        //    var result = commandAnalyzer.Parse();

        //    Debug.WriteLine("Parsed route: " + result.ToString());

        //}

        //#endregion

        //#region "Rand Command Test"

        //[TestMethod()]
        //public void RandCommandMiddleTest()
        //{
        //    PrepareTest();

        //    const string orig = "KLAX";
        //    const string dest = "ZSPD";

        //    string origRwy = RouteFindingCore.AirportList.RwyIdentList(orig).First();
        //    string destRwy = RouteFindingCore.AirportList.RwyIdentList(dest).Last();

        //    var genRoute = new RouteFinder().FindRoute(orig, origRwy, new SidHandler(orig).GetSidList(origRwy), dest, destRwy, new StarHandler(dest).GetStarList(destRwy));

        //    string rte = genRoute.ToString(false, false);
        //    Debug.WriteLine("Generated route: " + rte);

        //    genRouteRand(genRoute);

        //    rte = genRoute.ToString(false, false);
        //    Debug.WriteLine("Route with \"RAND\": " + rte);
        //    AnalyzerWithCommands commandAnalyzer = new AnalyzerWithCommands(orig, origRwy, dest, destRwy, genRoute.ToString(false, false));

        //    var result = commandAnalyzer.Parse();

        //    Debug.WriteLine("Parsed route: " + result.ToString());

        //}


        //private void genRouteRand(Route oldRte)
        //{

        //    for (int i = oldRte.Waypoints.Count - 5; i >= 3; i--)
        //    {
        //        oldRte.Waypoints.RemoveAt(i + 1);
        //        oldRte.Via.RemoveAt(i);
        //    }

        //    oldRte.Via[3] = "RAND";

        //}

        //[TestMethod()]
        //public void RandCommandFirstLastTest()
        //{
        //    PrepareTest();

        //    const string orig = "KBOS";
        //    const string dest = "KSFO";

        //    string origRwy = RouteFindingCore.AirportList.RwyIdentList(orig).First();
        //    string destRwy = RouteFindingCore.AirportList.RwyIdentList(dest).First();

        //    var genRoute = new RouteFinder().FindRoute(orig, origRwy, new SidHandler(orig).GetSidList(origRwy), dest, destRwy, new StarHandler(dest).GetStarList(destRwy));

        //    string rte = genRoute.ToString(false, false);
        //    Debug.WriteLine("Generated route: " + rte);

        //    genRoute.Via[0] = "RAND";
        //    genRoute.Via[genRoute.Via.Count - 1] = "RAND";

        //    rte = genRoute.ToString(false, false);
        //    Debug.WriteLine("Route with \"RAND\": " + rte);
        //    AnalyzerWithCommands commandAnalyzer = new AnalyzerWithCommands(orig, origRwy, dest, destRwy, genRoute.ToString(false, false));

        //    var result = commandAnalyzer.Parse();

        //    Debug.WriteLine("Parsed route: " + result.ToString());

        //}

        //#endregion

    }
}