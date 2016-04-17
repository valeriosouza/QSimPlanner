﻿namespace QSP.AircraftProfiles
{
    public class AircraftConfig
    {
        public string AC { get; private set; }
        public string Registration { get; private set; }
        public string TOProfile { get; private set; }
        public string LdgProfile { get; private set; }
        public double ZfwKg { get; private set; }
        public double MaxTOWtKg { get; private set; }
        public double MaxLdgWtKg { get; private set; }
        public WeightUnit WtUnit { get; private set; }

        public AircraftConfig(
            string AC,
            string Registration,
            string TOProfile,
            string LdgProfile,
            double ZfwKg,
            double MaxTOWtKg,
            double MaxLdgWtKg,
            WeightUnit WtUnit)
        {
            this.AC = AC;
            this.Registration = Registration;
            this.TOProfile = TOProfile;
            this.LdgProfile = LdgProfile;
            this.ZfwKg = ZfwKg;
            this.MaxTOWtKg = MaxTOWtKg;
            this.MaxLdgWtKg = MaxLdgWtKg;
            this.WtUnit = WtUnit;
        }
    }
}