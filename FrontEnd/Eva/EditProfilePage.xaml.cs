using Eva.DTO;
using Eva.Entidades;
using Eva.Response;
using Eva.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace Eva
{
    public partial class EditProfilePage : ContentPage
    {
        public EditProfilePage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ApplyFieldsFromSesion();
            _ = LoadProfileFromServerAsync();
        }

        void ApplyFieldsFromSesion()
        {
            lblEmailReadonly.Text = string.IsNullOrWhiteSpace(Sesion.Email) ? "—" : Sesion.Email;
            entryName.Text = Sesion.Name;
            entryLastName.Text = Sesion.LastName;
            entryAdress.Text = Sesion.Adress;
            entryChargerType.Text = Sesion.ChargerType;
        }

        async Task LoadProfileFromServerAsync()
        {
            if (Sesion.guid == Guid.Empty)
            {
                return;
            }

            await AppConfiguration.EnsureLoadedAsync();
            if (!AppConfiguration.IsApiBaseUrlConfigured())
            {
                return;
            }

            try
            {
                using HttpClient client = ApiHttp.CreateClient();
                HttpResponseMessage response = await client.GetAsync(
                    AppConfiguration.GetApiUrl("api/user/" + Sesion.guid));
                string body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    return;
                }

                ResGetUserApiDto? dto = JsonConvert.DeserializeObject<ResGetUserApiDto>(body);
                if (dto?.user == null)
                {
                    try
                    {
                        JObject jo = JObject.Parse(body);
                        JToken? ok = jo["result"] ?? jo["Result"];
                        if (ok?.Type == JTokenType.Boolean && !ok.Value<bool>())
                        {
                            return;
                        }

                        JToken? u = jo["user"] ?? jo["User"];
                        if (u == null)
                        {
                            return;
                        }

                        dto = new ResGetUserApiDto
                        {
                            Result = true,
                            user = u.ToObject<UserProfileApiDto>()
                        };
                    }
                    catch
                    {
                        return;
                    }
                }

                if (dto?.user == null)
                {
                    return;
                }

                UserProfileApiDto prof = dto.user;
                Sesion.Name = prof.Name ?? string.Empty;
                Sesion.LastName = prof.LastName ?? string.Empty;
                Sesion.Email = string.IsNullOrWhiteSpace(prof.Email) ? Sesion.Email : prof.Email;
                Sesion.Adress = prof.Adress ?? string.Empty;
                Sesion.ChargerType = prof.chargerType ?? string.Empty;
                SesionStore.PersistCurrent();

                await MainThread.InvokeOnMainThreadAsync(ApplyFieldsFromSesion);
            }
            catch
            {
            }
        }

        private async void OnVolverTapped(object sender, TappedEventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }

        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            if (Sesion.guid == Guid.Empty)
            {
                await DisplayAlertAsync("Perfil", "Inicia sesión para guardar cambios.", "Aceptar");
                return;
            }

            await AppConfiguration.EnsureLoadedAsync();
            if (!AppConfiguration.IsApiBaseUrlConfigured())
            {
                await DisplayAlertAsync("Perfil", "Configura ApiBaseUrl en appsettings.json.", "Aceptar");
                return;
            }

            string name = (entryName.Text ?? string.Empty).Trim();
            string last = (entryLastName.Text ?? string.Empty).Trim();
            string addr = (entryAdress.Text ?? string.Empty).Trim();
            string charger = (entryChargerType.Text ?? string.Empty).Trim();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(last) || string.IsNullOrEmpty(addr) || string.IsNullOrEmpty(charger))
            {
                await DisplayAlertAsync("Perfil", "Completa nombre, apellidos, dirección y tipo de cargador.", "Aceptar");
                return;
            }

            var dto = new DTOUpdateUser
            {
                guid = Sesion.guid,
                Name = name,
                LastName = last,
                Adress = addr,
                chargerType = charger
            };

            try
            {
                using HttpClient client = ApiHttp.CreateClient();
                string json = JsonConvert.SerializeObject(dto);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                using HttpResponseMessage response = await client.PutAsync(
                    AppConfiguration.GetApiUrl("api/user/update"),
                    content);
                string body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    await DisplayAlertAsync("Perfil", $"Error {(int)response.StatusCode}. Revisa la API.", "Aceptar");
                    return;
                }

                ResBase? res = JsonConvert.DeserializeObject<ResBase>(body);
                if (res != null && !res.Result)
                {
                    string msg = "No se pudo actualizar.";
                    if (res.errors is { Count: > 0 } && res.errors[0].message is { Length: > 0 } m)
                    {
                        msg = m;
                    }

                    await DisplayAlertAsync("Perfil", msg, "Aceptar");
                    return;
                }

                Sesion.Name = name;
                Sesion.LastName = last;
                Sesion.Adress = addr;
                Sesion.ChargerType = charger;
                SesionStore.PersistCurrent();
                await DisplayAlertAsync("Perfil", "Cambios guardados.", "Aceptar");
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Perfil", "Error de red: " + ex.Message, "Aceptar");
            }
        }
    }
}
