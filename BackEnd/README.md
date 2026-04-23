# Eva — backend (API .NET Framework)

## Legal notice

© 2026 JuanCulligan. All rights reserved.

This project is not licensed for public use, distribution, or modification. Commercial use is not permitted. You may not edit or redistribute this application without explicit authorization from the copyright holder.

## About

Solución .NET con proyecto **ASP.NET Web API** (`API`), lógica (`Logic`, `Core`), acceso a datos (`DataAccess`), **DTO** y **Helpers**. El cliente MAUI está en `FrontEnd/` del mismo repositorio.

## Requisitos

- **Windows** con [Visual Studio 2022](https://visualstudio.microsoft.com/) (carga de trabajo **ASP.NET y desarrollo web**).
- **.NET Framework 4.8** y **IIS Express** (instalados con Visual Studio).
- **SQL Server** local (o remoto) compatible con la cadena que configures.

## Abrir y restaurar paquetes

1. Abre `BackEnd/FastPlanner.slnx` (o el `.sln` si generas uno) en Visual Studio.
2. **Restaurar** paquetes NuGet (clic derecho en la solución → *Restore NuGet Packages*).
3. Establece `API` como proyecto de inicio.

## Base de datos

En el repositorio público **no** se incluyen cadenas reales de Azure ni contraseñas.

1. Crea la base (nombre coherente con tu entorno, p. ej. `DB_EVA_`) y ejecuta scripts que apliquen a tu esquema (hay SQL de ejemplo en `BackEnd/Database/`).
2. Configura la cadena **`DataAccess.Properties.Settings.DB_EVA_ConnectionString`** en:
   - `BackEnd/API/Web.config` → sección `<connectionStrings>` (desarrollo local típico: `Server=.;Initial Catalog=…;Integrated Security=True;`), **o**
   - variables de entorno / **Connection strings** en Azure App Service en despliegue (recomendado en la nube).

`BackEnd/DataAccess/app.config` en el repo usa la misma idea **solo para desarrollo**; al publicar, prioriza configuración en el servidor sin subir secretos.

## Variables de entorno (API keys y correo)

El código **no** lleva claves fijas en el repositorio: se leen con `Environment.GetEnvironmentVariable`. Configúralas en Windows (Variables de entorno del usuario/sistema) o en la configuración de aplicación de Azure **antes** de ejecutar funciones que las usen.

| Variable | Uso |
|----------|-----|
| `MapBox_API_Key` | Token Mapbox en el **servidor** (rutas, elevación, etc. en `MapboxService`). Puede ser el mismo estilo de token que uses en cliente según políticas de Mapbox. |
| `OPENWEATHER_API_KEY` | OpenWeatherMap (`OpenWeatherService`). |
| `opencharge_api_key` | OpenChargeMap (`OpenChargeService`). |
| `Gemini_API_Key` | Google Gemini (`GeminiApiService`). |
| `email_sender` | Cuenta remitente para correo de verificación (`Helpers.SendVerificationEmail`). |
| `email_password` | Contraseña o app password SMTP asociada al remitente. |

**IIS Express / Visual Studio:** reinicia el depurador tras cambiar variables de sistema para que el proceso las cargue.

Si una variable falta, la funcionalidad asociada fallará en tiempo de ejecución (revisa logs o excepciones).

## Ejecutar en local

1. Asegura la cadena SQL en `Web.config`.
2. Pulsa **F5** (IIS Express). La URL por defecto del proyecto API está en `API.csproj` (`IISUrl`), normalmente **`https://localhost:44352/`**.
3. En el MAUI, pon esa misma URL (con `/` final) en `FrontEnd/Eva/Resources/Raw/appsettings.json` → `ApiBaseUrl`.

## Datos abiertos ARESEP

La tarifa eléctrica se consulta a un endpoint público de ARESEP (`AresepService`); no requiere API key.

## Publicación

Los perfiles de publicación (`.pubxml`) y logs locales no se versionan en este repo; configura despliegue y secretos solo en tu entorno o en el portal de Azure.
