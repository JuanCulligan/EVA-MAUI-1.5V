using System;
using System.Collections.Generic;

namespace Eva.DTO
{
    public class ResGetRecentLocationsDto
    {
        public bool Result { get; set; }
        public List<LocationRowDto>? locations { get; set; }
    }

    public class LocationRowDto
    {
        public long Id { get; set; }
        public string? locationName { get; set; }
        public double lat { get; set; }
        public double lon { get; set; }
    }

    public class ResHistorialConsumoDto
    {
        public bool Result { get; set; }
        public List<ConsumoHistorialRowDto>? historial { get; set; }
    }

    public class ConsumoHistorialRowDto
    {
        public string? nombreReferencia { get; set; }
        public double consumoKwh { get; set; }
        public double monto { get; set; }
        public DateTime fechaConsumo { get; set; }
    }
}
