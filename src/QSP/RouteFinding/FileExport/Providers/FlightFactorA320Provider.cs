﻿namespace QSP.RouteFinding.FileExport.Providers
{
    public static class FlightFactorA320Provider
    {
        /// <summary>
        /// Get string of the flight plan to export.
        /// </summary>
        /// <exception cref="Exception"></exception>
        public static string GetExportText(ExportInput input)
        {
            var route = input.Route;
            var from = route.FirstWaypoint.ID.Substring(0, 4);
            var to = route.LastWaypoint.ID.Substring(0, 4);
            return $"RTE {from}{to}01 " + JarDesignAirbusProvider.GetExportText(input);
        }
    }
}
