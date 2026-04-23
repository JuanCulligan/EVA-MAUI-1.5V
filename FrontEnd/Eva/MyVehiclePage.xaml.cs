using Eva.DTO;
using Eva.Entidades;
using Eva.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;

namespace Eva
{
    public partial class MyVehiclePage : ContentPage
    {
        bool _hasVehicleInDb;

        public MyVehiclePage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            entryWhKm.Text = TripEstimatePreferences.GetWhPerKm().ToString(CultureInfo.InvariantCulture);
            entryCrcKwh.Text = TripEstimatePreferences.GetCrcPerKwh().ToString(CultureInfo.InvariantCulture);
            _ = LoadVehicleAsync();
        }

        private async void OnVolverTapped(object sender, TappedEventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }

        async Task LoadVehicleAsync()
        {
            entryBrand.Text = string.Empty;
            entryModel.Text = string.Empty;
            entryYear.Text = string.Empty;
            _hasVehicleInDb = false;
            btnDelete.IsVisible = false;
            btnSave.Text = "Guardar vehículo";

            if (Sesion.guid == Guid.Empty)
            {
                return;
            }

            await AppConfiguration.EnsureLoadedAsync();
            if (!AppConfiguration.IsApiBaseUrlConfigured())
            {
                return;
            }

            spinner.IsVisible = true;
            spinner.IsRunning = true;
            try
            {
                using HttpClient client = ApiHttp.CreateClient();
                HttpResponseMessage response = await client.GetAsync(
                    AppConfiguration.GetApiUrl("api/vehicle/" + Sesion.guid));
                string body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    return;
                }

                ResGetVehicleDto? dto = null;
                try
                {
                    dto = JsonConvert.DeserializeObject<ResGetVehicleDto>(body);
                }
                catch
                {
                    JObject jo = JObject.Parse(body);
                    bool ok = (jo["Result"]?.Value<bool>() == true) || (jo["result"]?.Value<bool>() == true);
                    if (!ok)
                    {
                        return;
                    }

                    dto = new ResGetVehicleDto
                    {
                        Result = true,
                        Brand = jo["Brand"]?.ToString() ?? jo["brand"]?.ToString(),
                        Model = jo["Model"]?.ToString() ?? jo["model"]?.ToString(),
                        Year = jo["Year"]?.Value<int>() ?? jo["year"]?.Value<int>() ?? 0
                    };
                }

                if (dto?.Result != true || string.IsNullOrWhiteSpace(dto.Brand))
                {
                    return;
                }

                _hasVehicleInDb = true;
                entryBrand.Text = dto.Brand ?? string.Empty;
                entryModel.Text = dto.Model ?? string.Empty;
                entryYear.Text = dto.Year > 0 ? dto.Year.ToString(CultureInfo.InvariantCulture) : string.Empty;
                btnDelete.IsVisible = true;
                btnSave.Text = "Actualizar vehículo";
            }
            catch
            {
            }
            finally
            {
                spinner.IsVisible = false;
                spinner.IsRunning = false;
            }
        }

        void PersistTripEstimatesFromForm()
        {
            if (double.TryParse((entryWhKm.Text ?? string.Empty).Trim().Replace(',', '.'),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out double wh)
                && wh is >= 30 and <= 600)
            {
                TripEstimatePreferences.SetWhPerKm(wh);
            }

            if (double.TryParse((entryCrcKwh.Text ?? string.Empty).Trim().Replace(',', '.'),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out double crc)
                && crc >= 0)
            {
                TripEstimatePreferences.SetCrcPerKwh(crc);
            }
        }

        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            if (Sesion.guid == Guid.Empty)
            {
                await DisplayAlertAsync("Vehículo", "Inicia sesión para guardar tu vehículo.", "Aceptar");
                return;
            }

            await AppConfiguration.EnsureLoadedAsync();
            if (!AppConfiguration.IsApiBaseUrlConfigured())
            {
                await DisplayAlertAsync("Vehículo", "Configura ApiBaseUrl en appsettings.json.", "Aceptar");
                return;
            }

            string brand = (entryBrand.Text ?? string.Empty).Trim();
            string model = (entryModel.Text ?? string.Empty).Trim();
            if (!int.TryParse((entryYear.Text ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int year)
                || year < 1980 || year > DateTime.UtcNow.Year + 1)
            {
                await DisplayAlertAsync("Vehículo", "Indica un año válido del vehículo.", "Aceptar");
                return;
            }

            if (string.IsNullOrEmpty(brand) || string.IsNullOrEmpty(model))
            {
                await DisplayAlertAsync("Vehículo", "Marca y modelo son obligatorios.", "Aceptar");
                return;
            }

            PersistTripEstimatesFromForm();

            spinner.IsVisible = true;
            spinner.IsRunning = true;
            btnSave.IsEnabled = false;
            try
            {
                using HttpClient client = ApiHttp.CreateClient();
                string json;
                HttpResponseMessage response;

                if (_hasVehicleInDb)
                {
                    var dto = new ReqUpdateVehicleDto { guid = Sesion.guid, Brand = brand, Model = model, Year = year };
                    json = JsonConvert.SerializeObject(dto);
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    response = await client.PutAsync(AppConfiguration.GetApiUrl("api/vehicle/update"), content);
                }
                else
                {
                    var dto = new ReqSaveVehicleDto { guid = Sesion.guid, Brand = brand, Model = model, Year = year };
                    json = JsonConvert.SerializeObject(dto);
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    response = await client.PostAsync(AppConfiguration.GetApiUrl("api/vehicle/save"), content);
                }

                string body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    await DisplayAlertAsync("Vehículo", $"Error {(int)response.StatusCode}.", "Aceptar");
                    return;
                }

                JObject? jo = null;
                try
                {
                    jo = JObject.Parse(body);
                }
                catch
                {
                }

                bool ok = jo == null
                    || jo["Result"]?.Value<bool>() == true
                    || jo["result"]?.Value<bool>() == true;
                if (!ok)
                {
                    await DisplayAlertAsync("Vehículo", "El servidor no pudo guardar el vehículo.", "Aceptar");
                    return;
                }

                _hasVehicleInDb = true;
                btnDelete.IsVisible = true;
                btnSave.Text = "Actualizar vehículo";
                await DisplayAlertAsync("Vehículo", "Vehículo guardado.", "Aceptar");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Vehículo", "Error: " + ex.Message, "Aceptar");
            }
            finally
            {
                spinner.IsVisible = false;
                spinner.IsRunning = false;
                btnSave.IsEnabled = true;
            }
        }

        private async void OnEliminarClicked(object sender, EventArgs e)
        {
            if (Sesion.guid == Guid.Empty || !_hasVehicleInDb)
            {
                return;
            }

            bool sure = await DisplayAlertAsync(
                "Eliminar vehículo",
                "¿Quitar el vehículo de tu cuenta? Los consumos en el servidor pueden dejar de tener referencia de auto.",
                "Sí, eliminar",
                "Cancelar");
            if (!sure)
            {
                return;
            }

            await AppConfiguration.EnsureLoadedAsync();
            if (!AppConfiguration.IsApiBaseUrlConfigured())
            {
                await DisplayAlertAsync("Vehículo", "Configura ApiBaseUrl.", "Aceptar");
                return;
            }

            spinner.IsVisible = true;
            spinner.IsRunning = true;
            try
            {
                using HttpClient client = ApiHttp.CreateClient();
                HttpResponseMessage response = await client.DeleteAsync(
                    AppConfiguration.GetApiUrl("api/vehicle/" + Sesion.guid));
                string body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    await DisplayAlertAsync("Vehículo", $"Error {(int)response.StatusCode}.", "Aceptar");
                    return;
                }

                JObject? jo = null;
                try
                {
                    jo = JObject.Parse(body);
                }
                catch
                {
                }

                bool ok = jo == null
                    || jo["Result"]?.Value<bool>() == true
                    || jo["result"]?.Value<bool>() == true;
                if (!ok)
                {
                    await DisplayAlertAsync("Vehículo", "No se pudo eliminar.", "Aceptar");
                    return;
                }

                _hasVehicleInDb = false;
                entryBrand.Text = string.Empty;
                entryModel.Text = string.Empty;
                entryYear.Text = string.Empty;
                btnDelete.IsVisible = false;
                btnSave.Text = "Guardar vehículo";
                await DisplayAlertAsync("Vehículo", "Vehículo eliminado.", "Aceptar");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Vehículo", "Error: " + ex.Message, "Aceptar");
            }
            finally
            {
                spinner.IsVisible = false;
                spinner.IsRunning = false;
            }
        }
    }
}
