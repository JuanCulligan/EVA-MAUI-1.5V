using Eva.Entidades;

namespace Eva.Services
{
    public static class SesionStore
    {
        const string KeyGuid = "eva_sesion_guid";
        const string KeyName = "eva_sesion_name";
        const string KeyLastName = "eva_sesion_lastname";
        const string KeyEmail = "eva_sesion_email";
        const string KeyAdress = "eva_sesion_adress";
        const string KeyChargerType = "eva_sesion_charger_type";
        const string KeyOpen = "eva_sesion_open";
        const string KeyLastActivityUtc = "eva_sesion_last_activity_utc";

        static readonly TimeSpan InactivityLimit = TimeSpan.FromDays(14);

        public static bool RestoreIfUnderInactivityLimit()
        {
            string? guidStr = Preferences.Get(KeyGuid, null);
            if (string.IsNullOrEmpty(guidStr) || !Guid.TryParse(guidStr, out Guid g) || g == Guid.Empty)
            {
                return false;
            }

            string? lastStr = Preferences.Get(KeyLastActivityUtc, null);
            if (string.IsNullOrEmpty(lastStr)
                || !DateTime.TryParse(lastStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime lastUtc))
            {
                ClearStorageAndMemory();
                return false;
            }

            if (DateTime.UtcNow - lastUtc > InactivityLimit)
            {
                ClearStorageAndMemory();
                return false;
            }

            Sesion.guid = g;
            Sesion.Name = Preferences.Get(KeyName, string.Empty) ?? string.Empty;
            Sesion.LastName = Preferences.Get(KeyLastName, string.Empty) ?? string.Empty;
            Sesion.Email = Preferences.Get(KeyEmail, string.Empty) ?? string.Empty;
            Sesion.Adress = Preferences.Get(KeyAdress, string.Empty) ?? string.Empty;
            Sesion.ChargerType = Preferences.Get(KeyChargerType, string.Empty) ?? string.Empty;
            string? openStr = Preferences.Get(KeyOpen, null);
            if (!string.IsNullOrEmpty(openStr)
                && DateTime.TryParse(openStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime open))
            {
                Sesion.open = open;
            }
            else
            {
                Sesion.open = DateTime.Now;
            }

            return true;
        }

        public static void PersistCurrent()
        {
            if (Sesion.guid == Guid.Empty)
            {
                ClearStorageAndMemory();
                return;
            }

            Preferences.Set(KeyGuid, Sesion.guid.ToString());
            Preferences.Set(KeyName, Sesion.Name ?? string.Empty);
            Preferences.Set(KeyLastName, Sesion.LastName ?? string.Empty);
            Preferences.Set(KeyEmail, Sesion.Email ?? string.Empty);
            Preferences.Set(KeyAdress, Sesion.Adress ?? string.Empty);
            Preferences.Set(KeyChargerType, Sesion.ChargerType ?? string.Empty);
            Preferences.Set(KeyOpen, Sesion.open.ToString("o"));
            TouchLastActivityIfLoggedIn();
        }

        public static void TouchLastActivityIfLoggedIn()
        {
            if (Sesion.guid == Guid.Empty)
            {
                return;
            }

            Preferences.Set(KeyLastActivityUtc, DateTime.UtcNow.ToString("o"));
        }

        public static void ClearStorageAndMemory()
        {
            Preferences.Remove(KeyGuid);
            Preferences.Remove(KeyName);
            Preferences.Remove(KeyLastName);
            Preferences.Remove(KeyEmail);
            Preferences.Remove(KeyAdress);
            Preferences.Remove(KeyChargerType);
            Preferences.Remove(KeyOpen);
            Preferences.Remove(KeyLastActivityUtc);

            Sesion.guid = Guid.Empty;
            Sesion.Name = string.Empty;
            Sesion.LastName = string.Empty;
            Sesion.Email = string.Empty;
            Sesion.Adress = string.Empty;
            Sesion.ChargerType = string.Empty;
            Sesion.open = default;
        }

        public static async Task TryAutoNavigateToMapAsync()
        {
            if (!RestoreIfUnderInactivityLimit() || Sesion.guid == Guid.Empty)
            {
                return;
            }

            await Task.Delay(120);
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    await Shell.Current.GoToAsync(nameof(MapPage));
                }
                catch
                {
                    // Shell aún no listo en algunos arranques; el usuario sigue en login.
                }
            });
        }
    }
}
