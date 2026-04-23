namespace Eva.Services
{
    public static class TripEstimatePreferences
    {
        const string KeyWh = "eva_trip_wh_per_km";
        const string KeyCrc = "eva_trip_crc_per_kwh";

        public static double GetWhPerKm()
        {
            double d = Preferences.Get(KeyWh, 165.0);
            return d is >= 30 and <= 600 ? d : 165.0;
        }

        public static void SetWhPerKm(double value)
        {
            if (value is >= 30 and <= 600)
            {
                Preferences.Set(KeyWh, value);
            }
        }

        public static double GetCrcPerKwh()
        {
            double d = Preferences.Get(KeyCrc, 95.0);
            return d >= 0 ? d : 95.0;
        }

        public static void SetCrcPerKwh(double value)
        {
            if (value >= 0)
            {
                Preferences.Set(KeyCrc, value);
            }
        }
    }
}
