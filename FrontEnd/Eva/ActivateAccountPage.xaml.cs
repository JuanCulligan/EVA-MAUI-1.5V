using Eva.DTO;
using Eva.Entidades;
using Eva.Response;
using Eva.Services;
using Newtonsoft.Json;
using System.Text;

namespace Eva
{
    [QueryProperty(nameof(EmailParam), "Email")]
    public partial class ActivateAccountPage : ContentPage
    {
        private string _emailDesdeRegistro = string.Empty;

        public string EmailParam
        {
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _emailDesdeRegistro = string.Empty;
                }
                else
                {
                    _emailDesdeRegistro = Uri.UnescapeDataString(value);
                }

                if (txtEmailActivacion != null && !string.IsNullOrEmpty(_emailDesdeRegistro))
                {
                    txtEmailActivacion.Text = _emailDesdeRegistro;
                }
            }
        }

        public ActivateAccountPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (!string.IsNullOrEmpty(_emailDesdeRegistro)
                && string.IsNullOrWhiteSpace(txtEmailActivacion.Text))
            {
                txtEmailActivacion.Text = _emailDesdeRegistro;
            }
        }

        private async void OnVolverTapped(object sender, TappedEventArgs e)
        {
            await Shell.Current.GoToAsync("//MainPage");
        }

        private async void btnActivar_Clicked(object sender, EventArgs e)
        {
            await AppConfiguration.EnsureLoadedAsync();

            if (!AppConfiguration.IsApiBaseUrlConfigured())
            {
                await DisplayAlert(
                    "Configuración",
                    "En Resources/Raw/appsettings.json pon ApiBaseUrl con la URL del backend EVABD.",
                    "Aceptar");
                return;
            }

            string correo = txtEmailActivacion.Text != null ? txtEmailActivacion.Text.Trim() : string.Empty;
            string codigo = txtCodigo.Text != null ? txtCodigo.Text.Trim() : string.Empty;

            if (string.IsNullOrWhiteSpace(correo) || !MainPage.EsCorreoValido(correo))
            {
                await DisplayAlert("Activación", "Ingresa un correo electrónico válido.", "Aceptar");
                return;
            }

            if (string.IsNullOrWhiteSpace(codigo))
            {
                await DisplayAlert("Activación", "Ingresa el código que recibiste por correo.", "Aceptar");
                return;
            }

            btnActivar.IsEnabled = false;
            btnActivar.Text = "";
            spinnerActivar.IsVisible = true;
            spinnerActivar.IsRunning = true;

            DTOActivateUser dto = new DTOActivateUser();
            dto.email = correo;
            dto.token = codigo.ToUpperInvariant();

            try
            {
                using HttpClient httpClient = ApiHttp.CreateClient();
                StringContent jsonContent = new StringContent(
                    JsonConvert.SerializeObject(dto),
                    Encoding.UTF8,
                    "application/json");

                HttpResponseMessage response = await httpClient.PostAsync(AppConfiguration.GetApiUrl("api/user/activate"), jsonContent);
                string responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    ResBase? res = JsonConvert.DeserializeObject<ResBase>(responseContent);
                    if (res != null && res.Result)
                    {
                        await DisplayAlert(
                            "Activación",
                            "Cuenta activada. Ya puedes iniciar sesión.",
                            "Aceptar");
                        await Shell.Current.GoToAsync("//MainPage");
                    }
                    else
                    {
                        string msg = MensajeErrores(res);
                        await DisplayAlert("Activación", msg, "Aceptar");
                    }
                }
                else
                {
                    string snippet = string.IsNullOrWhiteSpace(responseContent)
                        ? ""
                        : (responseContent.Length > 220 ? responseContent[..220] + "…" : responseContent);
                    await DisplayAlert(
                        "Activación",
                        "El servidor respondió " + (int)response.StatusCode + " " + response.ReasonPhrase + "\n\n" + snippet,
                        "Aceptar");
                }
            }
            catch (HttpRequestException ex)
            {
                await DisplayAlert("Activación", "Error de conexión: " + ex.Message, "Aceptar");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Activación", "Error: " + ex.Message, "Aceptar");
            }
            finally
            {
                spinnerActivar.IsVisible = false;
                spinnerActivar.IsRunning = false;
                btnActivar.IsEnabled = true;
                btnActivar.Text = "Activar cuenta";
            }
        }

        private static string MensajeErrores(ResBase? res)
        {
            if (res == null)
            {
                return "No se pudo leer la respuesta del servidor.";
            }

            if (res.errors == null || res.errors.Count == 0)
            {
                return "Código incorrecto o cuenta ya activada. Verifica el correo y el código.";
            }

            List<string> lineas = new List<string>();
            foreach (Error err in res.errors)
            {
                if (err != null && !string.IsNullOrWhiteSpace(err.message))
                {
                    lineas.Add(err.message);
                }
            }

            if (lineas.Count == 0)
            {
                return "No se pudo activar la cuenta.";
            }

            return string.Join("\n", lineas);
        }
    }
}
