namespace Eva.DTO
{
    public class DTOConsumoGuardar
    {
        public Guid guid { get; set; }
        public string nombreReferencia { get; set; } = string.Empty;
        public double TotalEnergyWh { get; set; }
        public double PercentUsed { get; set; }
    }
}
