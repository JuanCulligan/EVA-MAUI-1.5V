using Eva.DTO;
using Eva.Entidades;
using Eva.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace Eva
{
    public partial class ConsumosHistoryPage : ContentPage
    {
        List<ConsumoHistorialRowDto> _items = new List<ConsumoHistorialRowDto>();

        public ConsumosHistoryPage()
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

        void OnSwitchToggled(object sender, ToggledEventArgs e)
        {
            BindList();
        }

        void BindList()
        {
            bool kwh = switchKwh.IsToggled;
            IEnumerable<string> lines = _items.Select(r =>
            {
                string refe = string.IsNullOrWhiteSpace(r.nombreReferencia) ? "—" : r.nombreReferencia;
                string fecha = r.fechaConsumo.ToString("g");
                return kwh
                    ? $"{refe}\n{r.consumoKwh:F2} kWh · {fecha}"
                    : $"{refe}\n₡ {r.monto:N0} · {fecha}";
            });
            listConsumos.ItemsSource = lines.ToList();
        }

        async Task CargarAsync()
        {
            lblStatus.Text = string.Empty;
            if (Sesion.guid == Guid.Empty)
            {
                await DisplayAlertAsync("Consumos", "Inicia sesión para ver consumos.", "Aceptar");
                await Shell.Current.GoToAsync("..");
                return;
            }

            await AppConfiguration.EnsureLoadedAsync();
            if (!AppConfiguration.IsApiBaseUrlConfigured())
            {
                await DisplayAlertAsync("Consumos", "Configura ApiBaseUrl en appsettings.json (backend EVABD).", "Aceptar");
                return;
            }

            spinner.IsVisible = true;
            spinner.IsRunning = true;
            listConsumos.IsVisible = false;
            try
            {
                using HttpClient client = ApiHttp.CreateClient();
                HttpResponseMessage response = await client.GetAsync(
                    AppConfiguration.GetApiUrl("api/consumo/historial/" + Sesion.guid));
                string body = await response.Content.ReadAsStringAsync();

                _items = new List<ConsumoHistorialRowDto>();
                if (response.IsSuccessStatusCode)
                {
                    ResHistorialConsumoDto? dto = JsonConvert.DeserializeObject<ResHistorialConsumoDto>(body);
                    if (dto?.historial != null)
                    {
                        _items = dto.historial;
                    }
                }
                else
                {
                    lblStatus.Text = $"El servidor respondió {(int)response.StatusCode}. Revisa la API.";
                }

                BindList();
            }
            catch (Exception ex)
            {
                lblStatus.Text = "No se pudo cargar: " + ex.Message;
                _items = new List<ConsumoHistorialRowDto>();
                BindList();
            }
            finally
            {
                spinner.IsVisible = false;
                spinner.IsRunning = false;
                listConsumos.IsVisible = true;
            }
        }

        async void OnRegistrarConsumoClicked(object sender, EventArgs e)
        {
            if (Sesion.guid == Guid.Empty)
            {
                await DisplayAlertAsync("Consumos", "Inicia sesión.", "Aceptar");
                return;
            }

            await AppConfiguration.EnsureLoadedAsync();
            if (!AppConfiguration.IsApiBaseUrlConfigured())
            {
                await DisplayAlertAsync("Consumos", "Configura ApiBaseUrl en appsettings.json.", "Aceptar");
                return;
            }

            DTOConsumoGuardar dto = new DTOConsumoGuardar
            {
                guid = Sesion.guid,
                nombreReferencia = "App móvil — consumos",
                TotalEnergyWh = 18000,
                PercentUsed = 10
            };

            try
            {
                using HttpClient client = ApiHttp.CreateClient();
                string json = JsonConvert.SerializeObject(dto);
                using StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                HttpResponseMessage response = await client.PostAsync(AppConfiguration.GetApiUrl("api/consumo/guardar"), content);
                string respBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlertAsync("Consumos", "Consumo registrado.", "Aceptar");
                    await CargarAsync();
                }
                else
                {
                    string snippet = string.IsNullOrWhiteSpace(respBody) ? "" : (respBody.Length > 160 ? respBody[..160] + "…" : respBody);
                    await DisplayAlertAsync("Consumos", "Error " + (int)response.StatusCode + "\n" + snippet, "Aceptar");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Consumos", "No se pudo enviar: " + ex.Message, "Aceptar");
            }
        }

        static string? ExtractApiErrorSummary(string body)
        {
            try
            {
                JObject jo = JObject.Parse(body);
                JToken? errs = jo["errors"] ?? jo["Errors"];
                if (errs is not JArray arr || arr.Count == 0)
                {
                    return null;
                }

                JToken? first = arr[0];
                JToken? msg = first?["message"] ?? first?["Message"];
                return msg?.ToString();
            }
            catch
            {
                return null;
            }
        }
    }
}
