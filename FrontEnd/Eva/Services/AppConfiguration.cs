using System.Text.Json;
using System.Threading;

namespace Eva.Services
{
    public static class AppConfiguration
    {
        public const string ApiUrlPlaceholderEvabd = "TU-API-EVABD";

        private static readonly SemaphoreSlim LoadLock = new SemaphoreSlim(1, 1);
        private static bool loaded;
        private static string apiBaseUrl = string.Empty;
        private static string supportEmail = string.Empty;

        public static string ApiBaseUrl
        {
            get
            {
                return apiBaseUrl;
            }
        }

        public static async Task EnsureLoadedAsync()
        {
            if (loaded)
            {
                return;
            }

            await LoadLock.WaitAsync();
            try
            {
                if (loaded)
                {
                    return;
                }

                using Stream stream = await FileSystem.OpenAppPackageFileAsync("appsettings.json");
                using StreamReader reader = new StreamReader(stream);
                string json = await reader.ReadToEndAsync();
                using JsonDocument doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("ApiBaseUrl", out JsonElement urlEl))
                {
                    apiBaseUrl = NormalizeBaseUrl(urlEl.GetString() ?? string.Empty);
                }

                if (doc.RootElement.TryGetProperty("SupportEmail", out JsonElement mailEl))
                {
                    supportEmail = (mailEl.GetString() ?? string.Empty).Trim();
                }

                loaded = true;
            }
            finally
            {
                LoadLock.Release();
            }
        }

        public static string NormalizeBaseUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return string.Empty;
            }

            string t = url.Trim();

            if (!t.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                && !t.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                t = "https://" + t.TrimStart('/');
            }

            t = t.TrimEnd();

            if (!Uri.TryCreate(t, UriKind.Absolute, out Uri? absolute))
            {
                return t.EndsWith("/", StringComparison.Ordinal) ? t : t + "/";
            }

            string rebuilt = absolute.GetLeftPart(UriPartial.Authority);
            if (!string.IsNullOrEmpty(absolute.AbsolutePath) && absolute.AbsolutePath != "/")
            {
                rebuilt += absolute.AbsolutePath.TrimEnd('/');
            }

            return rebuilt.EndsWith("/", StringComparison.Ordinal) ? rebuilt : rebuilt + "/";
        }

        public static string GetApiUrl(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return ApiBaseUrl;
            }

            string rel = relativePath.TrimStart('/');

            if (!Uri.TryCreate(ApiBaseUrl, UriKind.Absolute, out Uri? baseUri))
            {
                string b = ApiBaseUrl.TrimEnd('/');
                return b + "/" + rel;
            }

            return new Uri(baseUri, rel).AbsoluteUri;
        }

        public static bool IsNgrokUrl()
        {
            return ApiBaseUrl.Contains("ngrok", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsApiBaseUrlConfigured()
        {
            if (string.IsNullOrWhiteSpace(apiBaseUrl))
            {
                return false;
            }

            if (apiBaseUrl.Contains(ApiUrlPlaceholderEvabd, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return !apiBaseUrl.Contains("TU-API-AZURE", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Correo de destino para “Reportar problema”; no versionar direcciones reales en repos públicos.</summary>
        public static string SupportEmail => supportEmail;

        public static bool IsSupportEmailConfigured()
        {
            return !string.IsNullOrWhiteSpace(supportEmail)
                && supportEmail.Contains('@', StringComparison.Ordinal)
                && supportEmail.Contains('.', StringComparison.Ordinal);
        }
    }
}
