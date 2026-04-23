#!/usr/bin/env bash
# Despliegue estable en Android: evita fallos de `dotnet run` (p. ej. "could not retrieve PID")
# y builds incompletos en .NET 10. Ver dotnet/android#10500.
set -euo pipefail
cd "$(dirname "$0")"
if [[ -z "${MAPBOX_DOWNLOADS_TOKEN:-}" ]]; then
  echo "Exporta MAPBOX_DOWNLOADS_TOKEN (secret sk. con DOWNLOADS:READ) y vuelve a ejecutar." >&2
  exit 1
fi
CFG="${1:-Release}"
if [[ $# -ge 1 ]]; then shift; fi
exec dotnet build Eva.csproj -c "$CFG" -f net10.0-android -t:Run "$@"
