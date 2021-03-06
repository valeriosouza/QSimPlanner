﻿using System.Collections.Generic;
using System.Linq;

namespace QSP.LandingPerfCalculation.Airbus
{
    public class AirbusPerfTable : IPerfTableItem
    {
        public List<Entry> Entries { get; set; }
        public double Multiplier { get; set; } = 1;

        public bool Equals(AirbusPerfTable a, double delta)
        {
            return Entries != null && a.Entries != null &&
                Entries.Count == a.Entries.Count &&
                Entries.All(x => a.Entries.Any(y => x.Equals(y, delta)));
        }
    }
}
