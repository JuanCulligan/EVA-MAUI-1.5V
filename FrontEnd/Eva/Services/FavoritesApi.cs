using Eva.DTO;
using Eva.Entidades;
using Eva.Response;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;

namespace Eva.Services
{
    public static class FavoritesApi
    {
        public static string StableId(string? name, double lat, double lon)
        {
            string n = name ?? string.Empty;
            return "fav_" + Math.Round(lat, 5).ToString(CultureInfo.InvariantCulture)
                + "_" + Math.Round(lon, 5).ToString(CultureInfo.InvariantCulture)
                + "_" + n.GetHashCode(StringComparison.Ordinal).ToString("X", CultureInfo.InvariantCulture);
        }

        public static async Task<List<SavedPlace>> GetFavoritesAsPlacesAsync(Guid userGuid, CancellationToken ct = default)
        {
            if (userGuid == Guid.Empty || !AppConfiguration.IsApiBaseUrlConfigured())
            {
                return new List<SavedPlace>();
            }

            using HttpClient client = ApiHttp.CreateClient();
            HttpResponseMessage response = await client.GetAsync(
                AppConfiguration.GetApiUrl("api/viaje/getFavorites/" + userGuid), ct);
            string body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                return new List<SavedPlace>();
            }

            List<FavoriteLocationDto> rows = new List<FavoriteLocationDto>();
            try
            {
                JObject jo = JObject.Parse(body);
                JToken? ok = jo["result"] ?? jo["Result"];
                if (ok?.Type == JTokenType.Boolean && ok.Value<bool>() == false)
                {
                    return new List<SavedPlace>();
                }

                JToken? fav = jo["favorites"] ?? jo["Favorites"];
                if (fav is JArray arr)
                {
                    rows = arr.ToObject<List<FavoriteLocationDto>>() ?? new List<FavoriteLocationDto>();
                }
            }
            catch
            {
                return new List<SavedPlace>();
            }

            List<SavedPlace> list = new List<SavedPlace>();
            foreach (FavoriteLocationDto? r in rows)
            {
                if (r == null || string.IsNullOrWhiteSpace(r.locationName))
                {
                    continue;
                }

                string id = StableId(r.locationName, r.lat, r.lon);
                list.Add(new SavedPlace
                {
                    Id = id,
                    Name = r.locationName.Trim(),
                    Kind = "Otro",
                    Latitude = r.lat,
                    Longitude = r.lon,
                    AddressNote = FavoriteNotesStore.Get(id)
                });
            }

            return list;
        }

        public static async Task<bool> SaveFavoriteAsync(Guid userGuid, string locationName, double lat, double lon, CancellationToken ct = default)
        {
            if (userGuid == Guid.Empty || !AppConfiguration.IsApiBaseUrlConfigured())
            {
                return false;
            }

            var dto = new ReqSaveFavoriteDto { guid = userGuid, locationName = locationName, lat = lat, lon = lon };
            using HttpClient client = ApiHttp.CreateClient();
            string json = JsonConvert.SerializeObject(dto);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            HttpResponseMessage response = await client.PostAsync(
                AppConfiguration.GetApiUrl("api/viaje/saveFavorite"), content, ct);
            string body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            try
            {
                JObject jo = JObject.Parse(body);
                JToken? ok = jo["result"] ?? jo["Result"];
                return ok?.Type != JTokenType.Boolean || ok.Value<bool>();
            }
            catch
            {
                return true;
            }
        }

        public static async Task<bool> DeleteFavoriteAsync(Guid userGuid, string locationName, double lat, double lon, CancellationToken ct = default)
        {
            if (userGuid == Guid.Empty || !AppConfiguration.IsApiBaseUrlConfigured())
            {
                return false;
            }

            var dto = new ReqDeleteFavoriteDto { guid = userGuid, locationName = locationName, lat = lat, lon = lon };
            using HttpClient client = ApiHttp.CreateClient();
            string json = JsonConvert.SerializeObject(dto);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            HttpResponseMessage response = await client.PostAsync(
                AppConfiguration.GetApiUrl("api/viaje/deleteFavorite"), content, ct);
            string body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            try
            {
                JObject jo = JObject.Parse(body);
                JToken? ok = jo["result"] ?? jo["Result"];
                return ok?.Type != JTokenType.Boolean || ok.Value<bool>();
            }
            catch
            {
                return true;
            }
        }
    }
}
