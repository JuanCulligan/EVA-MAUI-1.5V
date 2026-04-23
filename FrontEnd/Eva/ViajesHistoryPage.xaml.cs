using Eva.DTO;
using Eva.Entidades;
using Eva.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Eva
{
    public partial class ViajesHistoryPage : ContentPage
    {
        static readonly TimeSpan LongPressMin = TimeSpan.FromMilliseconds(520);
        DateTime _historialPointerDownUtc;
        bool _historialPointerActive;

        public ViajesHistoryPage()
        {
            InitializeComponent();
        }

        private async void OnVolverTapped(object sender, TappedEventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = CargarAsync();
        }

        async void OnRefreshing(object sender, EventArgs e)
        {
            await CargarAsync();
            refreshView.IsRefreshing = false;
        }

        void OnHistorialPointerPressed(object? sender, PointerEventArgs e)
        {
            if (sender is BindableObject bo && bo.BindingContext is LocationRowDto)
            {
                _historialPointerActive = true;
                _historialPointerDownUtc = DateTime.UtcNow;
            }
        }

        async void OnHistorialPointerReleased(object? sender, PointerEventArgs e)
        {
            if (!_historialPointerActive || sender is not BindableObject bo || bo.BindingContext is not LocationRowDto row)
            {
                _historialPointerActive = false;
                return;
            }

            _historialPointerActive = false;
            if (row.Id <= 0)
            {
                return;
            }

            if (DateTime.UtcNow - _historialPointerDownUtc < LongPressMin)
            {
                return;
            }

            await ConfirmarYEliminarHistorialAsync(row);
        }

        async void OnHistorialSwipeEliminar(object? sender, EventArgs e)
        {
            if (sender is null)
            {
                return;
            }

            LocationRowDto? row = RowDtoFromSwipeContext(sender);
            if (row == null || row.Id <= 0)
            {
                return;
            }

            await ConfirmarYEliminarHistorialAsync(row);
        }

        static LocationRowDto? RowDtoFromSwipeContext(object sender)
        {
            if (sender is not BindableObject start)
            {
                return null;
            }

            for (Element? el = start as Element; el != null; el = el.Parent)
            {
                if (el is SwipeView sv && sv.BindingContext is LocationRowDto dto)
                {
                    return dto;
                }
            }

            return null;
        }

        async Task ConfirmarYEliminarHistorialAsync(LocationRowDto row)
        {
            string name = string.IsNullOrWhiteSpace(row.locationName) ? "este destino" : row.locationName.Trim();
            bool ok = await DisplayAlertAsync(
                "Eliminar del historial",
                $"¿Quitar “{name}” del historial?",
                "Eliminar",
                "Cancelar");
            if (!ok)
            {
                return;
            }

            if (Sesion.guid == Guid.Empty)
            {
                return;
            }

            await AppConfiguration.EnsureLoadedAsync();
            if (!AppConfiguration.IsApiBaseUrlConfigured())
            {
                await DisplayAlertAsync("Historial", "Configura ApiBaseUrl.", "Aceptar");
                return;
            }

            try
            {
                using HttpClient client = ApiHttp.CreateClient();
                string url = AppConfiguration.GetApiUrl($"api/viaje/deleteHistory/{Sesion.guid}/{row.Id}");
                using HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Delete, url);
                HttpResponseMessage response = await client.SendAsync(req);
                string body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    await DisplayAlertAsync("Historial", $"No se pudo eliminar ({(int)response.StatusCode}).", "Aceptar");
                    return;
                }

                bool serverOk = true;
                try
                {
                    JObject jo = JObject.Parse(body);
                    JToken? rt = jo["result"] ?? jo["Result"];
                    if (rt?.Type == JTokenType.Boolean)
                    {
                        serverOk = rt.Value<bool>();
                    }
                }
                catch
                {
                }

                if (!serverOk)
                {
                    await DisplayAlertAsync("Historial", "El servidor rechazó la eliminación.", "Aceptar");
                    return;
                }

                if (listViajes.ItemsSource is List<LocationRowDto> list)
                {
                    list.RemoveAll(x => x.Id == row.Id);
                    listViajes.ItemsSource = null;
                    listViajes.ItemsSource = list;
                }
                else if (listViajes.ItemsSource is IEnumerable<LocationRowDto> en)
                {
                    listViajes.ItemsSource = en.Where(x => x.Id != row.Id).ToList();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Historial", ex.Message, "Aceptar");
            }
        }

        async Task CargarAsync()
        {
            lblStatus.Text = string.Empty;
            if (Sesion.guid == Guid.Empty)
            {
                await DisplayAlertAsync("Viajes", "Inicia sesión para ver el historial.", "Aceptar");
                await Shell.Current.GoToAsync("..");
                return;
            }

            await AppConfiguration.EnsureLoadedAsync();
            if (!AppConfiguration.IsApiBaseUrlConfigured())
            {
                await DisplayAlertAsync("Viajes", "Configura ApiBaseUrl en appsettings.json (backend EVABD).", "Aceptar");
                return;
            }

            spinner.IsVisible = true;
            spinner.IsRunning = true;
            listViajes.IsVisible = false;
            try
            {
                using HttpClient client = ApiHttp.CreateClient();
                HttpResponseMessage response = await client.GetAsync(
                    AppConfiguration.GetApiUrl("api/viaje/getRecentLocations/" + Sesion.guid));
                string body = await response.Content.ReadAsStringAsync();

                List<LocationRowDto> items = new List<LocationRowDto>();
                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        JObject jo = JObject.Parse(body);
                        JToken? resultTok = jo["result"] ?? jo["Result"];
                        if (resultTok?.Type == JTokenType.Boolean && resultTok.Value<bool>() == false)
                        {
                            lblStatus.Text = ExtractApiErrorSummary(jo) ?? "No se pudo leer el historial (servidor).";
                        }
                        else
                        {
                            JToken? locTok = jo["locations"] ?? jo["Locations"];
                            if (locTok is JArray arr)
                            {
                                items = arr.ToObject<List<LocationRowDto>>() ?? new List<LocationRowDto>();
                            }
                            else
                            {
                                ResGetRecentLocationsDto? dto = JsonConvert.DeserializeObject<ResGetRecentLocationsDto>(body);
                                if (dto?.locations != null)
                                {
                                    items = dto.locations;
                                }
                            }
                        }
                    }
                    catch
                    {
                        ResGetRecentLocationsDto? dto = JsonConvert.DeserializeObject<ResGetRecentLocationsDto>(body);
                        if (dto?.Result == false)
                        {
                            lblStatus.Text = "No se pudo leer el historial (servidor).";
                        }
                        else if (dto?.locations != null)
                        {
                            items = dto.locations;
                        }
                    }
                }
                else
                {
                    lblStatus.Text = $"El servidor respondió {(int)response.StatusCode}. Revisa la API.";
                }

                listViajes.ItemsSource = items;
            }
            catch (Exception ex)
            {
                lblStatus.Text = "No se pudo cargar: " + ex.Message;
                listViajes.ItemsSource = Array.Empty<LocationRowDto>();
            }
            finally
            {
                spinner.IsVisible = false;
                spinner.IsRunning = false;
                listViajes.IsVisible = true;
            }
        }

        static string? ExtractApiErrorSummary(JObject jo)
        {
            JToken? errs = jo["errors"] ?? jo["Errors"];
            if (errs is not JArray arr || arr.Count == 0)
            {
                return null;
            }

            JToken? first = arr[0];
            JToken? msg = first?["message"] ?? first?["Message"];
            return msg?.ToString();
        }
    }
}
