using Eva.Entidades;
using Eva.Services;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace Eva
{
    public partial class SettingsPage : ContentPage
    {
        public SettingsPage()
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
            lblNombreCompleto.Text = $"{Sesion.Name} {Sesion.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(lblNombreCompleto.Text))
            {
                lblNombreCompleto.Text = "Usuario";
            }

            lblCorreo.Text = string.IsNullOrWhiteSpace(Sesion.Email)
                ? "Sin correo"
                : Sesion.Email;
            lblSesionId.Text = Sesion.guid == Guid.Empty
                ? "Sesión no iniciada"
                : $"ID: {Sesion.guid}";
            lblVersion.Text = $"EVA · v{AppInfo.Current.VersionString} (build {AppInfo.Current.BuildString})";
        }

        async void OnLugaresClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(SavedPlacesPage));
        }

        async void OnEditarPerfilClicked(object sender, EventArgs e)
        {
            if (Sesion.guid == Guid.Empty)
            {
                await DisplayAlert("Cuenta", "Inicia sesión para editar tu perfil.", "Aceptar");
                return;
            }

            await Shell.Current.GoToAsync(nameof(EditProfilePage));
        }

        async void OnVehiculoClicked(object sender, EventArgs e)
        {
            if (Sesion.guid == Guid.Empty)
            {
                await DisplayAlert("Vehículo", "Inicia sesión para gestionar tu vehículo.", "Aceptar");
                return;
            }

            await Shell.Current.GoToAsync(nameof(MyVehiclePage));
        }

        async void OnTerminosClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(TermsPage));
        }

        async void OnReporteClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(ReportBugPage));
        }

        async void OnCerrarSesionClicked(object sender, EventArgs e)
        {
            bool ok = await DisplayAlert(
                "Cerrar sesión",
                "¿Seguro que quieres salir? Tus favoritos siguen en el servidor; las notas locales de lugares se quedan en el dispositivo.",
                "Sí, salir",
                "Cancelar");
            if (!ok)
            {
                return;
            }

            Guid g = Sesion.guid;
            await AppConfiguration.EnsureLoadedAsync();
            if (g != Guid.Empty && AppConfiguration.IsApiBaseUrlConfigured())
            {
                try
                {
                    using HttpClient client = ApiHttp.CreateClient();
                    string json = JsonConvert.SerializeObject(new { guid = g });
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    _ = await client.PostAsync(AppConfiguration.GetApiUrl("api/user/logout"), content);
                }
                catch
                {
                }
            }

            SesionStore.ClearStorageAndMemory();
            await Shell.Current.GoToAsync("//MainPage");
        }
    }
}
