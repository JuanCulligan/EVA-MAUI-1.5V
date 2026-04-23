# Eva — frontend (.NET MAUI)

## Legal notice

© 2026 JuanCulligan. All rights reserved.

This project is not licensed for public use, distribution, or modification. Commercial use is not permitted. You may not edit or redistribute this application without explicit authorization from the copyright holder.

## Requisitos

- [.NET SDK](https://dotnet.microsoft.com/download) compatible con el proyecto (p. ej. .NET 10 según `Eva.csproj`).
- Para **Android**: Android SDK / emulador o dispositivo; en macOS/Windows según el [entorno MAUI](https://learn.microsoft.com/dotnet/maui/get-started/installation).
- Opcional: **iOS / Mac Catalyst** solo en macOS con Xcode.

## Configuración (local; no subas secretos al repositorio)

### 1. URL del API (`appsettings.json`)

En el repositorio, `appsettings.json` viene con `ApiBaseUrl` vacío para no exponer tu despliegue. Edita `FrontEnd/Eva/Resources/Raw/appsettings.json` (puedes partir de `appsettings.example.json` en la misma carpeta):

```json
{
  "ApiBaseUrl": "https://localhost:44352/"
}
```

- Debe ser una URL absoluta y terminar en `/` (la app la normaliza si falta).
- Con el backend en Visual Studio / IIS Express, el puerto suele coincidir con el de `BackEnd/API/API.csproj` (`IISUrl`, por defecto `https://localhost:44352/`).
- Si usas **ngrok** u otro túnel HTTPS, pon aquí la URL pública y revisa que el dispositivo/emulador llegue a ese host.

Valores reservados que la app trata como “no configurado” (no usar en producción tal cual): `TU-API-EVABD`, `TU-API-AZURE`.

En el mismo archivo, **`SupportEmail`** es el destino del flujo “Reportar problema” (correo). Déjalo vacío en el repo público y pon tu correo solo en `appsettings.json` local (no lo subas).

### 2. Mapbox — token público en la app (`pk.…`)

Crea un token público en [Mapbox](https://account.mapbox.com/access-tokens/) y asígnalo en:

- `FrontEnd/Eva/MapPage.xaml.cs` → constante `MapboxAccessToken`
- `FrontEnd/Eva/Platforms/Android/Resources/values/mapbox_access_token.xml` → `mapbox_access_token` (mismo valor; lo usa el SDK nativo en Android)

### 3. Mapbox — token de descargas Gradle (`sk.…`) solo para compilar Android

El proyecto genera el AAR de navegación con Gradle y Mapbox. Necesitas un **secret token** con scope **DOWNLOADS:READ** (no lo commits).

En la **misma terminal** donde compilas:

```bash
export MAPBOX_DOWNLOADS_TOKEN='sk.…'
```

Luego desde `FrontEnd/Eva/`:

```bash
dotnet build -f net10.0-android
```

(o `dotnet publish` con el mismo framework). Sin esta variable, el target de Android fallará con el mensaje indicado en `Eva.csproj`.

## Ejecutar

Desde `FrontEnd/Eva/`:

```bash
dotnet build
dotnet run -f net10.0-android
```

Ajusta `-f` a `net10.0-ios`, `net10.0-maccatalyst` o `net10.0-windows10.0.19041.0` si aplica.

## Backend

El cliente espera el API documentado en `BackEnd/README.md`. Arranca el backend antes de probar login, mapa con consumos guardados en servidor, etc.
