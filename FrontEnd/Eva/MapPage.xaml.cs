using Eva.DTO;
using Eva.Entidades;
using Eva.Services;
using System.Globalization;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Eva.Response;
using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Threading;

namespace Eva
{
    public partial class MapPage : ContentPage
    {
        // Mapbox token público (pk.*): créalo en tu cuenta y configúralo solo en local; no subas tokens reales a Git.
        const string MapboxAccessToken = "";

        private bool mapDomReady;
        private bool chargersRequestStarted;
        private bool _mapPageHtmlLoaded;
        private CancellationTokenSource? _loadingFallbackCts;
        private DateTime _mapOverlayShownUtc;

        public MapPage()
        {
            InitializeComponent();
            mapWebView.Navigating += MapWebView_Navigating;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
#if ANDROID
            MapWebBridge.NavigateRequested -= OnMapWebBridgeNavigate;
            MapWebBridge.NavigateRequested += OnMapWebBridgeNavigate;
#endif
            await AppConfiguration.EnsureLoadedAsync();
            TripPickChannel.Unsubscribe(OnTripPickFromSavedPlaces);
            TripPickChannel.Subscribe(OnTripPickFromSavedPlaces);
#if ANDROID || IOS || MACCATALYST
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                    await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }
            catch
            {
                
            }
#endif
            if (!_mapPageHtmlLoaded)
            {
                mapDomReady = false;
                chargersRequestStarted = false;
                _ = LoadMapWithLocationSeedAsync();
            }
            else if (mapDomReady)
            {
                _ = CargarCargadoresDesdeApiAsync();
                _ = PushTripEstimateConstantsToWebAsync();
            }
        }

        protected override void OnDisappearing()
        {
            _loadingFallbackCts?.Cancel();
#if ANDROID
            MapWebBridge.NavigateRequested -= OnMapWebBridgeNavigate;
#endif
            base.OnDisappearing();
        }

#if ANDROID
        void OnMapWebBridgeNavigate(string url) => ProcessEvaSchemeUrl(url);
#endif

        void ProcessEvaSchemeUrl(string url)
        {
            const string prefix = "eva://";
            if (string.IsNullOrWhiteSpace(url)
                || !url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string path = url[prefix.Length..].Trim().TrimStart('/');
            if (path.Equals("map-ready", StringComparison.OrdinalIgnoreCase))
            {
                OnMapGlReadyFromWeb();
                return;
            }

            if (path.Equals("chargers-refresh", StringComparison.OrdinalIgnoreCase))
            {
                MainThread.BeginInvokeOnMainThread(() => _ = CargarCargadoresDesdeApiAsync(compactRefresh: true));
                return;
            }

            if (path.Equals("viajes", StringComparison.OrdinalIgnoreCase)
                || path.Equals("recientes", StringComparison.OrdinalIgnoreCase))
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        await Shell.Current.GoToAsync(nameof(ViajesHistoryPage));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex);
                    }
                });
                return;
            }

            if (path.Equals("consumos", StringComparison.OrdinalIgnoreCase))
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        await Shell.Current.GoToAsync(nameof(ConsumosHistoryPage));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex);
                    }
                });
                return;
            }

            if (path.StartsWith("consumo/registrar-viaje", StringComparison.OrdinalIgnoreCase))
            {
                string? encodedP = ExtractQueryParamFromEvaPath(path, "p");
                if (!string.IsNullOrEmpty(encodedP))
                {
                    MainThread.BeginInvokeOnMainThread(() => _ = RegistrarConsumoViajeAsync(encodedP));
                }

                return;
            }

            if (path.Equals("consumo/guardar", StringComparison.OrdinalIgnoreCase))
            {
                _ = GuardarConsumoRemotoAsync();
                return;
            }

            if (path.StartsWith("trip/battery-estimate", StringComparison.OrdinalIgnoreCase))
            {
                string? encodedP = ExtractQueryParamFromEvaPath(path, "p");
                MainThread.BeginInvokeOnMainThread(() => _ = CompleteTripStartWithBatteryAsync(encodedP));
                return;
            }

            if (path.Equals("configuracion", StringComparison.OrdinalIgnoreCase))
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        await Shell.Current.GoToAsync(nameof(SettingsPage));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex);
                    }
                });
                return;
            }

            if (path.Equals("lugares", StringComparison.OrdinalIgnoreCase))
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        await Shell.Current.GoToAsync(nameof(SavedPlacesPage));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex);
                    }
                });
                return;
            }

            if (path.Equals("reporte", StringComparison.OrdinalIgnoreCase))
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        await Shell.Current.GoToAsync(nameof(ReportBugPage));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex);
                    }
                });
                return;
            }
        }

        static readonly (double lng, double lat, double zoom) MapFallbackCenter = (-99.13, 19.43, 10);

        async Task LoadMapWithLocationSeedAsync()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                loadingOverlay.IsVisible = true;
                loadingOverlayLabel.Text = "Obteniendo ubicación…";
            });

            double lng = MapFallbackCenter.lng;
            double lat = MapFallbackCenter.lat;
            double zoom = MapFallbackCenter.zoom;

#if ANDROID || IOS || MACCATALYST
            try
            {
                Location? last = await Geolocation.Default.GetLastKnownLocationAsync();
                TimeSpan freshBudget = last == null ? TimeSpan.FromSeconds(8) : TimeSpan.FromSeconds(3);
                Task<Location?> freshTask = Geolocation.Default.GetLocationAsync(
                    new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(20)));
                Task winner = await Task.WhenAny(freshTask, Task.Delay(freshBudget));
                Location? fresh = null;
                if (winner == freshTask)
                {
                    try
                    {
                        fresh = await freshTask;
                    }
                    catch
                    {
                        fresh = null;
                    }
                }

                Location? pick = fresh ?? last;
                if (pick != null)
                {
                    lng = pick.Longitude;
                    lat = pick.Latitude;
                    zoom = 13.6;
                }
            }
            catch
            {
            }
#endif

            MainThread.BeginInvokeOnMainThread(() =>
            {
                loadingOverlayLabel.Text = "Cargando mapa…";
                LoadMap(lng, lat, zoom);
            });
        }

        void LoadMap(double centerLng, double centerLat, double centerZoom)
        {
            _loadingFallbackCts?.Cancel();
            _loadingFallbackCts = new CancellationTokenSource();
            CancellationToken token = _loadingFallbackCts.Token;
            mapDomReady = false;
            loadingOverlay.IsVisible = true;
            _mapOverlayShownUtc = DateTime.UtcNow;
            var assetV = WebUtility.UrlEncode(AppInfo.Current.BuildString);
            if (string.IsNullOrEmpty(assetV)) assetV = "0";
            var stampHtml = WebUtility.HtmlEncode($"{AppInfo.Current.VersionString} · build {AppInfo.Current.BuildString}");
            bool seededFromGps = Math.Abs(centerLng - MapFallbackCenter.lng) > 1e-6
                || Math.Abs(centerLat - MapFallbackCenter.lat) > 1e-6;
            double tripWh = TripEstimatePreferences.GetWhPerKm();
            double tripCrc = TripEstimatePreferences.GetCrcPerKwh();
            mapWebView.Source = new HtmlWebViewSource
            {
                Html = BuildHtml(MapboxAccessToken, stampHtml, assetV, centerLng, centerLat, centerZoom, seededFromGps, tripWh, tripCrc),
            };
            _ = MapLoadFallbackDismissAsync(token);
        }

        async Task MapLoadFallbackDismissAsync(CancellationToken token)
        {
            try
            {
                await Task.Delay(16000, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (!loadingOverlay.IsVisible)
                {
                    return;
                }

                loadingOverlay.IsVisible = false;
                mapDomReady = true;
                _mapPageHtmlLoaded = true;
                if (!chargersRequestStarted)
                {
                    chargersRequestStarted = true;
                    _ = CargarCargadoresDesdeApiAsync();
                }
            });
        }

        void OnMapGlReadyFromWeb()
        {
            _loadingFallbackCts?.Cancel();
            _ = DismissMapOverlayAfterMinAsync();
            _ = PushTripEstimateConstantsToWebAsync();
        }

        async Task DismissMapOverlayAfterMinAsync()
        {
            const int minMs = 520;
            int wait = minMs - (int)(DateTime.UtcNow - _mapOverlayShownUtc).TotalMilliseconds;
            if (wait > 0)
            {
                try
                {
                    await Task.Delay(wait);
                }
                catch
                {
                }
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                loadingOverlay.IsVisible = false;
                mapDomReady = true;
                _mapPageHtmlLoaded = true;
                if (!chargersRequestStarted)
                {
                    chargersRequestStarted = true;
                    _ = CargarCargadoresDesdeApiAsync();
                }
            });
        }

        void MapWebView_Navigating(object? sender, WebNavigatingEventArgs e)
        {
            if (!e.Url.StartsWith("eva://", StringComparison.OrdinalIgnoreCase))
                return;
            e.Cancel = true;
            ProcessEvaSchemeUrl(e.Url);
        }

        private async Task CargarCargadoresDesdeApiAsync(bool compactRefresh = false)
        {
            if (!AppConfiguration.IsApiBaseUrlConfigured())
            {
                await EjecutarEnMapaAsync(BuildChargersInjectScript(null));
                return;
            }

            int radiusKm = compactRefresh ? 3 : 8;

            try
            {
                Location? location = await Geolocation.Default.GetLocationAsync(
                    new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(12)));

                if (location == null)
                {
                    location = await Geolocation.Default.GetLastKnownLocationAsync();
                }

                if (location == null)
                {
                    await EjecutarEnMapaAsync(BuildChargersInjectScript(null));
                    return;
                }

                NearbyChargersRequest body = new NearbyChargersRequest
                {
                    Lat = location.Latitude,
                    Lon = location.Longitude,
                    RadiusKm = radiusKm
                };

                string jsonBody = JsonConvert.SerializeObject(body);
                using HttpClient client = ApiHttp.CreateClient();
                using StringContent content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                HttpResponseMessage response = await client.PostAsync(AppConfiguration.GetApiUrl("api/chargers/nearby"), content);
                string responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    await EjecutarEnMapaAsync(BuildChargersInjectScript(null));
                    return;
                }

                List<ChargerStationDto>? stations = JsonConvert.DeserializeObject<List<ChargerStationDto>>(responseText);
                if (stations == null)
                {
                    await EjecutarEnMapaAsync(BuildChargersInjectScript(null));
                    return;
                }

                List<ChargerStationDto> dentroDeRadio = FiltrarPorDistanciaKm(
                    location.Latitude,
                    location.Longitude,
                    stations,
                    radiusKm);

                await EjecutarEnMapaAsync(BuildChargersInjectScript(dentroDeRadio));
            }
            catch
            {
                await EjecutarEnMapaAsync(BuildChargersInjectScript(null));
            }
        }

        private static string BuildChargersInjectScript(List<ChargerStationDto>? stations)
        {
            if (stations == null || stations.Count == 0)
            {
                return "if(typeof loadChargersFromApi==='function')loadChargersFromApi([]);";
            }

            string json = JsonConvert.SerializeObject(
                stations,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            string b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            return "if(typeof loadChargersFromApiB64==='function')loadChargersFromApiB64('" + b64 + "');else if(typeof loadChargersFromApi==='function')loadChargersFromApi([]);";
        }

        private static List<ChargerStationDto> FiltrarPorDistanciaKm(
            double userLat,
            double userLon,
            List<ChargerStationDto> stations,
            double maxKm)
        {
            List<ChargerStationDto> lista = new List<ChargerStationDto>();
            foreach (ChargerStationDto s in stations)
            {
                double km = DistanciaKm(userLat, userLon, s.Latitude, s.Longitude);
                if (km <= maxKm)
                {
                    lista.Add(s);
                }
            }

            return lista;
        }

        private static double DistanciaKm(double lat1, double lon1, double lat2, double lon2)
        {
            double rlat1 = lat1 * (Math.PI / 180.0);
            double rlat2 = lat2 * (Math.PI / 180.0);
            double dlat = (lat2 - lat1) * (Math.PI / 180.0);
            double dlon = (lon2 - lon1) * (Math.PI / 180.0);
            double a = Math.Sin(dlat / 2.0) * Math.Sin(dlat / 2.0)
                + Math.Cos(rlat1) * Math.Cos(rlat2) * Math.Sin(dlon / 2.0) * Math.Sin(dlon / 2.0);
            double c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
            return 6371.0 * c;
        }

        private async Task StartTripFromSavedPlaceAsync(TripPickPayload payload)
        {
            if (payload == null)
            {
                return;
            }

            string name = string.IsNullOrWhiteSpace(payload.DisplayName) ? "Destino" : payload.DisplayName;
            string nameJs = JsonConvert.SerializeObject(name);
            string lng = payload.Longitude.ToString(CultureInfo.InvariantCulture);
            string lat = payload.Latitude.ToString(CultureInfo.InvariantCulture);
            await EjecutarEnMapaAsync($"openTripToDestination({lng},{lat},{nameJs});");
        }

        void OnTripPickFromSavedPlaces(object? sender, TripPickPayload payload)
        {
            MainThread.BeginInvokeOnMainThread(() => _ = StartTripFromSavedPlaceAsync(payload));
        }

        private async Task EjecutarEnMapaAsync(string script)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                for (int i = 0; i < 90 && !mapDomReady; i++)
                {
                    await Task.Delay(120);
                }

                if (!mapDomReady || mapWebView is null)
                {
                    return;
                }

                try
                {
                    await mapWebView.EvaluateJavaScriptAsync(script);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            });
        }

        static string? ExtractQueryParamFromEvaPath(string path, string key)
        {
            int q = path.IndexOf('?', StringComparison.Ordinal);
            if (q < 0 || q >= path.Length - 1)
            {
                return null;
            }

            foreach (string part in path[(q + 1)..].Split('&'))
            {
                int eq = part.IndexOf('=');
                if (eq <= 0)
                {
                    continue;
                }

                string k = Uri.UnescapeDataString(part[..eq]);
                if (k.Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    return Uri.UnescapeDataString(part[(eq + 1)..]);
                }
            }

            return null;
        }

        private static async Task GuardarHistorialDestinoViajeAsync(double lat, double lon, string locationName)
        {
            if (Sesion.guid == Guid.Empty || !AppConfiguration.IsApiBaseUrlConfigured())
            {
                return;
            }

            if (Math.Abs(lat) < 1e-6 && Math.Abs(lon) < 1e-6)
            {
                return;
            }

            ReqSaveRecentLocationDto dto = new ReqSaveRecentLocationDto
            {
                guid = Sesion.guid,
                locationName = locationName,
                lat = lat,
                lon = lon
            };

            try
            {
                using HttpClient client = ApiHttp.CreateClient();
                string json = JsonConvert.SerializeObject(dto);
                using StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                HttpResponseMessage response = await client.PostAsync(
                    AppConfiguration.GetApiUrl("api/viaje/saveRecentLocation"),
                    content);
                _ = await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        async Task CompleteTripStartWithBatteryAsync(string? base64Utf8Payload)
        {
            await AppConfiguration.EnsureLoadedAsync();

            async Task NotifyJsFallbackAsync()
            {
                await EjecutarEnMapaAsync("try{if(window.__evaAfterBatteryEstimate)window.__evaAfterBatteryEstimate(null);}catch(_e){}");
            }

            if (string.IsNullOrEmpty(base64Utf8Payload))
            {
                await NotifyJsFallbackAsync();
                return;
            }

            double olat;
            double olng;
            double dlat;
            double dlng;
            try
            {
                byte[] raw = Convert.FromBase64String(base64Utf8Payload);
                string json = Encoding.UTF8.GetString(raw);
                JObject jo = JObject.Parse(json);
                olat = jo.Value<double?>("olat") ?? jo.Value<double?>("oLat") ?? 0;
                olng = jo.Value<double?>("olng") ?? jo.Value<double?>("oLng") ?? 0;
                dlat = jo.Value<double?>("dlat") ?? jo.Value<double?>("dLat") ?? 0;
                dlng = jo.Value<double?>("dlng") ?? jo.Value<double?>("dLng") ?? 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                await NotifyJsFallbackAsync();
                return;
            }

            if (Sesion.guid == Guid.Empty || !AppConfiguration.IsApiBaseUrlConfigured())
            {
                await NotifyJsFallbackAsync();
                return;
            }

            var req = new BatteryEstimationRequestDto
            {
                guid = Sesion.guid,
                StartLat = olat,
                StartLon = olng,
                EndLat = dlat,
                EndLon = dlng,
                SpeedKmh = 50
            };

            try
            {
                using HttpClient client = ApiHttp.CreateClient();
                string jsonBody = JsonConvert.SerializeObject(req);
                using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                HttpResponseMessage response = await client.PostAsync(
                    AppConfiguration.GetApiUrl("api/battery/estimate"),
                    content);
                string body = await response.Content.ReadAsStringAsync();

                BatteryEstimationResponseDto? dto = null;
                try
                {
                    dto = JsonConvert.DeserializeObject<BatteryEstimationResponseDto>(body);
                }
                catch
                {
                    dto = null;
                }

                bool ok = dto != null && dto.Result && dto.TotalEnergyWh > 0;
                if (!response.IsSuccessStatusCode || !ok)
                {
                    await NotifyJsFallbackAsync();
                    return;
                }

                double kwh = dto!.TotalEnergyWh / 1000.0;
                double crc = Math.Max(0, Math.Round(kwh * TripEstimatePreferences.GetCrcPerKwh()));
                double pct = dto.PercentUsed;
                string payloadJson = JsonConvert.SerializeObject(new
                {
                    kwh,
                    costCrc = crc,
                    percentUsed = pct,
                    usedBackend = true
                });
                await EjecutarEnMapaAsync(
                    "try{if(window.__evaAfterBatteryEstimate)window.__evaAfterBatteryEstimate(" + payloadJson + ");}catch(_e){}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                await NotifyJsFallbackAsync();
            }
        }

        private async Task RegistrarConsumoViajeAsync(string base64Utf8Payload)
        {
            await AppConfiguration.EnsureLoadedAsync();

            if (Sesion.guid == Guid.Empty)
            {
                await DisplayAlertAsync("Consumos", "Inicia sesión para guardar el consumo de este viaje en tu historial.", "Aceptar");
                return;
            }

            if (!AppConfiguration.IsApiBaseUrlConfigured())
            {
                await DisplayAlertAsync("Consumos", "Configura ApiBaseUrl en appsettings.json (backend EVABD).", "Aceptar");
                return;
            }

            double kwh;
            string referencia;
            double? dlat = null;
            double? dlng = null;
            try
            {
                byte[] raw = Convert.FromBase64String(base64Utf8Payload);
                string json = Encoding.UTF8.GetString(raw);
                JObject jo = JObject.Parse(json);
                kwh = jo.Value<double?>("kwh") ?? 0;
                double distKm = jo.Value<double?>("distKm") ?? 0;
                if (kwh <= 0 && distKm > 0)
                {
                    kwh = distKm * 165.0 / 1000.0;
                }

                referencia = jo.Value<string>("ref") ?? "Destino";
                referencia = referencia.Trim();
                if (string.IsNullOrEmpty(referencia))
                {
                    referencia = "Viaje";
                }

                if (referencia.Length > 90)
                {
                    referencia = referencia[..90];
                }

                dlat = jo.Value<double?>("dlat");
                dlng = jo.Value<double?>("dlng");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                await DisplayAlertAsync("Consumos", "No se pudieron leer los datos del viaje.", "Aceptar");
                return;
            }

            if (dlat.HasValue && dlng.HasValue
                && !(Math.Abs(dlat.Value) < 1e-6 && Math.Abs(dlng.Value) < 1e-6))
            {
                await GuardarHistorialDestinoViajeAsync(dlat.Value, dlng.Value, referencia);
            }

            if (kwh <= 0)
            {
                return;
            }

            DTOConsumoGuardar dto = new DTOConsumoGuardar
            {
                guid = Sesion.guid,
                nombreReferencia = "Viaje · " + referencia,
                TotalEnergyWh = kwh * 1000.0,
                PercentUsed = 8
            };

            try
            {
                using HttpClient client = ApiHttp.CreateClient();
                string json = JsonConvert.SerializeObject(dto);
                using StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                HttpResponseMessage response = await client.PostAsync(AppConfiguration.GetApiUrl("api/consumo/guardar"), content);
                string body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    string snippet = string.IsNullOrWhiteSpace(body) ? "" : (body.Length > 160 ? body[..160] + "…" : body);
                    await DisplayAlertAsync("Consumos", "No se guardó el consumo: " + (int)response.StatusCode + " " + snippet, "Aceptar");
                    return;
                }

                if (!string.IsNullOrWhiteSpace(body))
                {
                    ResBase? res = JsonConvert.DeserializeObject<ResBase>(body);
                    if (res != null && !res.Result)
                    {
                        await DisplayAlertAsync("Consumos", "El servidor no pudo registrar el consumo del viaje.", "Aceptar");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Consumos", "Error de red: " + ex.Message, "Aceptar");
            }
        }

        private async Task GuardarConsumoRemotoAsync()
        {
            await AppConfiguration.EnsureLoadedAsync();

            if (Sesion.guid == Guid.Empty)
            {
                await DisplayAlertAsync("Consumos", "Inicia sesión para guardar consumo.", "Aceptar");
                return;
            }

            if (!AppConfiguration.IsApiBaseUrlConfigured())
            {
                await DisplayAlertAsync("Consumos", "Pon la URL del backend EVABD en Resources/Raw/appsettings.json (ApiBaseUrl).", "Aceptar");
                return;
            }

            DTOConsumoGuardar dto = new DTOConsumoGuardar();
            dto.guid = Sesion.guid;
            dto.nombreReferencia = "App móvil — mapa";
            dto.TotalEnergyWh = 18000;
            dto.PercentUsed = 10;

            try
            {
                using HttpClient client = ApiHttp.CreateClient();
                string json = JsonConvert.SerializeObject(dto);
                using StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                HttpResponseMessage response = await client.PostAsync(AppConfiguration.GetApiUrl("api/consumo/guardar"), content);
                string body = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlertAsync("Consumos", "Consumo registrado en el servidor.", "Aceptar");
                }
                else
                {
                    string snippet = string.IsNullOrWhiteSpace(body) ? "" : (body.Length > 160 ? body[..160] + "…" : body);
                    await DisplayAlertAsync("Consumos", "Error " + (int)response.StatusCode + " " + response.ReasonPhrase + "\n" + snippet, "Aceptar");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Consumos", "No se pudo conectar: " + ex.Message, "Aceptar");
            }
        }

        async Task PushTripEstimateConstantsToWebAsync()
        {
            double w = TripEstimatePreferences.GetWhPerKm();
            double c = TripEstimatePreferences.GetCrcPerKwh();
            string ws = w.ToString(CultureInfo.InvariantCulture);
            string cs = c.ToString(CultureInfo.InvariantCulture);
            await EjecutarEnMapaAsync($"try{{ if (window.__evaSetTripEstConstants) window.__evaSetTripEstConstants({ws},{cs}); }}catch(_e){{}}");
        }

        static string BuildHtml(
            string token,
            string appBuildStampHtml,
            string mapAssetVersion,
            double initialLng,
            double initialLat,
            double initialZoom,
            bool seededFromDeviceLocation,
            double tripEstWhPerKm,
            double tripEstCrcPerKwh)
        {
            string ilng = initialLng.ToString(CultureInfo.InvariantCulture);
            string ilat = initialLat.ToString(CultureInfo.InvariantCulture);
            string izoom = initialZoom.ToString(CultureInfo.InvariantCulture);
            string seededJs = seededFromDeviceLocation ? "true" : "false";
            var styleUrl = $"https://api.mapbox.com/styles/v1/mapbox/dark-v11?access_token={token}";
            return $@"
<!DOCTYPE html>
<html>
<head>
  <meta name='viewport' content='width=device-width, initial-scale=1, maximum-scale=1, user-scalable=no, viewport-fit=cover' />
  <link rel='preconnect' href='https://api.mapbox.com' crossorigin />
  <link rel='dns-prefetch' href='https://events.mapbox.com' />
  <link href='https://api.mapbox.com/mapbox-gl-js/v3.8.0/mapbox-gl.css?v={mapAssetVersion}' rel='stylesheet' />
  <script src='https://api.mapbox.com/mapbox-gl-js/v3.8.0/mapbox-gl.js?v={mapAssetVersion}'></script>
  <style>
    :root {{
      --bg: #0D0D0D;
      --card70: rgba(0,0,0,.70);
      --card80: rgba(0,0,0,.80);
      --card95: rgba(0,0,0,.95);
      --border10: rgba(255,255,255,.10);
      --text: #ffffff;
      --muted: rgba(156,163,175,1);
      --muted2: rgba(107,114,128,1);
      --blue: #3b82f6;
      --green: #22c55e;
      --orange: #f97316;
      --red: #ef4444;
      --pink: #ec4899;
      --gray: #6b7280;
      --purple: #a855f7;
      --eva-inset-top: 0px;
      --eva-inset-bottom: 0px;
      --eva-inset-left: 0px;
      --eva-inset-right: 0px;
      --eva-widget-gap: 12px;
    }}

    html, body {{ margin:0; padding:0; width:100%; height:100%; background:var(--bg); overflow:hidden; }}
    #map {{ position:absolute; inset:0; background:var(--bg); }}

    #err {{
      position: fixed;
      top: calc(8px + max(env(safe-area-inset-top, 0px), var(--eva-inset-top, 0px)) + var(--eva-widget-gap, 12px));
      left: 50%;
      transform: translateX(-50%);
      width: min(520px, calc(100vw - 48px));
      display: none;
      z-index: 3000;
      color: #e5e5e5;
      font-family: system-ui, -apple-system, Segoe UI, Roboto, sans-serif;
      padding: 12px 14px;
      text-align: left;
      background: rgba(0,0,0,.85);
      border: 1px solid rgba(255,255,255,.10);
      border-radius: 16px;
      box-shadow: 0 24px 60px rgba(0,0,0,.55);
      backdrop-filter: blur(16px);
      -webkit-backdrop-filter: blur(16px);
      pointer-events: none;
    }}

    .blur-lg {{ backdrop-filter: blur(16px); -webkit-backdrop-filter: blur(16px); }}
    .blur-xl {{ backdrop-filter: blur(22px); -webkit-backdrop-filter: blur(22px); }}
    .shadow-xl {{ box-shadow: 0 18px 40px rgba(0,0,0,.45); }}
    .shadow-2xl {{ box-shadow: 0 24px 60px rgba(0,0,0,.55); }}

    .box {{
      position:fixed;
      z-index:1000;
      background: var(--card70);
      border: 1px solid var(--border10);
      border-radius: 16px;
      color: var(--text);
    }}
    .box.pad12 {{ padding: 12px; }}
    .box.pad16 {{ padding: 16px; }}
    .fade-in-left {{
      opacity:0; transform: translateX(-20px);
      animation: fadeInLeft .3s ease forwards;
    }}
    .fade-in-right {{
      opacity:0; transform: translateX(20px);
      animation: fadeInRight .3s ease forwards;
    }}
    .fade-in-up {{
      opacity:0; transform: translateY(-20px);
      animation: fadeInUp .3s ease forwards;
    }}
    @keyframes fadeInLeft {{ to {{ opacity:1; transform: translateX(0); }} }}
    @keyframes fadeInRight {{ to {{ opacity:1; transform: translateX(0); }} }}
    @keyframes fadeInUp {{ to {{ opacity:1; transform: translateY(0); }} }}

    #speedBox {{
      top: 0;
      left: 0;
      margin: 0;
      min-width:80px;
      text-align:center;
      padding-top: calc(12px + max(env(safe-area-inset-top, 0px), var(--eva-inset-top, 0px)) + var(--eva-widget-gap, 12px));
      padding-left: 12px;
      padding-right: 12px;
      padding-bottom: 12px;
    }}
    #speedBox .label {{ font-size: 12px; color: rgba(156,163,175,1); }}
    #speedBox .value {{ font-size: 24px; font-weight: 700; line-height: 1.1; }}

    #etaBox {{
      top: 0;
      right: 0;
      margin: 0;
      padding-top: calc(16px + max(env(safe-area-inset-top, 0px), var(--eva-inset-top, 0px)) + var(--eva-widget-gap, 12px));
      padding-left: 16px;
      padding-right: 12px;
      padding-bottom: 16px;
    }}
    #etaRow {{
      display:flex; align-items:flex-end; gap:12px;
    }}
    #etaRow .cell .k {{ font-size: 12px; color: rgba(156,163,175,1); }}
    #etaRow .cell .v {{ font-size: 18px; font-weight: 600; }}
    #etaSep {{ width:1px; height: 24px; background: rgba(255,255,255,.10); }}
    #trafficBadge {{
      margin-top:10px;
      display:inline-flex; align-items:center; gap:6px;
      background: rgba(34,197,94,.90);
      border-radius: 999px;
      padding: 4px 12px;
      font-size: 12px;
      font-weight: 700;
      color:#04130a;
    }}
    #trafficBadge.blue {{
      background: rgba(59,130,246,.90);
      color: #071529;
    }}

    #compass {{
      position:fixed;
      top: calc(max(env(safe-area-inset-top, 0px), var(--eva-inset-top, 0px)) + var(--eva-widget-gap, 12px) + 161px);
      right: 12px;
      width:44px;
      height:44px;
      border-radius: 999px;
      background: var(--card70);
      border: 1px solid var(--border10);
      z-index:1000;
      display:flex;
      align-items:center;
      justify-content:center;
      color: var(--text);
      font-weight: 700;
      font-size: 14px;
    }}
    #compass::before {{
      content:'';
      position:absolute;
      top:-5px;
      width:0; height:0;
      border-left:6px solid transparent;
      border-right:6px solid transparent;
      border-bottom:10px solid #ef4444;
    }}

    #zoomControls {{
      position:fixed;
      bottom: max(24px, max(env(safe-area-inset-bottom, 0px), var(--eva-inset-bottom, 0px)));
      right: 12px;
      z-index:500;
      display:flex;
      flex-direction:column;
      gap:8px;
    }}
    .zbtn {{
      width:44px; height:44px;
      border-radius: 12px;
      background: rgba(0,0,0,.80);
      border: 1px solid rgba(255,255,255,.10);
      color: var(--text);
      font-size: 20px;
      font-weight: 300;
      display:flex;
      align-items:center;
      justify-content:center;
      user-select:none;
      cursor:pointer;
      transition: transform .08s ease, background .15s ease;
      box-shadow: 0 12px 24px rgba(0,0,0,.45);
    }}
    .zbtn:active {{ transform: scale(.95); }}
    .zbtn svg {{ width: 22px; height: 22px; stroke: currentColor; fill: none; stroke-width: 2; stroke-linecap: round; }}

    #fabWrap {{
      position:fixed;
      bottom: max(24px, max(env(safe-area-inset-bottom, 0px), var(--eva-inset-bottom, 0px)));
      left:50%;
      transform: translateX(-50%);
      z-index:1000;
    }}
    #fabCollapsed {{
      width:64px; height:64px;
      border-radius: 999px;
      background: linear-gradient(135deg, #2563eb, #3b82f6);
      box-shadow: 0 8px 32px rgba(59,130,246,.4);
      display:flex; align-items:center; justify-content:center;
      cursor:pointer;
      transition: transform .15s ease;
    }}
    #fabCollapsed:active {{ transform: scale(.9); }}
    #fabIcon {{
      width:24px; height:24px;
      fill: none;
      stroke: white;
      stroke-width: 2;
      stroke-linecap: round;
      stroke-linejoin: round;
    }}

    #fabExpanded {{
      width:min(672px, calc(100vw - 48px));
      background: var(--card95);
      border: 1px solid var(--border10);
      border-radius: 28px;
      padding: 24px;
      display:none;
      transform-origin: 50% 100%;
      animation: pop .22s cubic-bezier(.2,.9,.2,1.1) forwards;
    }}
    @keyframes pop {{ from {{ transform: scale(.8); opacity:0; }} to {{ transform: scale(1); opacity:1; }} }}

    #menuGrid {{
      display:grid;
      grid-template-columns: repeat(auto-fit, minmax(68px, 1fr));
      gap: 14px;
      margin-top: 8px;
      max-width: 100%;
    }}
    .menuOpt {{
      display:flex;
      flex-direction:column;
      align-items:center;
      gap:12px;
      cursor:pointer;
      user-select:none;
      transition: transform .12s ease;
    }}
    .menuOpt:active {{ transform: scale(.95); }}
    .optCircle {{
      width:64px; height:64px;
      border-radius: 999px;
      display:flex; align-items:center; justify-content:center;
      box-shadow: 0 4px 12px rgba(0,0,0,.35);
    }}
    .optCircle svg {{ width:24px; height:24px; stroke:white; stroke-width:2; fill:none; stroke-linecap:round; stroke-linejoin:round; }}
    .optLabel {{ color:white; font-size: 14px; font-weight: 600; }}

    #searchBarWrap {{
      position: fixed;
      top: calc(12px + max(env(safe-area-inset-top, 0px), var(--eva-inset-top, 0px)) + var(--eva-widget-gap, 12px));
      left: 0;
      right: 0;
      display: flex;
      justify-content: center;
      align-items: flex-start;
      padding: 0 16px;
      box-sizing: border-box;
      z-index: 1001;
      pointer-events: none;
    }}
    #searchBarColumn {{
      pointer-events: auto;
      width: min(448px, 100%);
      max-width: calc(100vw - 32px);
    }}
    #searchBar {{
      position: relative;
      width: 100%;
      height: 48px;
      margin: 0;
      background: var(--card95);
      border: 1px solid var(--border10);
      border-radius: 24px;
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 0 16px;
      box-shadow: 0 24px 60px rgba(0,0,0,.55);
      pointer-events: auto;
      animation: searchBarIn .28s ease forwards;
    }}
    @keyframes searchBarIn {{
      from {{ opacity: 0; transform: translateY(-10px); }}
      to {{ opacity: 1; transform: translateY(0); }}
    }}
    #searchBar svg {{ width:20px; height:20px; stroke: rgba(156,163,175,1); stroke-width:2; fill:none; }}
    #searchInput {{
      flex:1;
      background: transparent;
      border: none;
      outline: none;
      color: white;
      font-size: 14px;
    }}
    #searchInput::placeholder {{ color: rgba(107,114,128,1); }}
    #searchGoBtn {{
      flex-shrink: 0;
      height: 36px;
      padding: 0 14px;
      border-radius: 18px;
      border: none;
      background: var(--blue);
      color: white;
      font-size: 14px;
      font-weight: 700;
      cursor: pointer;
    }}
    #searchGoBtn:active {{ transform: scale(.97); }}
    .searchHint {{
      margin-top: 8px;
      font-size: 12px;
      color: #f87171;
      text-align: center;
      line-height: 1.35;
      max-width: 100%;
    }}
    .searchSuggestPanel {{
      margin-top: 8px;
      background: var(--card95);
      border: 1px solid var(--border10);
      border-radius: 18px;
      box-shadow: 0 24px 60px rgba(0,0,0,.55);
      max-height: min(46vh, 300px);
      overflow-y: auto;
      -webkit-overflow-scrolling: touch;
    }}
    .searchSuggestItem {{
      padding: 12px 14px;
      cursor: pointer;
      border-bottom: 1px solid rgba(255,255,255,.07);
      font-family: system-ui, -apple-system, Segoe UI, Roboto, sans-serif;
      font-size: 14px;
      color: var(--text);
    }}
    .searchSuggestItem:last-child {{ border-bottom: none; }}
    .searchSuggestItem:active {{ background: rgba(255,255,255,.08); }}
    .searchSuggestTitle {{ font-weight: 600; line-height: 1.35; }}
    .searchSuggestSub {{ font-size: 12px; color: var(--muted); margin-top: 4px; line-height: 1.3; }}

    #modalOverlay {{
      position:fixed;
      inset:0;
      display:none;
      align-items:center;
      justify-content:center;
      background: rgba(0,0,0,.60);
      z-index:2000;
      backdrop-filter: blur(8px);
      -webkit-backdrop-filter: blur(8px);
    }}
    .modal {{
      width: min(448px, calc(100vw - 48px));
      background: var(--card95);
      border: 1px solid var(--border10);
      border-radius: 24px;
      padding: 32px;
      box-shadow: 0 28px 80px rgba(0,0,0,.60);
      transform-origin: 50% 50%;
      animation: modalPop .22s cubic-bezier(.2,.9,.2,1.1) forwards;
      color:white;
    }}
    @keyframes modalPop {{ from {{ transform: scale(.9); opacity:0; }} to {{ transform: scale(1); opacity:1; }} }}
    .modalTitle {{ font-size: 24px; font-weight: 800; text-align:center; margin-bottom: 8px; }}
    .modalSub {{ font-size: 14px; color: rgba(156,163,175,1); text-align:center; margin-bottom: 24px; }}
    .infoGrid {{ display:grid; grid-template-columns: 1fr 1fr; gap: 12px; }}
    .infoCard {{
      background: rgba(255,255,255,.05);
      border: 1px solid rgba(255,255,255,.10);
      border-radius: 16px;
      padding: 16px;
    }}
    .infoCard .k {{ font-size: 12px; color: rgba(156,163,175,1); margin-bottom: 6px; display:flex; align-items:center; gap:8px; }}
    .infoCard .v {{ font-size: 16px; font-weight: 800; }}
    .btnPrimary {{
      width:100%;
      margin-top: 18px;
      background: linear-gradient(135deg, #2563eb, #3b82f6);
      border: none;
      border-radius: 16px;
      padding: 16px;
      color: white;
      font-weight: 800;
      cursor:pointer;
      transition: transform .08s ease, filter .15s ease;
      display:flex; align-items:center; justify-content:center; gap:10px;
    }}
    .btnPrimary:active {{ transform: scale(.98); }}
    .btnSecondary {{
      width:100%;
      margin-top: 18px;
      background: linear-gradient(135deg, #374151, #4b5563);
      border: none;
      border-radius: 16px;
      padding: 16px;
      color: white;
      font-weight: 800;
      cursor:pointer;
      transition: transform .08s ease, filter .15s ease;
      display:flex; align-items:center; justify-content:center; gap:10px;
    }}
    .btnSecondary:active {{ transform: scale(.98); }}

    .badgeMarker {{
      font-family: system-ui, -apple-system, Segoe UI, Roboto, sans-serif;
      font-size: 11px;
      font-weight: 600;
      color: white;
      display:flex;
      flex-direction:column;
      align-items:center;
      gap: 6px;
      filter: drop-shadow(0 10px 16px rgba(0,0,0,.55));
    }}
    .badge {{
      padding: 6px 10px;
      border-radius: 999px;
      border: 1px solid rgba(255,255,255,.10);
      background: rgba(0,0,0,.70);
      backdrop-filter: blur(16px);
      -webkit-backdrop-filter: blur(16px);
      white-space: nowrap;
    }}
    .pinSvg {{
      width: 18px;
      height: 18px;
    }}
    .pinSvg path {{ fill: rgba(255,255,255,.92); }}
    .chargerMarkerRoot {{ cursor: pointer; display: flex; flex-direction: column; align-items: center; }}
    .chargerBadge {{
      border-radius: 999px;
      border: 1px solid rgba(255,255,255,.10);
      background: rgba(0,0,0,.78);
      backdrop-filter: blur(12px);
      -webkit-backdrop-filter: blur(12px);
      white-space: nowrap;
      opacity: 0;
      max-height: 0;
      margin: 0;
      padding: 0;
      overflow: hidden;
      transition: opacity .18s ease, max-height .2s ease, padding .2s ease, margin .2s ease;
      pointer-events: none;
      font-size: 12px;
      font-weight: 600;
      color: rgba(255,255,255,.95);
    }}
    .chargerBadge.visible {{
      opacity: 1;
      max-height: 40px;
      padding: 5px 10px;
      margin-bottom: 4px;
      pointer-events: auto;
    }}
    .chargerGround {{
      position: relative;
      width: 30px;
      height: 30px;
      display: flex;
      align-items: center;
      justify-content: center;
    }}
    .chargerDot {{
      width: 15px;
      height: 15px;
      border-radius: 50%;
      border: 2px solid rgba(255,255,255,.92);
      box-shadow: 0 0 0 2px rgba(0,0,0,.35), 0 0 14px currentColor;
      z-index: 2;
    }}
    .chargerGround .pinSvg {{
      position: absolute;
      bottom: -2px;
      width: 20px;
      height: 20px;
      opacity: .88;
      z-index: 1;
      pointer-events: none;
    }}

    #navTurnBanner {{
      position: fixed;
      left: 12px;
      right: auto;
      transform: none;
      top: auto;
      bottom: calc(102px + max(env(safe-area-inset-bottom, 0px), var(--eva-inset-bottom, 0px)));
      width: min(340px, calc(100vw - 96px));
      max-width: calc(100vw - 96px);
      z-index: 2500;
      display: none;
      align-items: stretch;
      justify-content: center;
      min-height: 56px;
      padding: 12px 14px;
      border-radius: 18px;
      background: linear-gradient(180deg, rgba(15,23,42,.96) 0%, rgba(15,23,42,.88) 100%);
      border: 1px solid rgba(148,163,184,.25);
      box-shadow: 0 12px 40px rgba(0,0,0,.55), 0 0 0 1px rgba(255,255,255,.06) inset;
      color: #f8fafc;
      font-size: 16px;
      font-weight: 700;
      text-align: left;
      line-height: 1.4;
      backdrop-filter: blur(14px);
      -webkit-backdrop-filter: blur(14px);
    }}
    #navTurnBanner .navTurnInner {{
      display: flex;
      align-items: flex-start;
      gap: 12px;
    }}
    #navTurnBanner .navTurnGlyph {{
      flex: 0 0 auto;
      width: 36px;
      height: 36px;
      border-radius: 12px;
      background: rgba(59,130,246,.25);
      border: 1px solid rgba(96,165,250,.35);
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 18px;
      line-height: 1;
    }}
    #navTurnBanner .navTurnText {{
      flex: 1;
      min-width: 0;
      padding-top: 2px;
    }}

    .userWrap {{
      position: relative;
      width: 20px;
      height: 20px;
    }}
    .userWrap.userWrapNav {{
      width: 36px;
      height: 36px;
    }}
    .userDot {{
      position:absolute;
      inset:0;
      border-radius: 999px;
      background: var(--blue);
      border: 3px solid white;
      box-shadow: 0 0 18px rgba(59,130,246,.6);
      z-index:2;
    }}
    .userPulse {{
      position:absolute;
      left:50%;
      top:50%;
      width: 18px;
      height: 18px;
      transform: translate(-50%,-50%);
      border-radius: 999px;
      background: rgba(59,130,246,.25);
      box-shadow: 0 0 26px rgba(59,130,246,.35);
      animation: pulse 2s ease-out infinite;
      z-index:1;
    }}
    .userNavGlow {{
      position: absolute;
      left: 50%;
      top: 55%;
      width: 28px;
      height: 28px;
      transform: translate(-50%, -50%);
      border-radius: 50%;
      background: radial-gradient(circle, rgba(56,189,248,.45) 0%, rgba(59,130,246,.12) 55%, transparent 72%);
      animation: pulseNav 1.8s ease-out infinite;
      z-index: 0;
    }}
    .userNavArrow {{
      position: absolute;
      left: 0;
      top: 0;
      right: 0;
      bottom: 0;
      z-index: 2;
      filter: drop-shadow(0 0 10px rgba(56,189,248,.95)) drop-shadow(0 0 18px rgba(37,99,235,.55));
    }}
    .userNavArrow svg {{
      width: 100%;
      height: 100%;
      display: block;
    }}
    @keyframes pulse {{
      0% {{ transform: translate(-50%,-50%) scale(1); opacity: .9; }}
      100% {{ transform: translate(-50%,-50%) scale(3.2); opacity: 0; }}
    }}
    @keyframes pulseNav {{
      0% {{ transform: translate(-50%, -50%) scale(1); opacity: .85; }}
      100% {{ transform: translate(-50%, -50%) scale(2.4); opacity: 0; }}
    }}

    #mapBuildStamp {{
      position: fixed;
      bottom: max(6px, max(env(safe-area-inset-bottom, 0px), var(--eva-inset-bottom, 0px)));
      left: 8px;
      z-index: 2500;
      font-size: 10px;
      line-height: 1.2;
      color: rgba(255,255,255,.32);
      font-family: system-ui, -apple-system, sans-serif;
      pointer-events: none;
      max-width: 72vw;
    }}
  </style>
</head>
<body>
  <div id='map'></div>
  <div id='err'></div>
  <div id='mapBuildStamp'>{appBuildStampHtml}</div>
  <div id='navTurnBanner' role='status' aria-live='polite'></div>

  <div id='speedBox' class='box pad12 blur-lg shadow-xl fade-in-left'>
    <div class='label'>km/h</div>
    <div id='speedValue' class='value'>0</div>
  </div>

  <div id='etaBox' class='box pad16 blur-lg shadow-xl fade-in-right'>
    <div id='etaRow'>
      <div class='cell'>
        <div class='k'>Tiempo</div>
        <div id='etaTime' class='v'>0</div>
      </div>
      <div id='etaSep'></div>
      <div class='cell'>
        <div class='k'>Distancia</div>
        <div id='etaDist' class='v'>0</div>
      </div>
    </div>
    <div id='trafficBadge'>Sin tráfico</div>
  </div>

  <div id='compass' class='fade-in-up'>N</div>

  <div id='zoomControls'>
    <div id='myLocationBtn' class='zbtn blur-lg' title='Ir a mi ubicación' role='button'>
      <svg viewBox='0 0 24 24' aria-hidden='true'><circle cx='12' cy='12' r='3'/><path d='M12 2v3M12 19v3M2 12h3M19 12h3'/></svg>
    </div>
    <div id='zoomIn' class='zbtn blur-lg'>+</div>
    <div id='zoomOut' class='zbtn blur-lg'>−</div>
  </div>

  <div id='fabWrap'>
    <div id='fabCollapsed' title='Menú'>
      <svg id='fabIcon' viewBox='0 0 24 24' aria-hidden='true'>
        <path d='M3 10.5L12 3l9 7.5'></path>
        <path d='M5 10v10h14V10'></path>
      </svg>
    </div>

    <div id='fabExpanded' class='blur-xl shadow-2xl' style='position:relative;'>
      <div id='menuGrid'>
        <div class='menuOpt' data-id='viajes'>
          <div class='optCircle' style='background: var(--blue); box-shadow: 0 4px 12px rgba(59,130,246,.3);'>
            <svg viewBox='0 0 24 24'><path d='M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z'></path><path d='M9 22V12h6v10'></path></svg>
          </div>
          <div class='optLabel'>Historial</div>
        </div>
        <div class='menuOpt' data-id='report'>
          <div class='optCircle' style='background: var(--orange); box-shadow: 0 4px 12px rgba(249,115,22,.3);'>
            <svg viewBox='0 0 24 24'><path d='M12 9v4'></path><path d='M12 17h.01'></path><path d='M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z'></path></svg>
          </div>
          <div class='optLabel'>Reportar</div>
        </div>
        <div class='menuOpt' data-id='places'>
          <div class='optCircle' style='background: var(--green); box-shadow: 0 4px 12px rgba(34,197,94,.3);'>
            <svg viewBox='0 0 24 24'><path d='M12 21s7-4.35 7-11a7 7 0 1 0-14 0c0 6.65 7 11 7 11z'></path><path d='M12 10.5a2 2 0 1 0 0-4 2 2 0 0 0 0 4z'></path></svg>
          </div>
          <div class='optLabel'>Lugares</div>
        </div>
        <div class='menuOpt' data-id='consumos'>
          <div class='optCircle' style='background: var(--pink); box-shadow: 0 4px 12px rgba(236,72,153,.3);'>
            <svg viewBox='0 0 24 24'><path d='M3 22h12'></path><path d='M4 14h10'></path><path d='M14 14l4-4'></path><path d='M4 14V6a2 2 0 0 1 2-2h6a2 2 0 0 1 2 2v8'></path><path d='M18 10a2 2 0 0 1 2 2v6'></path></svg>
          </div>
          <div class='optLabel'>Consumos</div>
        </div>
        <div class='menuOpt' data-id='settings'>
          <div class='optCircle' style='background: var(--gray); box-shadow: 0 4px 12px rgba(107,114,128,.3);'>
            <svg viewBox='0 0 24 24'><path d='M12 15.5a3.5 3.5 0 1 0 0-7 3.5 3.5 0 0 0 0 7z'></path><path d='M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1-1.42 3.42h-.09a1.65 1.65 0 0 0-1.82.33 1.65 1.65 0 0 0-.5 1.6A2 2 0 0 1 12 24a2 2 0 0 1-1.94-1.5 1.65 1.65 0 0 0-.5-1.6 1.65 1.65 0 0 0-1.82-.33h-.09a2 2 0 0 1-1.42-3.42l.06-.06A1.65 1.65 0 0 0 4.6 15a1.65 1.65 0 0 0-1.6-.5A2 2 0 0 1 0 12a2 2 0 0 1 1.5-1.94 1.65 1.65 0 0 0 1.6-.5 1.65 1.65 0 0 0 .33-1.82l-.06-.06A2 2 0 0 1 3.42 4.3h.09a1.65 1.65 0 0 0 1.82-.33 1.65 1.65 0 0 0 .5-1.6A2 2 0 0 1 12 0a2 2 0 0 1 1.94 1.5 1.65 1.65 0 0 0 .5 1.6 1.65 1.65 0 0 0 1.82.33h.09A2 2 0 0 1 20.58 7.7l-.06.06A1.65 1.65 0 0 0 19.4 9a1.65 1.65 0 0 0 .5 1.6 1.65 1.65 0 0 0 1.6.33A2 2 0 0 1 24 12a2 2 0 0 1-1.5 1.94 1.65 1.65 0 0 0-1.6.5 1.65 1.65 0 0 0-.33 1.82z'></path></svg>
          </div>
          <div class='optLabel'>Ajustes</div>
        </div>
      </div>
    </div>
  </div>

  <div id='searchBarWrap'>
    <div id='searchBarColumn'>
      <div id='searchBar' class='blur-xl'>
        <svg viewBox='0 0 24 24'><path d='M21 21l-4.3-4.3'></path><path d='M11 19a8 8 0 1 1 0-16 8 8 0 0 1 0 16z'></path></svg>
        <input id='searchInput' type='search' enterkeyhint='search' autocomplete='off' autocorrect='off' spellcheck='false' placeholder='Dirección o lugar (Mapbox)' />
        <button type='button' id='searchGoBtn'>Buscar</button>
      </div>
      <div id='searchSuggestions' class='searchSuggestPanel blur-xl' style='display:none' role='listbox' aria-label='Sugerencias'></div>
      <div id='searchHint' class='searchHint' style='display:none'></div>
    </div>
  </div>

  <div id='modalOverlay'>
    <div id='modal' class='modal'></div>
  </div>

  <script>
    function evaApplySystemInsets() {{
      try {{
        const a = window.EvaAndroid;
        const insetFn = a && (a.getSystemBarInsetsDp || a.GetSystemBarInsetsDp);
        if (!a || typeof insetFn !== 'function') return;
        const s = JSON.parse(insetFn.call(a));
        const r = document.documentElement;
        r.style.setProperty('--eva-inset-top', (s.top || 0) + 'px');
        r.style.setProperty('--eva-inset-bottom', (s.bottom || 0) + 'px');
        r.style.setProperty('--eva-inset-left', (s.left || 0) + 'px');
        r.style.setProperty('--eva-inset-right', (s.right || 0) + 'px');
        try {{
          if (window.__evaMap && typeof window.__evaMap.setPadding === 'function') {{
            window.__evaMap.setPadding({{ top: 0, bottom: s.bottom || 0, left: 0, right: 0 }});
          }}
        }} catch (_e2) {{}}
      }} catch (_e) {{}}
    }}

    const MAP_STYLE_DEFAULT = '{styleUrl}';
    let map = null;
    let _mapInitError = null;
    try {{
      mapboxgl.accessToken = '{token}';
      map = new mapboxgl.Map({{
        container: 'map',
        style: MAP_STYLE_DEFAULT,
        center: [{ilng}, {ilat}],
        zoom: {izoom},
        pitch: 0,
        bearing: 0,
        maxPitch: 78,
        renderWorldCopies: false,
        antialias: false,
        fadeDuration: 0
      }});
    }} catch (e) {{
      _mapInitError = e;
    }}

    try {{ window.__evaMap = map; }} catch (_m) {{}}
    evaApplySystemInsets();
    window.addEventListener('resize', () => {{ evaApplySystemInsets(); }});
    [0, 50, 200, 600].forEach((ms) => {{ setTimeout(() => evaApplySystemInsets(), ms); }});

    const UI = {{
      state: 'normal',
      timeMin: 15,
      distKm: 8.5,
      tripDurationSec: 0,
      tripEnergyKwh: 0,
      tripCostCrc: 0,
      tripBatteryPercentUsed: null,
      navTimer: null,
      navTick: null
    }};

    const CHARGER_LABEL_MIN_ZOOM = 15;
    let TRIP_EST_WH_PER_KM = {tripEstWhPerKm.ToString(CultureInfo.InvariantCulture)};
    let TRIP_EST_CRC_PER_KWH = {tripEstCrcPerKwh.ToString(CultureInfo.InvariantCulture)};
    window.__evaSetTripEstConstants = function(wh, crc) {{
      try {{
        const w = Number(wh), c = Number(crc);
        if (!isNaN(w) && w > 20 && w < 800) TRIP_EST_WH_PER_KM = w;
        if (!isNaN(c) && c >= 0) TRIP_EST_CRC_PER_KWH = c;
      }} catch (_x) {{}}
    }};

    window.__evaAfterBatteryEstimate = function(data) {{
      try {{
        if (data && data.usedBackend && typeof data.kwh === 'number' && data.kwh > 0) {{
          UI.tripEnergyKwh = data.kwh;
          UI.tripCostCrc = (typeof data.costCrc === 'number' && !isNaN(data.costCrc)) ? data.costCrc : Math.max(0, Math.round(data.kwh * TRIP_EST_CRC_PER_KWH));
          UI.tripBatteryPercentUsed = (typeof data.percentUsed === 'number' && !isNaN(data.percentUsed)) ? data.percentUsed : null;
        }}
      }} catch (_e) {{}}
      show(qs('modalOverlay'), false);
      setState('navigating');
    }};

    function formatTripEta(durationSec) {{
      try {{
        const ms = Date.now() + durationSec * 1000;
        return new Date(ms).toLocaleTimeString(undefined, {{ hour: '2-digit', minute: '2-digit' }});
      }} catch (_) {{
        return '—';
      }}
    }}

    function buildTripEstimates(distKm, durationSec) {{
      const sec = Math.max(1, Number(durationSec) || 60);
      const durationMin = Math.max(1, Math.round(sec / 60));
      const energyKwh = Math.round(((distKm * TRIP_EST_WH_PER_KM) / 1000) * 100) / 100;
      const costCrc = Math.max(0, Math.round(energyKwh * TRIP_EST_CRC_PER_KWH));
      return {{
        durationMin,
        durationSec: sec,
        eta: formatTripEta(sec),
        energyKwh,
        costCrc
      }};
    }}

    let tripDestLng = null;
    let tripDestLat = null;
    let tripDestName = '';
    let tripDestIsCharger = false;
    let tripOriginLng = null;
    let tripOriginLat = null;
    let tripRouteGeometry = null;
    let tripRouteCoords = [];
    let tripRouteSliceIndex = 0;
    let tripTotalRouteDistM = 0;
    let tripTotalRouteDurSec = 0;
    let navTripStartMs = 0;
    let tripPlannedDistKm = 0;
    let navRouteSteps = [];
    let navStepIndex = 0;
    const chargerMarkersMeta = [];
    let geocodeDebounceTimer = null;
    let geocodeAbortController = null;
    let lastSuggestFeatures = [];
    let lastChargerRefreshLat = null;
    let lastChargerRefreshLng = null;
    let lastChargerRefreshAt = 0;
    function maybeRequestChargerRefresh(lng, lat) {{
      if (lng == null || lat == null || isNaN(lng) || isNaN(lat)) return;
      const now = Date.now();
      if (now - lastChargerRefreshAt < 12000) return;
      if (lastChargerRefreshLat == null) {{
        lastChargerRefreshLat = lat;
        lastChargerRefreshLng = lng;
        return;
      }}
      const movedKm = distKmJs(lng, lat, lastChargerRefreshLng, lastChargerRefreshLat);
      if (movedKm < 0.8) return;
      lastChargerRefreshAt = now;
      lastChargerRefreshLat = lat;
      lastChargerRefreshLng = lng;
      evaOpenNative('chargers-refresh');
    }}

    function qs(id) {{ return document.getElementById(id); }}
    function setText(id, t) {{ qs(id).textContent = t; }}
    function show(el, on) {{ el.style.display = on ? 'flex' : 'none'; }}

    function hideSearchHint() {{
      const h = qs('searchHint');
      if (h) {{ h.style.display = 'none'; h.textContent = ''; }}
    }}
    function showSearchHint(msg) {{
      const h = qs('searchHint');
      if (!h) return;
      h.textContent = msg || '';
      h.style.display = msg ? 'block' : 'none';
    }}

    function evaOpenNative(path) {{
      let p = String(path || '').trim();
      const low = p.toLowerCase();
      if (low.indexOf('eva://') === 0) p = p.substring(6);
      while (p.length && p.charAt(0) === '/') p = p.substring(1);
      try {{
        const a = window.EvaAndroid;
        if (a) {{
          if (typeof a.open === 'function') {{ a.open(p); return; }}
          if (typeof a.Open === 'function') {{ a.Open(p); return; }}
        }}
      }} catch (e0) {{}}
      try {{ window.location.href = 'eva://' + p; }} catch (e1) {{}}
    }}

    let __evaMapReadySent = false;
    function notifyNativeMapReady() {{
      if (__evaMapReadySent) return;
      __evaMapReadySent = true;
      evaOpenNative('map-ready');
    }}

    function setVisibleNormalBoxes(on) {{
      qs('speedBox').style.display = on ? 'block' : 'none';
      qs('etaBox').style.display = on ? 'block' : 'none';
      qs('compass').style.display = on ? 'flex' : 'none';
    }}

    function setFabExpanded(on) {{
      qs('fabCollapsed').style.display = on ? 'none' : 'flex';
      qs('fabExpanded').style.display = on ? 'block' : 'none';
    }}

    function setSearchBarVisible(on) {{
      const w = qs('searchBarWrap');
      if (!w) return;
      w.style.display = on ? 'flex' : 'none';
    }}
    setSearchBarVisible(false);

    qs('myLocationBtn').addEventListener('click', () => {{ if (!map) return; flyToMyLocation(); }});
    qs('zoomIn').addEventListener('click', () => {{ if (!map) return; map.zoomIn(); }});
    qs('zoomOut').addEventListener('click', () => {{ if (!map) return; map.zoomOut(); }});

    qs('fabCollapsed').addEventListener('click', () => setState('menu'));

    function hideSearchSuggestions() {{
      lastSuggestFeatures = [];
      const box = qs('searchSuggestions');
      if (!box) return;
      box.style.display = 'none';
      box.innerHTML = '';
    }}

    function resolveTripOrigin() {{
      let olng = lastUserLng;
      let olat = lastUserLat;
      if ((olng == null || olat == null) && userMarker) {{
        const ll = userMarker.getLngLat();
        olng = ll.lng;
        olat = ll.lat;
      }}
      if (olng == null || olat == null) {{
        if (map) {{
          const c = map.getCenter();
          olng = c.lng;
          olat = c.lat;
        }} else {{
          olng = defaultMapCenter[0];
          olat = defaultMapCenter[1];
        }}
      }}
      return [olng, olat];
    }}

    function openTripToDestination(lng, lat, name) {{
      tripDestLng = lng;
      tripDestLat = lat;
      tripDestName = name || 'Destino';
      tripDestIsCharger = false;
      setFabExpanded(false);
      setSearchBarVisible(false);
      hideSearchHint();
      hideSearchSuggestions();
      const inp = qs('searchInput');
      if (inp) inp.value = '';
      const [olng, olat] = resolveTripOrigin();
      tripOriginLng = olng;
      tripOriginLat = olat;
      navRouteSteps = [];
      navStepIndex = 0;
      UI.state = 'previewCharger';
      renderPreviewModalChargerLoading(tripDestName);
      show(qs('modalOverlay'), true);
      requestMapboxDrivingRoute(olng, olat, lng, lat, (route) => {{
        if (UI.state !== 'previewCharger') return;
        navRouteSteps = Array.isArray(route.steps) ? route.steps : [];
        navStepIndex = 0;
        tripTotalRouteDistM = (route.distance != null && !isNaN(route.distance)) ? route.distance : 0;
        tripTotalRouteDurSec = (route.duration != null && !isNaN(route.duration) && route.duration > 0) ? route.duration : 60;
        const distKm = Math.round(route.distance / 100) / 10;
        const durSec = route.duration || 60;
        renderPreviewTripEstimates(tripDestName, distKm, durSec, false);
        show(qs('modalOverlay'), true);
      }}, () => {{
        if (UI.state !== 'previewCharger') return;
        navRouteSteps = [];
        navStepIndex = 0;
        const d = distKmJs(olng, olat, lng, lat);
        const tMin = Math.max(1, Math.round((d / 35) * 60));
        const distKm = Math.round(d * 10) / 10;
        tripTotalRouteDistM = Math.max(1, d * 1000);
        tripTotalRouteDurSec = Math.max(60, tMin * 60);
        renderPreviewTripEstimates((tripDestName || 'Destino') + ' (aprox.)', distKm, tMin * 60, true);
        show(qs('modalOverlay'), true);
      }});
    }}

    async function fetchMapboxGeocode(query) {{
      const q = query.trim();
      if (q.length < 2) return null;
      if (geocodeAbortController) geocodeAbortController.abort();
      geocodeAbortController = new AbortController();
      const token = mapboxgl.accessToken;
      const enc = encodeURIComponent(q);
      let url = 'https://api.mapbox.com/geocoding/v5/mapbox.places/' + enc + '.json?access_token=' + encodeURIComponent(token)
        + '&autocomplete=true&limit=8&language=es&types=address,place,poi,locality,neighborhood';
      if (lastUserLng != null && lastUserLat != null) {{
        url += '&proximity=' + encodeURIComponent(lastUserLng + ',' + lastUserLat);
      }}
      const res = await fetch(url, {{ signal: geocodeAbortController.signal }});
      if (!res.ok) return null;
      return await res.json();
    }}

    function renderGeocodeSuggestions(features) {{
      hideSearchHint();
      const panel = qs('searchSuggestions');
      if (!panel) return;
      panel.innerHTML = '';
      lastSuggestFeatures = (features && features.length) ? features.slice(0, 8) : [];
      if (!lastSuggestFeatures.length) {{
        panel.style.display = 'none';
        return;
      }}
      lastSuggestFeatures.forEach((f) => {{
        const coords = f.geometry && f.geometry.coordinates;
        if (!coords || coords.length < 2) return;
        const row = document.createElement('div');
        row.className = 'searchSuggestItem';
        row.setAttribute('role', 'option');
        const main = f.place_name || f.text || '';
        const subParts = [];
        if (f.context && f.context.length) {{
          f.context.slice(0, 2).forEach((c) => {{ if (c.text) subParts.push(c.text); }});
        }}
        const sub = subParts.join(' · ');
        row.innerHTML = '<div class=""searchSuggestTitle"">' + escapeHtml(main) + '</div>'
          + (sub ? '<div class=""searchSuggestSub"">' + escapeHtml(sub) + '</div>' : '');
        row.addEventListener('mousedown', (ev) => {{
          ev.preventDefault();
          openTripToDestination(coords[0], coords[1], main);
        }});
        panel.appendChild(row);
      }});
      panel.style.display = panel.childNodes.length ? 'block' : 'none';
    }}

    function scheduleGeocodeSuggest() {{
      if (geocodeDebounceTimer) clearTimeout(geocodeDebounceTimer);
      geocodeDebounceTimer = setTimeout(async () => {{
        const inp = qs('searchInput');
        const text = inp ? inp.value : '';
        if (!text || text.trim().length < 2) {{
          hideSearchSuggestions();
          hideSearchHint();
          return;
        }}
        try {{
          const data = await fetchMapboxGeocode(text);
          if (!data || !data.features) {{
            hideSearchSuggestions();
            showSearchHint('Sin respuesta de Mapbox. Comprueba datos o conexión.');
            return;
          }}
          if (!data.features.length) {{
            hideSearchSuggestions();
            showSearchHint('No hay coincidencias. Prueba otra dirección.');
            return;
          }}
          renderGeocodeSuggestions(data.features);
        }} catch (e) {{
          if (e.name === 'AbortError') return;
          hideSearchSuggestions();
          showSearchHint('Error de búsqueda: ' + (e && e.message ? e.message : 'red'));
        }}
      }}, 320);
    }}

    async function geocodeAndGoFirstResult() {{
      const inp = qs('searchInput');
      const text = inp ? inp.value.trim() : '';
      if (text.length < 2) {{
        showSearchHint('Escribe al menos 2 caracteres y pulsa Buscar.');
        return;
      }}
      hideSearchHint();
      try {{
        if (geocodeAbortController) geocodeAbortController.abort();
        geocodeAbortController = new AbortController();
        const token = mapboxgl.accessToken;
        const enc = encodeURIComponent(text);
        let url = 'https://api.mapbox.com/geocoding/v5/mapbox.places/' + enc + '.json?access_token=' + encodeURIComponent(token)
          + '&limit=1&language=es&types=address,place,poi,locality,neighborhood';
        if (lastUserLng != null && lastUserLat != null) {{
          url += '&proximity=' + encodeURIComponent(lastUserLng + ',' + lastUserLat);
        }}
        const res = await fetch(url, {{ signal: geocodeAbortController.signal }});
        if (!res.ok) {{
          showSearchHint('Mapbox respondió ' + res.status + '. Revisa conexión o token.');
          return;
        }}
        const data = await res.json();
        if (!data.features || !data.features.length) {{
          showSearchHint('No se encontró ese lugar. Prueba otra búsqueda.');
          return;
        }}
        const f = data.features[0];
        const c = f.geometry.coordinates;
        openTripToDestination(c[0], c[1], f.place_name || f.text || text);
      }} catch (e) {{
        if (e.name === 'AbortError') return;
        showSearchHint('No se pudo buscar: ' + (e && e.message ? e.message : 'error'));
      }}
    }}

    const searchInputEl = qs('searchInput');
    if (searchInputEl) {{
      searchInputEl.addEventListener('input', () => scheduleGeocodeSuggest());
      searchInputEl.addEventListener('keydown', (e) => {{
        if (e.key === 'Enter') {{
          e.preventDefault();
          if (lastSuggestFeatures.length > 0) {{
            const f = lastSuggestFeatures[0];
            const coords = f.geometry && f.geometry.coordinates;
            if (coords && coords.length >= 2) {{
              openTripToDestination(coords[0], coords[1], f.place_name || f.text || '');
              return;
            }}
          }}
          geocodeAndGoFirstResult();
        }}
      }});
      searchInputEl.addEventListener('blur', () => {{
        setTimeout(() => hideSearchSuggestions(), 280);
      }});
    }}

    const searchGoBtn = qs('searchGoBtn');
    if (searchGoBtn) {{
      searchGoBtn.addEventListener('click', () => geocodeAndGoFirstResult());
    }}

    document.querySelectorAll('.menuOpt').forEach(el => {{
      el.addEventListener('click', () => {{
        const id = el.getAttribute('data-id');
        if (id === 'viajes') {{
          evaOpenNative('viajes');
        }} else if (id === 'consumos') {{
          evaOpenNative('consumos');
        }} else if (id === 'settings') {{
          evaOpenNative('configuracion');
        }} else if (id === 'places') {{
          evaOpenNative('lugares');
        }} else if (id === 'report') {{
          evaOpenNative('reporte');
        }} else {{
          setState('normal');
        }}
      }});
    }});

    qs('modalOverlay').addEventListener('click', (e) => {{
        if (e.target === qs('modalOverlay')) {{
        if (UI.state === 'summary' || UI.state === 'previewCharger' || UI.state === 'chargerInfo') {{
          setState('normal');
        }}
      }}
    }});

    function renderPreviewModalChargerLoading(name) {{
      const safe = escapeHtml(name || 'Destino');
      qs('modal').innerHTML = `
        <div class='modalTitle'>Estimación del viaje</div>
        <div class='modalSub'>Destino: ${{safe}}</div>
        <div class='modalSub' style='margin-top:10px;font-size:13px'>Obteniendo ruta con Mapbox…</div>
        <div class='infoGrid' style='opacity:.5;margin-top:10px'>
          <div class='infoCard'><div class='k'><span style='color:#fb923c'>⏱️</span> Duración del viaje</div><div class='v'>—</div></div>
          <div class='infoCard'><div class='k'><span style='color:#a855f7'>🕐</span> Llegada estimada</div><div class='v'>—</div></div>
          <div class='infoCard'><div class='k'><span style='color:#34d399'>🔋</span> Consumo energético</div><div class='v'>—</div></div>
          <div class='infoCard'><div class='k'><span style='color:#60a5fa'>💲</span> Consumo monetario</div><div class='v'>—</div></div>
        </div>
        <p class='modalSub' style='margin-top:14px;margin-bottom:0;font-size:11px;opacity:.75'>Las cifras son estimaciones orientativas.</p>
      `;
    }}

    function renderPreviewTripEstimates(name, distKm, durationSec, approximate) {{
      const est = buildTripEstimates(distKm, durationSec);
      UI.timeMin = est.durationMin;
      UI.distKm = distKm;
      UI.tripDurationSec = est.durationSec;
      UI.tripEnergyKwh = est.energyKwh;
      UI.tripCostCrc = est.costCrc;
      const safe = escapeHtml(name || 'Destino');
      const approxNote = approximate ? '<div class=""modalSub"" style=""margin-top:6px;font-size:12px;color:#fbbf24"">Ruta aproximada (sin detalle de calles).</div>' : '';
      qs('modal').innerHTML = `
        <div class='modalTitle'>Estimación del viaje</div>
        <div class='modalSub'>Destino: ${{safe}}</div>
        <div class='modalSub' style='margin-top:4px;font-size:13px'>Distancia del trayecto: <strong>${{distKm}} km</strong></div>
        ${{approxNote}}
        <div class='infoGrid' style='margin-top:16px'>
          <div class='infoCard'>
            <div class='k'><span style='color:#fb923c'>⏱️</span> Duración del viaje</div>
            <div class='v'>${{est.durationMin}} min</div>
          </div>
          <div class='infoCard'>
            <div class='k'><span style='color:#a855f7'>🕐</span> Llegada estimada</div>
            <div class='v'>${{est.eta}}</div>
          </div>
          <div class='infoCard'>
            <div class='k'><span style='color:#34d399'>🔋</span> Consumo energético</div>
            <div class='v'>${{est.energyKwh.toFixed(2)}} kWh</div>
          </div>
          <div class='infoCard'>
            <div class='k'><span style='color:#60a5fa'>💲</span> Consumo monetario</div>
            <div class='v'>₡ ${{est.costCrc.toLocaleString()}}</div>
          </div>
        </div>
        <p class='modalSub' style='margin-top:14px;margin-bottom:0;font-size:11px;opacity:.75'>Valores orientativos a partir de la distancia y la duración de la ruta.</p>
        <button id='startTrip' class='btnPrimary'>
          <span>▶</span> Iniciar viaje
        </button>
      `;
      qs('startTrip').addEventListener('click', () => {{
        try {{
          const bad = (v) => (v == null || v === '' || (typeof v === 'number' && (isNaN(v) || !isFinite(v))));
          if (bad(tripOriginLng) || bad(tripOriginLat) || bad(tripDestLng) || bad(tripDestLat)) {{
            try {{ alert('Faltan coordenadas del viaje. Vuelve a planificar la ruta en el mapa.'); }} catch (_) {{}}
            return;
          }}
          const btn = qs('startTrip');
          if (btn) {{ btn.disabled = true; btn.innerHTML = '<span>⏳</span> Calculando consumo…'; }}
          const pack = JSON.stringify({{ olat: Number(tripOriginLat), olng: Number(tripOriginLng), dlat: Number(tripDestLat), dlng: Number(tripDestLng) }});
          const b64 = btoa(unescape(encodeURIComponent(pack)));
          evaOpenNative('trip/battery-estimate?p=' + encodeURIComponent(b64));
        }} catch (_) {{
          try {{ if (window.__evaAfterBatteryEstimate) window.__evaAfterBatteryEstimate(null); }} catch (_e) {{}}
        }}
      }});
    }}

    function renderSummaryModal() {{
      const kwh = (UI.tripEnergyKwh != null && UI.tripEnergyKwh > 0) ? UI.tripEnergyKwh.toFixed(2) : '—';
      const crc = (UI.tripCostCrc != null && UI.tripCostCrc > 0) ? UI.tripCostCrc.toLocaleString() : '—';
      const pctLine = (UI.tripBatteryPercentUsed != null && !isNaN(UI.tripBatteryPercentUsed) && UI.tripBatteryPercentUsed > 0)
        ? '<div class=""modalSub"" style=""margin-top:8px;font-size:12px;opacity:.85"">Estimación backend: ~' + Number(UI.tripBatteryPercentUsed).toFixed(1) + ' % de batería del trayecto</div>'
        : '';
      qs('modal').innerHTML = `
        <div style='display:flex;justify-content:center;margin-bottom:12px;font-size:56px;color: var(--green);'>✓</div>
        <div class='modalTitle'>¡Viaje completado!</div>
        <div class='modalSub'>Resumen de tu viaje</div>
        ${{pctLine}}
        <div class='infoGrid'>
          <div class='infoCard'>
            <div class='k'><span style='color: #fb923c;'>⏱️</span> Duración</div>
            <div class='v'>${{UI.timeMin}} min</div>
          </div>
          <div class='infoCard'>
            <div class='k'><span style='color: var(--purple);'>🧭</span> Distancia</div>
            <div class='v'>${{UI.distKm}} km</div>
          </div>
          <div class='infoCard'>
            <div class='k'><span style='color: #34d399;'>🔋</span> Consumo energético</div>
            <div class='v'>${{kwh}} kWh</div>
          </div>
          <div class='infoCard'>
            <div class='k'><span style='color: #60a5fa;'>💲</span> Consumo monetario</div>
            <div class='v'>₡ ${{crc}}</div>
          </div>
        </div>
        <p class='modalSub' style='margin-top:12px;margin-bottom:0;font-size:12px;opacity:.8;line-height:1.4'>
          Al pulsar <strong>Finalizar</strong> se envía esta estimación al historial de consumos (kWh y monto en colones) si tienes sesión y el backend configurado.
        </p>
        <button id='finishTrip' class='btnSecondary'>Finalizar</button>
      `;
      qs('finishTrip').addEventListener('click', () => {{
        try {{
          const kwh = (UI.tripEnergyKwh != null && !isNaN(UI.tripEnergyKwh) && UI.tripEnergyKwh > 0) ? Number(UI.tripEnergyKwh) : 0;
          const distKm = (UI.distKm != null && !isNaN(UI.distKm) && UI.distKm > 0) ? Number(UI.distKm) : 0;
          const refName = (typeof tripDestName === 'string' && tripDestName.trim()) ? tripDestName.trim() : 'Destino';
          const dlat = (tripDestLat != null && !isNaN(tripDestLat)) ? Number(tripDestLat) : 0;
          const dlng = (tripDestLng != null && !isNaN(tripDestLng)) ? Number(tripDestLng) : 0;
          const payload = JSON.stringify({{ kwh: kwh, distKm: distKm, ref: refName, dlat: dlat, dlng: dlng }});
          const b64 = btoa(unescape(encodeURIComponent(payload)));
          evaOpenNative('consumo/registrar-viaje?p=' + encodeURIComponent(b64));
        }} catch (e0) {{}}
        setState('normal');
      }});
    }}

    function escapeHtml(s) {{
      if (s == null) return '';
      return String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/""/g,'&quot;');
    }}

    function distKmJs(lng1, lat1, lng2, lat2) {{
      const R = 6371;
      const toR = Math.PI / 180;
      const dLat = (lat2 - lat1) * toR;
      const dLng = (lng2 - lng1) * toR;
      const a = Math.sin(dLat / 2) * Math.sin(dLat / 2)
        + Math.cos(lat1 * toR) * Math.cos(lat2 * toR) * Math.sin(dLng / 2) * Math.sin(dLng / 2);
      return R * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    }}

    function remainingRouteDistanceM() {{
      const coords = tripRouteCoords;
      if (!coords || coords.length < 2) return null;
      const from = Math.max(0, Math.min(tripRouteSliceIndex, coords.length - 1));
      let sum = 0;
      for (let i = from; i < coords.length - 1; i++) {{
        const a = coords[i], b = coords[i + 1];
        if (!a || !b || a.length < 2 || b.length < 2) continue;
        sum += distKmJs(a[0], a[1], b[0], b[1]) * 1000;
      }}
      return sum;
    }}

    function formatEtaCountdown(sec) {{
      const s = Math.max(0, Math.round(Number(sec) || 0));
      if (s < 3600) return Math.max(1, Math.round(s / 60)) + ' min';
      const h = Math.floor(s / 3600);
      const m = Math.round((s % 3600) / 60);
      return h + ' h ' + (m < 10 ? '0' : '') + m + ' min';
    }}

    function removeTripRouteFromMap() {{
      if (!map) {{
        tripRouteCoords = [];
        tripRouteSliceIndex = 0;
        return;
      }}
      try {{ if (map.getLayer('trip-route-line')) map.removeLayer('trip-route-line'); }} catch (_) {{}}
      try {{ if (map.getSource('trip-route')) map.removeSource('trip-route'); }} catch (_) {{}}
      tripRouteCoords = [];
      tripRouteSliceIndex = 0;
    }}

    function updateRemainingTripRoute(lng, lat) {{
      if (!map) return;
      if (!tripRouteCoords || tripRouteCoords.length < 2) return;
      if (lng == null || lat == null || isNaN(lng) || isNaN(lat)) return;
      const start = Math.max(0, tripRouteSliceIndex - 8);
      const end = Math.min(tripRouteCoords.length - 1, tripRouteSliceIndex + 220);
      let bestI = tripRouteSliceIndex;
      let bestM = 1e18;
      for (let i = start; i <= end; i++) {{
        const c = tripRouteCoords[i];
        if (!c || c.length < 2) continue;
        const dM = distKmJs(lng, lat, c[0], c[1]) * 1000;
        if (dM < bestM) {{ bestM = dM; bestI = i; }}
      }}
      lastRouteSnapDistanceM = bestM;
      if (bestM > 160) return;
      if (bestI <= tripRouteSliceIndex) return;
      tripRouteSliceIndex = bestI;
      const remain = tripRouteCoords.slice(tripRouteSliceIndex);
      if (remain.length < 2) return;
      try {{
        const src = map.getSource('trip-route');
        if (src && src.setData) {{
          src.setData({{ type: 'Feature', properties: {{}}, geometry: {{ type: 'LineString', coordinates: remain }} }});
        }}
      }} catch (_) {{}}
    }}

    function drawTripRouteFromGeoJson(geometry) {{
      if (!map) return;
      if (!geometry) return;
      removeTripRouteFromMap();
      try {{
        tripRouteCoords = (geometry && geometry.coordinates && geometry.coordinates.length) ? geometry.coordinates : [];
        tripRouteSliceIndex = 0;
        map.addSource('trip-route', {{
          type: 'geojson',
          data: {{ type: 'Feature', properties: {{}}, geometry }}
        }});
        map.addLayer({{
          id: 'trip-route-line',
          type: 'line',
          source: 'trip-route',
          layout: {{ 'line-cap': 'round', 'line-join': 'round' }},
          paint: {{ 'line-color': '#3b82f6', 'line-width': 6, 'line-opacity': 0.92 }}
        }});
      }} catch (_) {{}}
    }}

    function requestMapboxDrivingRoute(olng, olat, dlng, dlat, onSuccess, onError) {{
      const token = mapboxgl.accessToken;
      const coordPath = olng + ',' + olat + ';' + dlng + ',' + dlat;
      const u = 'https://api.mapbox.com/directions/v5/mapbox/driving/'
        + coordPath
        + '?access_token=' + encodeURIComponent(token)
        + '&geometries=geojson&overview=full&steps=true&language=es';
      fetch(u)
        .then((r) => {{ if (!r.ok) throw new Error('directions'); return r.json(); }})
        .then((data) => {{
          const rt = data && data.routes && data.routes[0];
          if (!rt || rt.distance == null) {{
            tripRouteGeometry = null;
            tripRouteCoords = [];
            tripRouteSliceIndex = 0;
            if (onError) onError();
            return;
          }}
          tripRouteGeometry = rt.geometry || null;
          tripRouteCoords = (rt.geometry && rt.geometry.coordinates && rt.geometry.coordinates.length) ? rt.geometry.coordinates : [];
          tripRouteSliceIndex = 0;
          const leg = rt.legs && rt.legs[0];
          const steps = (leg && leg.steps) ? leg.steps : [];
          if (onSuccess) onSuccess({{ distance: rt.distance, duration: rt.duration, geometry: rt.geometry, steps }});
        }})
        .catch(() => {{
          tripRouteGeometry = null;
          tripRouteCoords = [];
          tripRouteSliceIndex = 0;
          if (onError) onError();
        }});
    }}

    function maybeRerouteIfOffRoute() {{
      if (UI.state !== 'navigating') return;
      if (!map) return;
      if (tripDestLng == null || tripDestLat == null) return;
      if (lastUserLng == null || lastUserLat == null) return;
      if (!tripRouteCoords || tripRouteCoords.length < 2) return;
      const d = (lastRouteSnapDistanceM == null || isNaN(lastRouteSnapDistanceM)) ? 9999 : lastRouteSnapDistanceM;
      if (d < 140) return;
      const now = Date.now();
      if (_rerouteInFlight) return;
      if (now - _lastRerouteAt < 12000) return;
      _rerouteInFlight = true;
      _lastRerouteAt = now;
      requestMapboxDrivingRoute(lastUserLng, lastUserLat, tripDestLng, tripDestLat, (route) => {{
        try {{
          tripRouteGeometry = route.geometry;
          tripTotalRouteDistM = (route.distance != null && !isNaN(route.distance)) ? route.distance : tripTotalRouteDistM;
          tripTotalRouteDurSec = (route.duration != null && !isNaN(route.duration) && route.duration > 0) ? route.duration : tripTotalRouteDurSec;
          navRouteSteps = Array.isArray(route.steps) ? route.steps : [];
          navStepIndex = 0;
          removeTripRouteFromMap();
          drawTripRouteFromGeoJson(tripRouteGeometry);
          applyNavCameraFit();
          updateNavTurnInstruction();
        }} catch (_) {{}}
        _rerouteInFlight = false;
      }}, () => {{
        _rerouteInFlight = false;
      }});
    }}

    function updateChargerBadgeVisibility() {{
      if (!map) return;
      const z = map.getZoom();
      const showLabels = z >= CHARGER_LABEL_MIN_ZOOM;
      const nav = (UI.state === 'navigating');
      const navRadiusKm = 1.4;
      const hasUser = (lastUserLng != null && lastUserLat != null && !isNaN(lastUserLng) && !isNaN(lastUserLat));
      chargerMarkersMeta.forEach((meta) => {{
        const badge = meta.wrapEl && meta.wrapEl.querySelector('.chargerBadge');
        if (!badge) return;
        if (nav && hasUser) {{
          const dKm = distKmJs(lastUserLng, lastUserLat, meta.lng, meta.lat);
          const isDest = tripDestIsCharger && tripDestLng != null && tripDestLat != null
            && Math.abs(Number(meta.lng) - Number(tripDestLng)) < 0.0002
            && Math.abs(Number(meta.lat) - Number(tripDestLat)) < 0.0002;
          meta.wrapEl.style.display = (isDest || dKm <= navRadiusKm) ? '' : 'none';
        }} else {{
          meta.wrapEl.style.display = '';
        }}
        if (showLabels) badge.classList.add('visible');
        else badge.classList.remove('visible');
      }});
    }}

    function closeChargerInfoModal() {{
      UI.state = 'normal';
      show(qs('modalOverlay'), false);
    }}

    function formatChargerConnections(station) {{
      const raw = station.Connections || station.connections;
      if (!raw || !raw.length) return '—';
      const parts = [];
      for (let i = 0; i < raw.length; i++) {{
        const c = raw[i] || {{}};
        const t = c.ConnectionType || c.connectionType || '';
        const p = c.PowerKW != null ? c.PowerKW : c.powerKW;
        const ts = String(t || 'Conector').trim();
        if (p != null && !isNaN(Number(p)) && Number(p) > 0) parts.push(ts + ' · ' + Number(p) + ' kW');
        else if (ts) parts.push(ts);
      }}
      return parts.length ? parts.join(' · ') : '—';
    }}

    function openChargerInfoModal(station) {{
      if (!station) return;
      const lng = Number(station.Longitude != null ? station.Longitude : station.longitude);
      const lat = Number(station.Latitude != null ? station.Latitude : station.latitude);
      if (isNaN(lng) || isNaN(lat)) return;
      const name = escapeHtml(station.Name || station.name || 'Cargador');
      const addr = escapeHtml(station.Address || station.address || 'Sin dirección');
      const st = escapeHtml(station.OperationalStatus || station.operationalStatus || '—');
      const ut = escapeHtml(station.UsageType || station.usageType || '—');
      const connectors = escapeHtml(formatChargerConnections(station));
      UI.state = 'chargerInfo';
      qs('modal').innerHTML = `
        <div class='modalTitle'>Cargador</div>
        <div class='modalSub' style='text-align:left;margin-top:8px;font-weight:700'>${{name}}</div>
        <div class='modalSub' style='text-align:left;margin-top:10px;line-height:1.45'>${{addr}}</div>
        <div class='infoGrid' style='margin-top:14px'>
          <div class='infoCard'><div class='k'>Conectores / potencia</div><div class='v' style='font-size:13px'>${{connectors}}</div></div>
          <div class='infoCard'><div class='k'>Estado</div><div class='v' style='font-size:13px'>${{st}}</div></div>
          <div class='infoCard'><div class='k'>Tipo de uso</div><div class='v' style='font-size:13px'>${{ut}}</div></div>
        </div>
        <button type='button' id='chargerInfoStart' class='btnPrimary' style='margin-top:16px'><span>▶</span> Iniciar viaje aquí</button>
        <button type='button' id='chargerInfoClose' class='btnSecondary' style='margin-top:10px'>Cerrar</button>
      `;
      qs('chargerInfoClose').addEventListener('click', () => closeChargerInfoModal());
      qs('chargerInfoStart').addEventListener('click', () => {{
        const rawName = station.Name || station.name || 'Cargador';
        show(qs('modalOverlay'), false);
        UI.state = 'normal';
        tripDestIsCharger = true;
        openTripToDestination(lng, lat, rawName);
      }});
      show(qs('modalOverlay'), true);
    }}

    let userMarker = null;
    let geoWatchId = null;
    const defaultMapCenter = [{ilng}, {ilat}];
    const __evaSeededUserPos = {seededJs};
    let lastUserLng = __evaSeededUserPos ? defaultMapCenter[0] : null;
    let lastUserLat = __evaSeededUserPos ? defaultMapCenter[1] : null;
    let lastUserSpeedMs = null;
    let lastUserHeadingDeg = null;
    let navArrivalDone = false;
    const NAV_ARRIVAL_KM = 0.065;
    let lastRouteSnapDistanceM = null;
    let _lastMoveLng = null;
    let _lastMoveLat = null;
    let _lastMoveAt = 0;
    let _rerouteInFlight = false;
    let _lastRerouteAt = 0;
    const chargerMarkers = [];
    let _lastNavCamLng = null;
    let _lastNavCamLat = null;
    let _lastNavCamAt = 0;

    function bearingDeg(fromLng, fromLat, toLng, toLat) {{
      const toR = Math.PI / 180;
      const y = Math.sin((toLng - fromLng) * toR) * Math.cos(toLat * toR);
      const x = Math.cos(fromLat * toR) * Math.sin(toLat * toR)
        - Math.sin(fromLat * toR) * Math.cos(toLat * toR) * Math.cos((toLng - fromLng) * toR);
      const brng = Math.atan2(y, x) * 180 / Math.PI;
      return (brng + 360) % 360;
    }}

    function buildExploreMarkerHtml() {{
      return `<div class='userPulse'></div><div class='userDot'></div>`;
    }}

    function buildNavPuckMarkerHtml() {{
      return `<div class='userNavGlow'></div><div class='userNavArrow'><svg viewBox='0 0 64 64' aria-hidden='true'><path d='M32 5 L54 54 L32 43 L10 54 Z' fill='#38BDF8' stroke='rgba(255,255,255,.95)' stroke-width='2.2' stroke-linejoin='round'/></svg></div>`;
    }}

    function buildUserMarkerEl() {{
      const el = document.createElement('div');
      el.className = 'userWrap';
      el.innerHTML = buildExploreMarkerHtml();
      return el;
    }}

    function newUserMarker(lng, lat) {{
      return new mapboxgl.Marker({{
        element: buildUserMarkerEl(),
        anchor: 'center'
      }}).setLngLat([lng, lat]).addTo(map);
    }}

    function applyUserMarkerNavVisual(isNav) {{
      if (!userMarker) return;
      const el = userMarker.getElement();
      if (!el) return;
      el.className = isNav ? 'userWrap userWrapNav' : 'userWrap';
      el.innerHTML = isNav ? buildNavPuckMarkerHtml() : buildExploreMarkerHtml();
    }}

    function syncNavMarkerRotation() {{
      if (!userMarker || UI.state !== 'navigating') return;
      try {{ userMarker.setRotation(0); }} catch (_) {{}}
    }}

    function followNavCamera(lng, lat, headingDeg, initial) {{
      if (UI.state !== 'navigating') return;
      if (lng == null || lat == null || isNaN(lng) || isNaN(lat)) return;
      const h = (headingDeg != null && !isNaN(headingDeg)) ? headingDeg : 0;
      const now = Date.now();
      const movedM = (_lastNavCamLng == null) ? 999 : distKmJs(lng, lat, _lastNavCamLng, _lastNavCamLat) * 1000;
      const minMs = initial ? 0 : 450;
      if (!initial && now - _lastNavCamAt < minMs && movedM < 2.5) return;
      _lastNavCamAt = now;
      _lastNavCamLng = lng;
      _lastNavCamLat = lat;
      try {{
        map.setPadding({{ top: 88, bottom: 220, left: 20, right: 72 }});
        map.easeTo({{
          center: [lng, lat],
          zoom: Math.max(map.getZoom(), 17),
          pitch: 60,
          bearing: h,
          offset: [0, 115],
          duration: initial ? 900 : 650,
          easing: (t) => t,
          essential: true
        }});
      }} catch (_) {{}}
    }}

    function applyNavCameraFit() {{
      if (lastUserLng == null || lastUserLat == null) return;
      const h = (lastUserHeadingDeg != null && !isNaN(lastUserHeadingDeg)) ? lastUserHeadingDeg : 0;
      followNavCamera(lastUserLng, lastUserLat, h, true);
    }}

    function maybeFollowNavCamera(lng, lat) {{
      const h = (lastUserHeadingDeg != null && !isNaN(lastUserHeadingDeg)) ? lastUserHeadingDeg : 0;
      followNavCamera(lng, lat, h, false);
    }}

    function resetTripMapView() {{
      document.body.classList.remove('map-navigating');
      navArrivalDone = false;
      _lastNavCamLng = null;
      _lastNavCamLat = null;
      _lastNavCamAt = 0;
      removeTripRouteFromMap();
      try {{
        map.setPadding({{ top: 0, bottom: 0, left: 0, right: 0 }});
        map.easeTo({{ pitch: 0, bearing: 0, duration: 500, essential: true }});
      }} catch (_) {{}}
      try {{
        applyUserMarkerNavVisual(false);
        if (userMarker) userMarker.setRotation(0);
      }} catch (_) {{}}
    }}

    function getStepEndCoord(step) {{
      if (!step) return null;
      const g = step.geometry;
      if (g && g.coordinates && g.coordinates.length) {{
        const c = g.coordinates[g.coordinates.length - 1];
        if (c && c.length >= 2) return c;
      }}
      if (step.maneuver && step.maneuver.location && step.maneuver.location.length >= 2)
        return step.maneuver.location;
      return null;
    }}

    function humanizeNavInstruction(raw) {{
      if (raw == null || typeof raw !== 'string') return '';
      let s = raw.trim();
      if (!s) return '';
      const i = s.indexOf(' / ');
      if (i > 0) {{
        s = s.slice(0, i).trim();
      }}
      s = s.replace(/\s*\/\s*/g, ' ').replace(/\s+/g, ' ').trim();
      s = s.replace(/\s*\([^)]{{0,40}}\)\s*$/g, '').trim();
      return s;
    }}

    function navGlyphForManeuver(step) {{
      try {{
        if (!step || !step.maneuver) return '\\u27A1';
        const t = String(step.maneuver.type || '').toLowerCase();
        const mod = String(step.maneuver.modifier || '').toLowerCase();
        if (t.indexOf('roundabout') >= 0 || t.indexOf('rotary') >= 0) return '\\u21BB';
        if (t.indexOf('arrive') >= 0) return '\\u25CE';
        if (t.indexOf('turn') >= 0) {{
          if (mod.indexOf('left') >= 0 || t.indexOf('left') >= 0) return '\\u21B0';
          if (mod.indexOf('right') >= 0 || t.indexOf('right') >= 0) return '\\u21B1';
        }}
        if (t.indexOf('merge') >= 0) return '\\u2934';
        if (t.indexOf('fork') >= 0) return '\\u2A02';
        if (t.indexOf('off ramp') >= 0 || t.indexOf('off ramp') >= 0) return '\\u21E2';
        return '\\u2191';
      }} catch (_) {{
        return '\\u27A1';
      }}
    }}

    function updateNavTurnInstruction() {{
      const banner = qs('navTurnBanner');
      if (!banner || UI.state !== 'navigating') return;
      if (lastUserLng == null || lastUserLat == null) return;
      if (!navRouteSteps.length) {{
        banner.innerHTML = '<div class=""navTurnInner""><div class=""navTurnGlyph"">\\u25CE</div><div class=""navTurnText"">' + escapeHtml('Sigue la ruta hacia el destino.') + '</div></div>';
        return;
      }}
      while (navStepIndex < navRouteSteps.length - 1) {{
        const step = navRouteSteps[navStepIndex];
        const end = getStepEndCoord(step);
        if (!end) break;
        const dM = distKmJs(lastUserLng, lastUserLat, end[0], end[1]) * 1000;
        if (dM < 42) navStepIndex++;
        else break;
      }}
      const cur = navRouteSteps[navStepIndex];
      let txt = 'Sigue la ruta';
      if (cur) {{
        if (cur.maneuver && cur.maneuver.instruction) {{
          txt = humanizeNavInstruction(cur.maneuver.instruction);
          if (!txt) txt = 'Sigue la ruta';
        }} else if (cur.name) {{
          txt = 'Continúa por ' + String(cur.name).split(' / ')[0].trim();
        }}
      }}
      const g = navGlyphForManeuver(cur);
      banner.innerHTML = '<div class=""navTurnInner""><div class=""navTurnGlyph"">' + g + '</div><div class=""navTurnText"">' + escapeHtml(txt) + '</div></div>';
    }}

    function updateNavigationHud() {{
      if (UI.state !== 'navigating' || tripDestLng == null || lastUserLng == null) return;
      const remKmBird = distKmJs(lastUserLng, lastUserLat, tripDestLng, tripDestLat);
      let dispSpeed = 0;
      if (lastUserSpeedMs != null && lastUserSpeedMs > 0.4) {{
        dispSpeed = Math.round(lastUserSpeedMs * 3.6);
      }}
      setText('speedValue', dispSpeed > 0 ? String(dispSpeed) : '—');
      const cruiseKmh = Math.max(dispSpeed, 22);
      let remSec = null;
      const remAlong = remainingRouteDistanceM();
      const totalM = tripTotalRouteDistM;
      if (remAlong != null && totalM > 80 && tripTotalRouteDurSec > 5) {{
        const ratio = Math.min(1, Math.max(0, remAlong / totalM));
        remSec = tripTotalRouteDurSec * ratio;
      }}
      if (remSec == null || isNaN(remSec)) {{
        remSec = Math.max(60, (remKmBird / Math.max(cruiseKmh, 8)) * 3600);
      }}
      const showDistKm = (remAlong != null && remAlong > 5) ? (remAlong / 1000) : remKmBird;
      if (showDistKm < 1) {{
        setText('etaDist', Math.max(0, Math.round(showDistKm * 1000)) + ' m');
      }} else {{
        setText('etaDist', showDistKm.toFixed(1) + ' km');
      }}
      setText('etaTime', formatEtaCountdown(remSec));
      const badge = qs('trafficBadge');
      if (remKmBird < 0.25) {{
        badge.textContent = 'Casi llegando';
      }} else {{
        badge.textContent = 'En ruta';
      }}
      updateRemainingTripRoute(lastUserLng, lastUserLat);
      maybeRerouteIfOffRoute();
      updateNavTurnInstruction();
    }}

    function checkDestinationArrival() {{
      if (navArrivalDone || UI.state !== 'navigating') return;
      if (tripDestLng == null || lastUserLng == null) return;
      const rem = distKmJs(lastUserLng, lastUserLat, tripDestLng, tripDestLat);
      if (rem <= NAV_ARRIVAL_KM) {{
        navArrivalDone = true;
        finishTripByArrival();
      }}
    }}

    function finishTripByArrival() {{
      if (UI.state !== 'navigating') return;
      const elapsedMs = (navTripStartMs > 0) ? (Date.now() - navTripStartMs) : 0;
      UI.timeMin = Math.max(1, Math.round(elapsedMs / 60000));
      UI.distKm = (tripPlannedDistKm != null && !isNaN(tripPlannedDistKm) && tripPlannedDistKm > 0) ? tripPlannedDistKm : UI.distKm;
      if (UI.distKm == null || isNaN(UI.distKm) || UI.distKm <= 0) {{
        UI.distKm = (tripTotalRouteDistM > 50) ? Math.round(tripTotalRouteDistM / 100) / 10 : 0;
      }}
      if (UI.navTick) {{ clearInterval(UI.navTick); UI.navTick = null; }}
      if (UI.navTimer) {{ clearTimeout(UI.navTimer); UI.navTimer = null; }}
      setState('summary');
    }}

    let userLocationFirstFix = true;
    function applyUserLocation(lng, lat, heading, speedMs) {{
      lastUserLng = lng;
      lastUserLat = lat;
      if (speedMs != null && !isNaN(speedMs) && speedMs >= 0) {{
        lastUserSpeedMs = speedMs;
      }}
      if (!userMarker) {{
        userMarker = newUserMarker(lng, lat);
      }} else {{
        userMarker.setLngLat([lng, lat]);
      }}
      if (UI.state === 'navigating') {{
        updateNavigationHud();
        checkDestinationArrival();
        const now = Date.now();
        if ((heading == null || isNaN(heading)) && _lastMoveLng != null && _lastMoveLat != null) {{
          const movedM = distKmJs(_lastMoveLng, _lastMoveLat, lng, lat) * 1000;
          if (movedM > 2.2 && now - _lastMoveAt < 8000) {{
            heading = bearingDeg(_lastMoveLng, _lastMoveLat, lng, lat);
          }}
        }}
        _lastMoveLng = lng;
        _lastMoveLat = lat;
        _lastMoveAt = now;
        const h = heading != null && !isNaN(heading) ? heading : lastUserHeadingDeg;
        if (h != null && !isNaN(h)) {{
          lastUserHeadingDeg = h;
          syncNavMarkerRotation();
          const c = qs('compass');
          if (c) {{
            const dirs = ['N','NE','E','SE','S','SW','W','NW'];
            const i = (Math.round(((h % 360) / 45)) % 8 + 8) % 8;
            c.textContent = dirs[i];
          }}
        }}
        maybeFollowNavCamera(lng, lat);
      }} else {{
        if (heading != null && !isNaN(heading)) {{
          lastUserHeadingDeg = heading;
        }}
      }}
      if (userLocationFirstFix) {{
        userLocationFirstFix = false;
        try {{
          const c = map.getCenter();
          const dKm = distKmJs(c.lng, c.lat, lng, lat);
          const z = Math.max(map.getZoom(), 13);
          if (dKm < 0.12) {{
            map.jumpTo({{ center: [lng, lat], zoom: z, essential: true }});
          }} else if (dKm < 1.8) {{
            map.easeTo({{ center: [lng, lat], zoom: z, essential: true, duration: 280 }});
          }} else {{
            map.flyTo({{ center: [lng, lat], zoom: z, essential: true, duration: 700 }});
          }}
        }} catch (_e) {{
          map.flyTo({{ center: [lng, lat], zoom: Math.max(map.getZoom(), 13), essential: true, duration: 700 }});
        }}
      }}
      maybeRequestChargerRefresh(lng, lat);
      updateChargerBadgeVisibility();
    }}

    function onGeoError(err) {{
      console.warn('Geolocalización:', err && err.message);
      if (!userMarker) {{
        userMarker = newUserMarker(defaultMapCenter[0], defaultMapCenter[1]);
        lastUserLng = defaultMapCenter[0];
        lastUserLat = defaultMapCenter[1];
      }}
      updateChargerBadgeVisibility();
    }}

    function startUserGeolocation() {{
      if (!navigator.geolocation) {{
        onGeoError(null);
        return;
      }}
      const opts = {{ enableHighAccuracy: true, timeout: 22000, maximumAge: 120000 }};
      navigator.geolocation.getCurrentPosition(
        (pos) => applyUserLocation(pos.coords.longitude, pos.coords.latitude, pos.coords.heading, pos.coords.speed),
        onGeoError,
        opts
      );
      geoWatchId = navigator.geolocation.watchPosition(
        (pos) => applyUserLocation(pos.coords.longitude, pos.coords.latitude, pos.coords.heading, pos.coords.speed),
        () => {{ }},
        opts
      );
    }}

    function pinSvg() {{
      return `<svg class='pinSvg' viewBox='0 0 24 24' aria-hidden='true'>
        <path d='M12 22s7-6 7-12a7 7 0 1 0-14 0c0 6 7 12 7 12z'></path>
      </svg>`;
    }}

    function clearChargerMarkers() {{
      while (chargerMarkers.length) {{
        const m = chargerMarkers.pop();
        try {{ m.remove(); }} catch (_) {{}}
      }}
      chargerMarkersMeta.length = 0;
    }}

    function loadChargersFromApiB64(b64) {{
      try {{
        const raw = atob(String(b64 || ''));
        const stations = JSON.parse(raw);
        loadChargersFromApi(stations);
      }} catch (e) {{
        console.warn('loadChargersFromApiB64', e);
        loadChargersFromApi([]);
      }}
    }}

    function loadChargersFromApi(stations) {{
      clearChargerMarkers();
      if (!stations || stations.length === 0) {{
        return;
      }}
      const root = getComputedStyle(document.documentElement);
      const green = root.getPropertyValue('--green').trim();
      const orange = root.getPropertyValue('--orange').trim();
      const red = root.getPropertyValue('--red').trim();
      for (let i = 0; i < stations.length; i++) {{
        const s = stations[i];
        const st = String(s.OperationalStatus || s.operationalStatus || '').toLowerCase();
        const ut = String(s.UsageType || s.usageType || '').toLowerCase();
        let color = green;
        if (st.indexOf('out') >= 0 || st.indexOf('fuera') >= 0 || st.indexOf('offline') >= 0) {{
          color = red;
        }} else if (ut.indexOf('use') >= 0 || ut.indexOf('uso') >= 0 || ut.indexOf('occupied') >= 0) {{
          color = orange;
        }}
        const lng = Number(s.Longitude != null ? s.Longitude : s.longitude);
        const lat = Number(s.Latitude != null ? s.Latitude : s.latitude);
        if (lng == null || lat == null || isNaN(lng) || isNaN(lat)) {{
          continue;
        }}
        addChargerMarker(color, lng, lat, s);
      }}
      updateChargerBadgeVisibility();
    }}

    function addChargerMarker(color, lng, lat, station) {{
      const wrap = document.createElement('div');
      wrap.className = 'badgeMarker chargerMarkerRoot';
      const rawName = (station && (station.Name || station.name)) ? (station.Name || station.name) : 'Cargador';
      const labelText = rawName.length > 26 ? rawName.substring(0, 26) + '…' : rawName;
      const badgeLabel = '🔌 ' + escapeHtml(labelText);
      wrap.innerHTML = `
        <div class='chargerBadge' style='box-shadow:0 0 0 1px rgba(255,255,255,.06) inset;color:${{color}}'>
          <span style='display:inline-block;width:7px;height:7px;border-radius:999px;background:${{color}};margin-right:6px;vertical-align:middle;box-shadow:0 0 10px ${{color}}88;'></span>
          <span style='vertical-align:middle;color:rgba(255,255,255,.95)'>${{badgeLabel}}</span>
        </div>
        <div class='chargerGround' style='color:${{color}}'>
          <div class='chargerDot' style='background:${{color}}'></div>
          ${{pinSvg()}}
        </div>
      `;
      wrap.addEventListener('click', (e) => {{
        e.stopPropagation();
        openChargerInfoModal(station);
      }});
      const m = new mapboxgl.Marker({{ element: wrap, anchor: 'bottom' }})
        .setLngLat([lng, lat])
        .addTo(map);
      chargerMarkers.push(m);
      chargerMarkersMeta.push({{ wrapEl: wrap, lng, lat }});
    }}

    function flyToMyLocation() {{
      if (lastUserLng != null && lastUserLat != null) {{
        map.flyTo({{
          center: [lastUserLng, lastUserLat],
          zoom: Math.max(map.getZoom(), 16),
          essential: true,
          duration: 950
        }});
        return;
      }}
      if (userMarker) {{
        const ll = userMarker.getLngLat();
        map.flyTo({{ center: [ll.lng, ll.lat], zoom: Math.max(map.getZoom(), 16), essential: true, duration: 950 }});
        return;
      }}
      navigator.geolocation.getCurrentPosition(
        (pos) => {{
          applyUserLocation(pos.coords.longitude, pos.coords.latitude, pos.coords.heading, pos.coords.speed);
          map.flyTo({{
            center: [pos.coords.longitude, pos.coords.latitude],
            zoom: 16,
            essential: true,
            duration: 950
          }});
        }},
        () => {{}},
        {{ enableHighAccuracy: true, timeout: 15000, maximumAge: 0 }}
      );
    }}

    function setState(next) {{
      UI.state = next;
      const badge = qs('trafficBadge');

      if (next === 'normal') {{
        resetTripMapView();
        navRouteSteps = [];
        navStepIndex = 0;
        const ntb = qs('navTurnBanner');
        if (ntb) ntb.style.display = 'none';
        setFabExpanded(false);
        setSearchBarVisible(false);
        show(qs('modalOverlay'), false);
        setVisibleNormalBoxes(true);
        badge.classList.remove('blue');
        badge.textContent = 'Sin tráfico';
        setText('speedValue', '0');
        setText('etaTime', '0');
        setText('etaDist', '0');
        if (UI.navTick) {{ clearInterval(UI.navTick); UI.navTick = null; }}
        if (UI.navTimer) {{ clearTimeout(UI.navTimer); UI.navTimer = null; }}
        lastUserSpeedMs = null;
        qs('speedBox').style.background = 'rgba(0,0,0,.70)';
        qs('speedBox').style.borderRadius = '16px';
        qs('speedBox').style.padding = '12px';
        qs('speedValue').style.fontSize = '24px';
        tripRouteGeometry = null;
        tripRouteCoords = [];
        tripRouteSliceIndex = 0;
        tripDestLng = null;
        tripDestLat = null;
        tripDestName = '';
        tripTotalRouteDistM = 0;
        tripTotalRouteDurSec = 0;
        navTripStartMs = 0;
        tripPlannedDistKm = 0;
        UI.tripDurationSec = 0;
        UI.tripEnergyKwh = 0;
        UI.tripCostCrc = 0;
        UI.tripBatteryPercentUsed = null;
        hideSearchSuggestions();
        return;
      }}

      if (next === 'menu') {{
        setVisibleNormalBoxes(false);
        setFabExpanded(true);
        setSearchBarVisible(true);
        show(qs('modalOverlay'), false);
        setTimeout(() => {{
          try {{
            const si = qs('searchInput');
            if (si) si.focus();
          }} catch (_) {{}}
        }}, 320);
        return;
      }}

      if (next === 'navigating') {{
        show(qs('modalOverlay'), false);
        setSearchBarVisible(false);
        document.body.classList.add('map-navigating');
        setVisibleNormalBoxes(true);
        navTripStartMs = Date.now();
        tripPlannedDistKm = (UI.distKm != null && !isNaN(UI.distKm)) ? UI.distKm : 0;
        const ntb = qs('navTurnBanner');
        if (ntb) {{
          ntb.style.display = 'flex';
          updateNavTurnInstruction();
        }}

        qs('speedBox').style.background = 'rgba(0,0,0,.80)';
        qs('speedBox').style.border = '1px solid rgba(255,255,255,.10)';
        qs('speedBox').style.padding = '16px';
        qs('speedBox').style.minWidth = '100px';
        qs('speedValue').style.fontSize = '48px';

        badge.classList.add('blue');
        badge.textContent = 'En ruta';
        drawTripRouteFromGeoJson(tripRouteGeometry);
        updateNavigationHud();

        applyUserMarkerNavVisual(true);
        syncNavMarkerRotation();
        applyNavCameraFit();

        UI.navTick = setInterval(() => {{
          if (UI.state !== 'navigating') return;
          updateNavigationHud();
          checkDestinationArrival();
        }}, 2000);
        return;
      }}

      if (next === 'summary') {{
        resetTripMapView();
        if (UI.navTick) {{ clearInterval(UI.navTick); UI.navTick = null; }}
        if (UI.navTimer) {{ clearTimeout(UI.navTimer); UI.navTimer = null; }}
        setVisibleNormalBoxes(false);
        setFabExpanded(false);
        setSearchBarVisible(false);
        renderSummaryModal();
        show(qs('modalOverlay'), true);
        return;
      }}
    }}

    let _chargerVisRaf = null;
    function scheduleChargerBadgeUpdate() {{
      if (_chargerVisRaf != null) return;
      _chargerVisRaf = requestAnimationFrame(() => {{
        _chargerVisRaf = null;
        updateChargerBadgeVisibility();
      }});
    }}

    if (map) {{
      map.on('load', () => {{
        evaApplySystemInsets();
        const _readyTimer = setTimeout(() => {{ notifyNativeMapReady(); }}, 6000);
        map.once('idle', () => {{ clearTimeout(_readyTimer); notifyNativeMapReady(); }});
        startUserGeolocation();
        map.on('moveend', scheduleChargerBadgeUpdate);
        map.on('zoom', scheduleChargerBadgeUpdate);
        map.on('click', () => {{
          if (UI.state === 'menu') setState('normal');
        }});
        setState('normal');
      }});

      map.on('error', (e) => {{
        try {{
          setSearchBarVisible(false);
          const msg = (e && e.error && (e.error.message || e.error.toString())) ? (e.error.message || e.error.toString()) : 'Error cargando el mapa.';
          const el = document.getElementById('err');
          el.style.display = 'block';
          el.innerHTML = '<div style=""font-size:14px;font-weight:800;margin-bottom:4px;"">No se pudo cargar el mapa</div><div style=""font-size:12px;opacity:.85;word-break:break-word;"">' + msg + '</div>';
        }} catch (_) {{}}
      }});
    }} else {{
      try {{
        const el = document.getElementById('err');
        if (el) {{
          el.style.display = 'block';
          const msg = (_mapInitError && (_mapInitError.message || _mapInitError.toString())) ? (_mapInitError.message || _mapInitError.toString()) : 'No se pudo inicializar el mapa.';
          el.innerHTML = '<div style=""font-size:14px;font-weight:800;margin-bottom:4px;"">No se pudo cargar el mapa</div><div style=""font-size:12px;opacity:.85;word-break:break-word;"">' + msg + '</div>';
        }}
      }} catch (_) {{}}
    }}
  </script>
</body>
</html>";
        }
    }
}
