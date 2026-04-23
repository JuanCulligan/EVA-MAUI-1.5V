namespace Eva.Entidades
{
    public class SavedPlace
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = string.Empty;
        public string Kind { get; set; } = "Otro";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? AddressNote { get; set; }
    }
}
