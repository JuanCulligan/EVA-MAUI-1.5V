using Eva.Entidades;
using Eva.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.Communication;

namespace Eva
{
    public partial class ReportBugPage : ContentPage
    {
        public ReportBugPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await AppConfiguration.EnsureLoadedAsync();
            if (AppConfiguration.IsSupportEmailConfigured())
            {
                SupportDestinationLabel.Text = "Destino: " + AppConfiguration.SupportEmail;
            }
            else
            {
                SupportDestinationLabel.Text =
                    "Configura \"SupportEmail\" en Resources/Raw/appsettings.json (no subas correos reales a repos públicos).";
            }
        }

        private async void OnVolverTapped(object sender, TappedEventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }

        async void OnEnviarClicked(object sender, EventArgs e)
        {
            await AppConfiguration.EnsureLoadedAsync();
            if (!AppConfiguration.IsSupportEmailConfigured())
            {
                await DisplayAlert(
                    "Reporte",
                    "Configura \"SupportEmail\" en Resources/Raw/appsettings.json con tu correo de soporte (solo en local).",
                    "Aceptar");
                return;
            }

            string supportTo = AppConfiguration.SupportEmail;

            string asunto = entryAsunto.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(asunto))
            {
                await DisplayAlert("Reporte", "Escribe un asunto breve.", "Aceptar");
                return;
            }

            string detalle = editorDetalle.Text?.Trim() ?? string.Empty;
            if (detalle.Length < 8)
            {
                await DisplayAlert("Reporte", "Describe el problema con un poco más de detalle.", "Aceptar");
                return;
            }

            string cuerpo = $"App: EVA v{AppInfo.Current.VersionString} (build {AppInfo.Current.BuildString})\n" +
                            $"Dispositivo: {DeviceInfo.Current.Model} · {DeviceInfo.Current.Platform} {DeviceInfo.Current.VersionString}\n" +
                            $"Usuario: {(string.IsNullOrWhiteSpace(Sesion.Email) ? "no sesión" : Sesion.Email)}\n\n" +
                            "---\n\n" +
                            detalle;

            string subject = "[EVA] " + asunto;

            try
            {
                if (Email.Default.IsComposeSupported)
                {
                    await Email.Default.ComposeAsync(new EmailMessage
                    {
                        To = new List<string> { supportTo },
                        Subject = subject,
                        Body = cuerpo
                    });
                }
                else
                {
                    string q = $"mailto:{supportTo}?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(cuerpo)}";
                    await Launcher.Default.OpenAsync(new Uri(q));
                }
            }
            catch (FeatureNotSupportedException)
            {
                string q = $"mailto:{supportTo}?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(cuerpo)}";
                await Launcher.Default.OpenAsync(new Uri(q));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Reporte", "No se pudo abrir el correo: " + ex.Message, "Aceptar");
            }
        }
    }
}
