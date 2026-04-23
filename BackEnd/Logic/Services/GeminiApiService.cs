using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Services
{
    public class GeminiApiService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly string _apiKey;

        public GeminiApiService()
        {
            _apiKey = Environment.GetEnvironmentVariable("Gemini_API_Key");// consigue api key de mis varibles de entorno
        }

        // crea la estructura para enviar el prompt
        public async Task<string> SendPrompt(string prompt)
        {
            var body = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            string jsonBody = JsonConvert.SerializeObject(body);

            // preparamso para envira la informacion
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}");

            //pone el propmet en la estructura json, nos asguramos de que sea utf8 para no romper nada y le decimos a google que estamos mandodno un json
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            //toma la response de google y lo conierte en una string
            HttpResponseMessage response = await _httpClient.SendAsync(request);

            return await response.Content.ReadAsStringAsync();
        }
    }
}
