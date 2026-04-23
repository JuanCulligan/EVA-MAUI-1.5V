using Eva.Response;

namespace Eva.DTO
{
    public class ResGetVehicleDto : ResBase
    {
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public int Year { get; set; }
    }

    public class ReqSaveVehicleDto
    {
        public Guid guid { get; set; }
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Year { get; set; }
    }

    public class ReqUpdateVehicleDto
    {
        public Guid guid { get; set; }
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Year { get; set; }
    }
}
