using Eva.Enums;

namespace Eva.Entidades
{
    public class Error
    {
        public EnumErrors code { get; set; }
        public string message { get; set; } = string.Empty;
    }
}
