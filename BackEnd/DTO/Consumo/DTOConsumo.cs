using System;

namespace DTO.Consumo
{
    public class DTOConsumo
    {
        public Guid guid { get; set; }
        public string nombreReferencia { get; set; }
        public double TotalEnergyWh { get; set; }
        public double PercentUsed { get; set; }
    }
}