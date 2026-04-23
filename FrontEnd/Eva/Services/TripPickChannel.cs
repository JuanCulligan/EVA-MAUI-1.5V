namespace Eva.Services
{
    public sealed class TripPickPayload
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }

    public static class TripPickChannel
    {
        static EventHandler<TripPickPayload>? _tripPicked;

        public static void Subscribe(EventHandler<TripPickPayload> handler)
        {
            _tripPicked -= handler;
            _tripPicked += handler;
        }

        public static void Unsubscribe(EventHandler<TripPickPayload> handler)
        {
            _tripPicked -= handler;
        }

        public static void Send(TripPickPayload payload)
        {
            _tripPicked?.Invoke(null, payload);
        }
    }
}
