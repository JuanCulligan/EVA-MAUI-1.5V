using System;

namespace Core.Entities.Request
{
    public class ReqUpdateVehicle
    {
        public Guid guid { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public int Year { get; set; }
    }
}
