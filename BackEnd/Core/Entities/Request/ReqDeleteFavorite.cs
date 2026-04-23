using System;

namespace Core.Entities.Request
{
    public class ReqDeleteFavorite
    {
        public Guid guid { get; set; }
        public string locationName { get; set; }
        public double lat { get; set; }
        public double lon { get; set; }
    }
}
