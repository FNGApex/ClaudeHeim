#!/bin/bash
# One-time setup for the ClaudeHeim Linux runner: installs BepInEx 5.4.23.5 (unix doorstop) into the WSL Valheim.
# Idempotent. Needs the native Linux Valheim already installed through the WSL Steam client.
#
#   setup-bepinex.sh <game dir> [fresh]
#   fresh = delete the game's Unity prefs and do not seed them (a first-ever launch, as a new user has it)
set -eu
GAME=$1
FRESH=${2:-}
VER=5.4.23.5
[ -x "$GAME/valheim.x86_64" ] || { echo "no valheim.x86_64 in $GAME"; exit 1; }

# The Linux build ships valheim_Data/Plugins/libparty.so but PlayFab Party asks for libParty.so (case-sensitive
# file system): without the link every run logs a DllNotFoundException that 'expect noerrors' counts.
# libparty.so also needs libpulse-mainloop-glib.so.0 (apt: libpulse-mainloop-glib0).
[ -e "$GAME/valheim_Data/Plugins/libParty.so" ] || ln -s libparty.so "$GAME/valheim_Data/Plugins/libParty.so"

# A fresh prefs store has no "language" key; PlatformPrefs then asks SteamUtils.IsSteamRunningOnSteamDeck(), which
# throws when a plugin (Auga's Awake) reaches Localization.instance before Steam is up. Seed the key like a
# Windows install that has been played once. Unity's Linux prefs keep string values base64-encoded. With BepInEx the
# chainloader touches PlayerPrefs before Unity knows company/product, so the file lives under unknown/unknown.
PREFS="$HOME/.config/unity3d/unknown/unknown/prefs"
if [ "$FRESH" = "fresh" ]; then
    rm -f "$PREFS"
    echo "prefs cleared (fresh install)"
elif [ ! -f "$PREFS" ]; then
    mkdir -p "$(dirname "$PREFS")"
    printf '<unity_prefs version_major="1" version_minor="1">\n\t<pref name="language" type="string">%s</pref>\n</unity_prefs>\n' \
        "$(printf English | base64)" >"$PREFS"
elif ! grep -q 'name="language"' "$PREFS"; then
    sed -i "s|</unity_prefs>|\t<pref name=\"language\" type=\"string\">$(printf English | base64)</pref>\n</unity_prefs>|" "$PREFS"
fi

if [ -f "$GAME/BepInEx/core/BepInEx.dll" ] && [ -f "$GAME/run_bepinex.sh" ]; then echo "BepInEx already installed"; exit 0; fi
tmp=$(mktemp -d)
curl -fsSL -o "$tmp/bep.zip" "https://github.com/BepInEx/BepInEx/releases/download/v$VER/BepInEx_linux_x64_$VER.zip"
unzip -oq "$tmp/bep.zip" -d "$GAME"
rm -rf "$tmp"
sed -i 's/\r$//' "$GAME/run_bepinex.sh"
chmod +x "$GAME/run_bepinex.sh" "$GAME/valheim.x86_64"
mkdir -p "$GAME/BepInEx/plugins"
echo "BepInEx $VER installed in $GAME"
