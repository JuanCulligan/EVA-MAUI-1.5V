using System.Collections.Generic;

namespace Eva.DTO
{
    public class ChargerStationDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Address { get; set; } = string.Empty;
        public string OperationalStatus { get; set; } = string.Empty;
        public string UsageType { get; set; } = string.Empty;

        public List<ChargerConnectionDto> Connections { get; set; } = new List<ChargerConnectionDto>();
    }
}
