﻿using QSP.RouteFinding.Airports;
using QSP.Utilities.Units;
using System.Collections.Generic;

namespace QSP.UI.Views.FuelPlan.Route
{
    public interface IFindAltnView
    {
        string ICAO { get; }
        string Length { get; }
        LengthUnit LengthUnit { get; }
        string SelectedIcao { get; }
        void ShowWarning(string msg);
        IReadOnlyList<AlternateFinder.AlternateInfo> Alternates { set; }
    }
}
