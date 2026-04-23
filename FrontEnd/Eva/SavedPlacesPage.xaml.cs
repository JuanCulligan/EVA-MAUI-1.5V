using Eva.Entidades;
using Eva.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;

namespace Eva
{
    [QueryProperty(nameof(PickQuery), "Pick")]
    public partial class SavedPlacesPage : ContentPage
    {
        bool _pickMode;
        double? _draftLat;
        double? _draftLng;

        public string PickQuery
        {
            set
            {
                _pickMode = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1";
            }
        }

        public SavedPlacesPage()
        {
            InitializeComponent();
            pickerKind.ItemsSource = new[] { "Casa", "Trabajo", "Otro" };
            pickerKind.SelectedIndex = 0;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            lblTitle.Text = _pickMode ? "Elegir destino" : "Mis lugares";
            lblPickHint.IsVisible = true;
            lblPickHint.Text = _pickMode
                ? "Toca un lugar para iniciar el viaje. Los favoritos se guardan en tu cuenta (servidor)."
                : "Los lugares se sincronizan con el servidor. La nota de dirección se guarda solo en este dispositivo.";
            panelAdd.IsVisible = !_pickMode;
            _ = RefreshListAsync();
        }

        private async void OnVolverTapped(object sender, TappedEventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }

        async Task RefreshListAsync()
        {
            listPlaces.ItemsSource = null;
            spinner.IsVisible = true;
            spinner.IsRunning = true;
            try
            {
                await AppConfiguration.EnsureLoadedAsync();
                if (Sesion.guid == Guid.Empty || !AppConfiguration.IsApiBaseUrlConfigured())
                {
                    listPlaces.ItemsSource = new List<SavedPlace>();
                    return;
                }

                List<SavedPlace> fromApi = await FavoritesApi.GetFavoritesAsPlacesAsync(Sesion.guid);
                listPlaces.ItemsSource = fromApi;
            }
            finally
            {
                spinner.IsVisible = false;
                spinner.IsRunning = false;
            }
        }

        async void OnUsarUbicacionClicked(object sender, EventArgs e)
        {
#if ANDROID || IOS || MACCATALYST
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }

                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlertAsync("Ubicación", "Activa el permiso de ubicación para guardar coordenadas.", "Aceptar");
                    return;
                }
            }
            catch
            {
            }
#endif
            try
            {
                Location? loc = await Geolocation.Default.GetLocationAsync(
                    new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(20)));
                loc ??= await Geolocation.Default.GetLastKnownLocationAsync();
                if (loc == null)
                {
                    await DisplayAlertAsync("Ubicación", "No se pudo obtener la posición. Intenta de nuevo en un sitio con mejor señal.", "Aceptar");
                    return;
                }

                _draftLat = loc.Latitude;
                _draftLng = loc.Longitude;
                lblCoords.Text = $"Coordenadas: {_draftLat:F5}, {_draftLng:F5}";
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Ubicación", ex.Message, "Aceptar");
            }
        }

        async void OnGuardarClicked(object sender, EventArgs e)
        {
            if (Sesion.guid == Guid.Empty)
            {
                await DisplayAlertAsync("Lugares", "Inicia sesión para guardar favoritos en el servidor.", "Aceptar");
                return;
            }

            await AppConfiguration.EnsureLoadedAsync();
            if (!AppConfiguration.IsApiBaseUrlConfigured())
            {
                await DisplayAlertAsync("Lugares", "Configura ApiBaseUrl en appsettings.json.", "Aceptar");
                return;
            }

            string kind = pickerKind.SelectedItem?.ToString() ?? "Otro";
            string nombre = entryNombre.Text?.Trim() ?? string.Empty;
            string locationName = string.IsNullOrWhiteSpace(nombre) ? kind : $"{kind} · {nombre}";

            if (_draftLat == null || _draftLng == null)
            {
                await DisplayAlertAsync("Lugares", "Pulsa «Usar mi ubicación» para fijar las coordenadas del lugar.", "Aceptar");
                return;
            }

            spinner.IsVisible = true;
            spinner.IsRunning = true;
            try
            {
                bool ok = await FavoritesApi.SaveFavoriteAsync(Sesion.guid, locationName, _draftLat.Value, _draftLng.Value);
                if (!ok)
                {
                    await DisplayAlertAsync("Lugares", "No se pudo guardar en el servidor. Revisa la conexión o que exista el procedimiento de favoritos.", "Aceptar");
                    return;
                }

                string stableId = FavoritesApi.StableId(locationName, _draftLat.Value, _draftLng.Value);
                FavoriteNotesStore.Set(stableId, entryNota.Text);

                entryNombre.Text = string.Empty;
                entryNota.Text = string.Empty;
                _draftLat = null;
                _draftLng = null;
                lblCoords.Text = "Coordenadas: —";
                await RefreshListAsync();
                await DisplayAlertAsync("Lugares", "Lugar guardado en tu cuenta.", "Aceptar");
            }
            finally
            {
                spinner.IsVisible = false;
                spinner.IsRunning = false;
            }
        }

        async void OnEliminarClicked(object sender, EventArgs e)
        {
            if (sender is not Button btn || btn.BindingContext is not SavedPlace p)
            {
                return;
            }

            bool ok = await DisplayAlertAsync("Eliminar", $"¿Quitar «{p.Name}» del servidor?", "Sí", "No");
            if (!ok)
            {
                return;
            }

            if (Sesion.guid == Guid.Empty)
            {
                return;
            }

            spinner.IsVisible = true;
            spinner.IsRunning = true;
            try
            {
                bool removed = await FavoritesApi.DeleteFavoriteAsync(Sesion.guid, p.Name, p.Latitude, p.Longitude);
                FavoriteNotesStore.Set(p.Id, null);
                if (!removed)
                {
                    await DisplayAlertAsync("Lugares", "No se pudo eliminar en el servidor. Si acabas de desplegar la API, ejecuta el script SP_EliminarViajeGuardado.sql en la base de datos.", "Aceptar");
                }

                await RefreshListAsync();
            }
            finally
            {
                spinner.IsVisible = false;
                spinner.IsRunning = false;
            }
        }

        async void OnItemTapped(object sender, TappedEventArgs e)
        {
            if (sender is not Border b || b.BindingContext is not SavedPlace p)
            {
                return;
            }

            if (!_pickMode)
            {
                bool ok = await DisplayAlertAsync("Ruta", $"¿Iniciar viaje hacia «{p.Name}»?", "Sí", "No");
                if (!ok)
                {
                    return;
                }
            }

            TripPickChannel.Send(new TripPickPayload
            {
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                DisplayName = p.Name
            });
            await Shell.Current.GoToAsync("..");
        }
    }
}
