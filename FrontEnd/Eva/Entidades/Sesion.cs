namespace Eva.Entidades
{
    public static class Sesion
    {
        public static Guid guid { get; set; }
        public static string Name { get; set; } = string.Empty;
        public static string LastName { get; set; } = string.Empty;
        public static string Email { get; set; } = string.Empty;
        public static string Adress { get; set; } = string.Empty;
        public static string ChargerType { get; set; } = string.Empty;
        public static DateTime open { get; set; }
    }
}
