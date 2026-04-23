#!/usr/bin/env bash
# Compila el módulo :nav a AAR. Requiere MAPBOX_DOWNLOADS_TOKEN (secret token de Mapbox con permiso Downloads:Read).
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

if [[ -z "${MAPBOX_DOWNLOADS_TOKEN:-}" ]]; then
  echo "ERROR: MAPBOX_DOWNLOADS_TOKEN no está definido." >&2
  echo "Ejemplo: export MAPBOX_DOWNLOADS_TOKEN='sk...' && dotnet publish ... (no uses -p: el token JWT contiene '=' y MSBuild lo trunca)" >&2
  exit 2
fi

GRADLE_VER="8.9"
GRADLE_HOME="$SCRIPT_DIR/../.gradle-local/gradle-${GRADLE_VER}"
GRADLE_ZIP="$(dirname "$GRADLE_HOME")/gradle-${GRADLE_VER}-bin.zip"
if [[ ! -d "$GRADLE_HOME" ]]; then
  mkdir -p "$(dirname "$GRADLE_ZIP")"
  curl -L -o "$GRADLE_ZIP" "https://services.gradle.org/distributions/gradle-${GRADLE_VER}-bin.zip"
  unzip -q "$GRADLE_ZIP" -d "$(dirname "$GRADLE_HOME")"
fi

# Major version de Java (17, 21, 25, …). No usar /usr/libexec/java_home -v 17 solo: en macOS puede
# devolver Java 25 ("17+"), y Gradle 8.9 + Groovy falla con class file major 69.
_java_major() {
  "$1/bin/java" -version 2>&1 | head -1 | sed -E 's/.* version "([0-9]+).*/\1/'
}

_pick_jdk_macos() {
  local home maj want
  for want in 21 17; do
    for home in /Library/Java/JavaVirtualMachines/*.jdk/Contents/Home; do
      [[ -d "$home" && -x "$home/bin/java" ]] || continue
      maj="$(_java_major "$home")"
      if [[ "$maj" == "$want" ]]; then
        echo "$home"
        return 0
      fi
    done
    for home in \
      "/opt/homebrew/opt/openjdk@21/libexec/openjdk.jdk/Contents/Home" \
      "/opt/homebrew/opt/openjdk@17/libexec/openjdk.jdk/Contents/Home" \
      "/usr/local/opt/openjdk@21/libexec/openjdk.jdk/Contents/Home" \
      "/usr/local/opt/openjdk@17/libexec/openjdk.jdk/Contents/Home" \
      ; do
      [[ -d "$home" && -x "$home/bin/java" ]] || continue
      maj="$(_java_major "$home")"
      if [[ "$maj" == "$want" ]]; then
        echo "$home"
        return 0
      fi
    done
  done
  return 1
}

_pick_jdk_linux() {
  local home maj want
  for want in 21 17; do
    for home in /usr/lib/jvm/java-21-* /usr/lib/jvm/java-17-* /usr/lib/jvm/temurin-21-* /usr/lib/jvm/temurin-17-*; do
      [[ -d "$home" && -x "$home/bin/java" ]] || continue
      maj="$(_java_major "$home")"
      if [[ "$maj" == "$want" ]]; then
        echo "$home"
        return 0
      fi
    done
  done
  return 1
}

export JAVA_HOME
if [[ "$(uname -s)" == "Darwin" ]]; then
  _picked="$(_pick_jdk_macos || true)"
  if [[ -n "${_picked:-}" ]]; then
    JAVA_HOME="$_picked"
  fi
else
  if [[ -n "${JAVA_HOME:-}" && -x "${JAVA_HOME}/bin/java" ]]; then
    _maj="$(_java_major "$JAVA_HOME")"
    if [[ "$_maj" != "17" && "$_maj" != "21" ]]; then
      JAVA_HOME=""
    fi
  fi
  if [[ -z "${JAVA_HOME:-}" ]]; then
    _picked="$(_pick_jdk_linux || true)"
    if [[ -n "${_picked:-}" ]]; then
      JAVA_HOME="$_picked"
    fi
  fi
fi

if [[ -z "${JAVA_HOME:-}" ]] || [[ ! -x "${JAVA_HOME}/bin/java" ]]; then
  echo "ERROR: Necesitas JDK 17 o 21 instalado (no solo 8/11/25) para compilar el módulo nativo." >&2
  echo "macOS: brew install --cask temurin@17" >&2
  echo "Luego verifica: /usr/libexec/java_home -V" >&2
  exit 3
fi

_maj="$(_java_major "$JAVA_HOME")"
if [[ "$_maj" != "17" && "$_maj" != "21" ]]; then
  echo "ERROR: JAVA_HOME debe ser Java 17 o 21; ahora es ${_maj} ($JAVA_HOME)." >&2
  exit 3
fi

LOG="$SCRIPT_DIR/last-gradle-build.log"
# Aislar de ~/.gradle (init.d, caches, etc.) para builds reproducibles (CI/nube).
export GRADLE_USER_HOME="$SCRIPT_DIR/.gradle-user-home"
set +e
"$GRADLE_HOME/bin/gradle" --console=plain --no-daemon :nav:touchMauiDepsExported --stacktrace 2>&1 | tee "$LOG"
STATUS=${PIPESTATUS[0]}
set -e
exit "$STATUS"
