using Eva.Entidades;

namespace Eva.Response
{
    public class ResBase
    {
        public bool Result { get; set; }
        public List<Error>? errors { get; set; }
    }
}
