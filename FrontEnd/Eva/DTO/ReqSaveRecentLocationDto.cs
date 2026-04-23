namespace Eva.DTO
{
    public class ReqSaveRecentLocationDto
    {
        public Guid guid { get; set; }
        public string locationName { get; set; } = string.Empty;
        public double lat { get; set; }
        public double lon { get; set; }
    }
}
