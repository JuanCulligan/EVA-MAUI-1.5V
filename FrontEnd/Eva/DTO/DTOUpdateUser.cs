namespace Eva.DTO
{
    public class DTOUpdateUser
    {
        public Guid guid { get; set; }
        public string Name { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Adress { get; set; } = string.Empty;
        public string chargerType { get; set; } = string.Empty;
    }
}
