# Eva — .NET MAUI + API

## Legal notice

© 2026 JuanCulligan. All rights reserved.

This project is not licensed for public use, distribution, or modification. Commercial use is not permitted. You may not edit or redistribute this application without explicit authorization from the copyright holder.

## Estructura del repositorio

| Carpeta | Descripción |
|---------|-------------|
| `FrontEnd/` | Cliente .NET MAUI (`FrontEnd/Eva/`). |
| `BackEnd/` | API ASP.NET Web API (.NET Framework 4.8) y capas asociadas. |

En el remoto público **no** se incluyen: contraseñas de base de datos reales, URLs de despliegue personales, API keys, tokens Mapbox, ni logs o perfiles de publicación con datos sensibles. Debes configurar todo en local (ver README de cada capa).

## Cómo ejecutar el sistema completo

1. **Backend** (Windows + Visual Studio): sigue [BackEnd/README.md](BackEnd/README.md) (SQL Server, `Web.config`, variables de entorno, F5 con IIS Express).
2. **Frontend** (MAUI): sigue [FrontEnd/README.md](FrontEnd/README.md) (`appsettings.json` → `ApiBaseUrl` apuntando al API, tokens Mapbox `pk` y `MAPBOX_DOWNLOADS_TOKEN` para Android).

Orden típico: levantar el API → copiar la URL base (p. ej. `https://localhost:44352/`) en `FrontEnd/Eva/Resources/Raw/appsettings.json` → compilar/ejecutar la app MAUI.

## Mapbox (solo desarrollo local)

Para el **mapa en el cliente** MAUI, token público (`pk.…`) en:

- `FrontEnd/Eva/MapPage.xaml.cs` → `MapboxAccessToken`
- `FrontEnd/Eva/Platforms/Android/Resources/values/mapbox_access_token.xml` → `mapbox_access_token`

Para **compilar Android** con el módulo nativo Mapbox, exporta `MAPBOX_DOWNLOADS_TOKEN` (secret `sk.…` con DOWNLOADS:READ); detalle en [FrontEnd/README.md](FrontEnd/README.md).

No subas tokens reales en commits públicos.
