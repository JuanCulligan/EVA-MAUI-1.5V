namespace Eva.Services
{
    public static class ApiHttp
    {
        public static HttpClient CreateClient()
        {
            HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(120);
            if (AppConfiguration.IsNgrokUrl())
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation("ngrok-skip-browser-warning", "true");
            }

            return client;
        }
    }
}
