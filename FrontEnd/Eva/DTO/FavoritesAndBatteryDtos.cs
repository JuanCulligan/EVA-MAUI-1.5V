using Eva.Response;

namespace Eva.DTO
{
    public class FavoriteLocationDto
    {
        public string? locationName { get; set; }
        public double lat { get; set; }
        public double lon { get; set; }
    }

    public class ReqSaveFavoriteDto
    {
        public Guid guid { get; set; }
        public string locationName { get; set; } = string.Empty;
        public double lat { get; set; }
        public double lon { get; set; }
    }

    public class ReqDeleteFavoriteDto
    {
        public Guid guid { get; set; }
        public string locationName { get; set; } = string.Empty;
        public double lat { get; set; }
        public double lon { get; set; }
    }

    public class BatteryEstimationRequestDto
    {
        public Guid guid { get; set; }
        public double StartLat { get; set; }
        public double StartLon { get; set; }
        public double EndLat { get; set; }
        public double EndLon { get; set; }
        public double SpeedKmh { get; set; } = 50;
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Year { get; set; }
    }

    public class BatteryEstimationResponseDto : ResBase
    {
        public double TotalEnergyWh { get; set; }
        public double PercentUsed { get; set; }
    }

    public class ResGetUserApiDto : ResBase
    {
        public UserProfileApiDto? user { get; set; }
    }

    public class UserProfileApiDto
    {
        public Guid guid { get; set; }
        public string? Name { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Adress { get; set; }
        public string? chargerType { get; set; }
    }
}
