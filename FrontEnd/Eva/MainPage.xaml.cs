using Eva.DTO;
using Eva.Entidades;
using Eva.Response;
using Eva.Services;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace Eva
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void btnIngresar_Clicked(object sender, EventArgs e)
        {
            await EnviarBackEnd();
        }

        private async Task EnviarBackEnd()
        {
            await AppConfiguration.EnsureLoadedAsync();

            btnIngresar.Text = "";
            btnIngresar.IsEnabled = false;
            spinner.IsVisible = true;
            spinner.IsRunning = true;

            try
            {
                if (string.IsNullOrEmpty(txtEmail.Text) || string.IsNullOrEmpty(txtPassword.Text))
                {
                    await DisplayAlertAsync("Login", "Ingrese correo y contraseña", "Aceptar");
                }
                else if (EsCorreoValido(txtEmail.Text))
                {
                    if (!AppConfiguration.IsApiBaseUrlConfigured())
                    {
                        await DisplayAlertAsync(
                            "Configuración",
                            "En Resources/Raw/appsettings.json pon ApiBaseUrl con la URL del backend EVABD (termina en /).",
                            "Aceptar");
                        return;
                    }

                    string baseUrl = AppConfiguration.ApiBaseUrl;

                    var dtoLogin = new DTOLogin
                    {
                        email = txtEmail.Text,
                        password = txtPassword.Text
                    };

                    var jsonContent = new StringContent(JsonConvert.SerializeObject(dtoLogin), Encoding.UTF8, "application/json");
                    using HttpClient httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(35) };
                    if (AppConfiguration.IsNgrokUrl())
                    {
                        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("ngrok-skip-browser-warning", "true");
                    }

                    try
                    {
                        var response = await httpClient.PostAsync(AppConfiguration.GetApiUrl("api/user/login"), jsonContent);

                        if (response.IsSuccessStatusCode)
                        {
                            var responseContent = await response.Content.ReadAsStringAsync();
                            var res = JsonConvert.DeserializeObject<ResLogin>(responseContent);

                            if (res != null && res.Result && res.user != null)
                            {
                                Sesion.guid = res.user.guid;
                                Sesion.Name = res.user.Name;
                                Sesion.LastName = res.user.LastName;
                                Sesion.Email = string.IsNullOrWhiteSpace(res.user.Email) ? txtEmail.Text.Trim() : res.user.Email;
                                Sesion.Adress = res.user.Adress ?? string.Empty;
                                Sesion.ChargerType = res.user.chargerType ?? string.Empty;
                                Sesion.open = DateTime.Now;
                                SesionStore.PersistCurrent();
                                await Shell.Current.GoToAsync(nameof(MapPage));
                            }
                            else
                            {
                                var msg = res?.errors != null && res.errors.Count > 0
                                    ? string.Join("\n", res.errors.Select(e => e.message))
                                    : "Credenciales incorrectas";
                                await DisplayAlertAsync("Login", msg, "Aceptar");
                            }
                        }
                        else
                        {
                            var body = await response.Content.ReadAsStringAsync();
                            var status = (int)response.StatusCode;

                            if (status == 502 || status == 504)
                            {
                                string hint502 = AppConfiguration.IsNgrokUrl()
                                    ? "Si usas ngrok: verifica que el túnel apunte al puerto correcto de tu API local y actualiza ApiBaseUrl en appsettings.json."
                                    : "Revisa que el API en la nube esté en ejecución y que ApiBaseUrl en appsettings.json sea correcta.";
                                await DisplayAlertAsync("Login",
                                    "Error de puerta de enlace (502/504).\n\n" + hint502,
                                    "Aceptar");
                            }
                            else if (body.Contains("ERR_NGROK", StringComparison.OrdinalIgnoreCase)
                                || body.Contains("is offline", StringComparison.OrdinalIgnoreCase))
                            {
                                await DisplayAlertAsync("Login",
                                    "El túnel ngrok no está activo o la URL está desactualizada.\n\n"
                                    + "Actualiza ApiBaseUrl en Resources/Raw/appsettings.json con la URL https actual de ngrok.",
                                    "Aceptar");
                            }
                            else
                            {
                                var snippet = string.IsNullOrWhiteSpace(body)
                                    ? ""
                                    : (body.Length > 200 ? body[..200] + "…" : body);
                                await DisplayAlertAsync("Login", $"BackEnd respondió {status} {response.ReasonPhrase}\n\n{snippet}", "Aceptar");
                            }
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        await DisplayAlertAsync(
                            "Login",
                            "Tiempo de espera agotado (35 s). Revisa la URL del API, la red del emulador o que el servidor responda.",
                            "Aceptar");
                    }
                    catch (HttpRequestException ex)
                    {
                        string detalle = ex.Message;
                        if (ex.InnerException != null)
                        {
                            detalle += "\n" + ex.InnerException.Message;
                        }

                        await DisplayAlertAsync(
                            "Sin conexión al API",
                            "No se pudo contactar el servidor. Comprueba internet en el dispositivo o emulador.\n\n"
                            + "Si acabas de cambiar appsettings.json, vuelve a compilar e instalar la app (el JSON va empaquetado).\n\n"
                            + "URL configurada:\n" + baseUrl + "\n\n"
                            + detalle,
                            "Aceptar");
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlertAsync("Error", "Error inesperado: " + ex.Message, "Aceptar");
                    }
                }
                else
                {
                    await DisplayAlertAsync("Login", "Correo electrónico inválido", "Aceptar");
                }
            }
            finally
            {
                spinner.IsVisible = false;
                spinner.IsRunning = false;
                btnIngresar.IsEnabled = true;
                btnIngresar.Text = "Ingresar";
            }
        }

        public static bool EsCorreoValido(string correo)
        {
            if (string.IsNullOrWhiteSpace(correo)) return false;
            string patron = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(correo, patron, RegexOptions.IgnoreCase);
        }

        async void OnRegistrarseTapped(object sender, TappedEventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(RegisterPage));
        }

        async void OnActivarCuentaTapped(object sender, TappedEventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(ActivateAccountPage));
        }

        async void OnTerminosTapped(object sender, TappedEventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(TermsPage));
        }
    }
}
