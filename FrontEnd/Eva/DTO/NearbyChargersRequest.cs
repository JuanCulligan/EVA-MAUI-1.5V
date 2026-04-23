namespace Eva.DTO
{
    public class NearbyChargersRequest
    {
        public double Lat { get; set; }
        public double Lon { get; set; }

        public int RadiusKm { get; set; } = 8;
    }
}
