using Eva.Data;
using Eva.DTO;
using Eva.Entidades;
using Eva.Response;
using Eva.Services;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace Eva
{
    public partial class RegisterPage : ContentPage
    {
        public RegisterPage()
        {
            InitializeComponent();
            pickerPais.ItemsSource = CountryCityCatalog.GetCountries();
            pickerPais.SelectedIndexChanged += PickerPais_SelectedIndexChanged;
            pickerTipoCargador.ItemsSource = new List<string>
            {
                "Wallbox residencial",
                "Enchufe doméstico",
                "Cargador público AC",
                "Cargador público DC",
                "Otro / varios"
            };
        }

        private void PickerPais_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (pickerPais.SelectedItem == null)
            {
                pickerCiudad.ItemsSource = null;
                pickerCiudad.SelectedItem = null;
                pickerCiudad.IsEnabled = false;
                return;
            }

            string pais = pickerPais.SelectedItem.ToString() ?? string.Empty;
            pickerCiudad.ItemsSource = CountryCityCatalog.GetCities(pais);
            pickerCiudad.SelectedItem = null;
            pickerCiudad.IsEnabled = pickerCiudad.ItemsSource is List<string> list && list.Count > 0;
        }

        private async void OnVolverTapped(object sender, TappedEventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }

        private async void btnRegistrar_Clicked(object sender, EventArgs e)
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

            string nombre = txtNombre.Text != null ? txtNombre.Text.Trim() : string.Empty;
            string apellidos = txtApellidos.Text != null ? txtApellidos.Text.Trim() : string.Empty;
            string correo = txtEmail.Text != null ? txtEmail.Text.Trim() : string.Empty;
            string password = txtPassword.Text != null ? txtPassword.Text : string.Empty;
            string password2 = txtPasswordConfirm.Text != null ? txtPasswordConfirm.Text : string.Empty;

            if (string.IsNullOrWhiteSpace(nombre))
            {
                await DisplayAlert("Registro", "Ingresa tu nombre.", "Aceptar");
                return;
            }

            if (string.IsNullOrWhiteSpace(apellidos))
            {
                await DisplayAlert("Registro", "Ingresa tus apellidos.", "Aceptar");
                return;
            }

            if (string.IsNullOrWhiteSpace(correo) || !MainPage.EsCorreoValido(correo))
            {
                await DisplayAlert("Registro", "Correo electrónico inválido.", "Aceptar");
                return;
            }

            if (pickerPais.SelectedItem == null)
            {
                await DisplayAlert("Registro", "Selecciona tu país.", "Aceptar");
                return;
            }

            if (pickerCiudad.SelectedItem == null || !pickerCiudad.IsEnabled)
            {
                await DisplayAlert("Registro", "Selecciona la ciudad de tu país.", "Aceptar");
                return;
            }

            string paisSel = pickerPais.SelectedItem.ToString() ?? string.Empty;
            string ciudadSel = pickerCiudad.SelectedItem.ToString() ?? string.Empty;
            string direccion = CountryCityCatalog.BuildAddress(paisSel, ciudadSel);

            if (string.IsNullOrWhiteSpace(direccion))
            {
                await DisplayAlert("Registro", "No se pudo formar la ubicación. Vuelve a elegir país y ciudad.", "Aceptar");
                return;
            }

            if (direccion.Length > 50)
            {
                await DisplayAlert("Registro", "La ubicación supera 50 caracteres (límite en BD). Elige otra ciudad o reporta el caso.", "Aceptar");
                return;
            }

            if (pickerTipoCargador.SelectedItem == null)
            {
                await DisplayAlert("Registro", "Selecciona el tipo de cargador.", "Aceptar");
                return;
            }

            string tipoCargador = pickerTipoCargador.SelectedItem.ToString() ?? string.Empty;
            if (tipoCargador.Length > 50)
            {
                await DisplayAlert("Registro", "Tipo de cargador demasiado largo.", "Aceptar");
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                await DisplayAlert("Registro", "Ingresa una contraseña.", "Aceptar");
                return;
            }

            if (!EsContrasenaFuerte(password))
            {
                await DisplayAlert(
                    "Registro",
                    "La contraseña debe tener al menos 8 caracteres, una mayúscula, una minúscula, un número y un símbolo.",
                    "Aceptar");
                return;
            }

            if (password != password2)
            {
                await DisplayAlert("Registro", "Las contraseñas no coinciden.", "Aceptar");
                return;
            }

            btnRegistrar.IsEnabled = false;
            btnRegistrar.Text = "";
            spinnerRegistro.IsVisible = true;
            spinnerRegistro.IsRunning = true;

            DTOUserRegister dto = new DTOUserRegister();
            dto.name = nombre;
            dto.LastName = apellidos;
            dto.email = correo;
            dto.password = password;
            dto.Adress = direccion;
            dto.chargerType = tipoCargador;

            try
            {
                using HttpClient httpClient = ApiHttp.CreateClient();
                StringContent jsonContent = new StringContent(
                    JsonConvert.SerializeObject(dto),
                    Encoding.UTF8,
                    "application/json");

                HttpResponseMessage response = await httpClient.PostAsync(AppConfiguration.GetApiUrl("api/user/register"), jsonContent);
                string responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    ResBase? res = JsonConvert.DeserializeObject<ResBase>(responseContent);
                    if (res != null && res.Result)
                    {
                        string rutaActivacion = $"{nameof(ActivateAccountPage)}?Email={Uri.EscapeDataString(correo)}";
                        await Shell.Current.GoToAsync(rutaActivacion);
                    }
                    else
                    {
                        string msg = MensajeErrores(res);
                        await DisplayAlert("Registro", msg, "Aceptar");
                    }
                }
                else
                {
                    string snippet = string.IsNullOrWhiteSpace(responseContent)
                        ? ""
                        : (responseContent.Length > 220 ? responseContent[..220] + "…" : responseContent);
                    await DisplayAlert(
                        "Registro",
                        "El servidor respondió " + (int)response.StatusCode + " " + response.ReasonPhrase + "\n\n" + snippet,
                        "Aceptar");
                }
            }
            catch (HttpRequestException ex)
            {
                await DisplayAlert("Registro", "Error de conexión: " + ex.Message, "Aceptar");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Registro", "Error: " + ex.Message, "Aceptar");
            }
            finally
            {
                spinnerRegistro.IsVisible = false;
                spinnerRegistro.IsRunning = false;
                btnRegistrar.IsEnabled = true;
                btnRegistrar.Text = "Registrarse";
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
                return "No se pudo completar el registro.";
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
                return "No se pudo completar el registro.";
            }

            return string.Join("\n", lineas);
        }

        private static bool EsContrasenaFuerte(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            return Regex.IsMatch(
                password,
                @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,}$");
        }
    }
}
