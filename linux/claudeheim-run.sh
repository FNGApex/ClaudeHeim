#!/bin/bash
# ClaudeHeim Linux runner, game side. Called by Run-ClaudeHeim-Linux.ps1 through wsl.exe; runs one game session
# on a private Xvfb display (nothing appears on the user's desktop) and returns when the game has exited.
#
#   claudeheim-run.sh <game dir> <WxH> <timeout s> <gpu|software> [NAME=value ...]
#
# NAME=value pairs are exported into the game's environment (CLAUDEHEIM, CLAUDEHEIM_SCRIPT, CLAUDEHEIM_OUT, ...).
# gpu = Mesa d3d12 on the WSL GPU (/dev/dxg), software = llvmpipe.
set -u
GAME=$1; RES=$2; TIMEOUT=$3; RENDER=$4; shift 4
W=${RES%x*}; H=${RES#*x}
export PATH="$PATH:/usr/games"

# SteamAPI_Init needs a running Steam client logged in to an account that owns Valheim. It gets its own Xvfb
# display (:89) so no Steam window ever lands on the user's desktop through WSLg.
if ! pgrep -f 'ubuntu12_32/steam( |$)' >/dev/null; then
    echo "starting steam"
    if [ ! -e /tmp/.X11-unix/X89 ]; then
        setsid nohup Xvfb :89 -screen 0 1280x800x24 -nolisten tcp >/dev/null 2>&1 </dev/null &
        sleep 1
    fi
    DISPLAY=:89 setsid nohup steam -silent >"$HOME/steam_run.log" 2>&1 </dev/null &
    for _ in $(seq 1 90); do [ -S "$HOME/.steam/steam.pipe" ] || [ -p "$HOME/.steam/steam.pipe" ] && break; sleep 1; done
    sleep 20
fi

# First free X display from :90 up.
D=90
while [ -e "/tmp/.X11-unix/X$D" ] || [ -e "/tmp/.X$D-lock" ]; do D=$((D + 1)); done
Xvfb ":$D" -screen 0 "${W}x${H}x24" -nolisten tcp >/dev/null 2>&1 &
XVFB=$!
trap 'kill $XVFB 2>/dev/null' EXIT
for _ in $(seq 1 50); do [ -e "/tmp/.X11-unix/X$D" ] && break; sleep 0.1; done

export DISPLAY=":$D"
# CLAUDEHEIM_WATCH_PORT=n: serve the display view-only over VNC on localhost:n (WSL forwards it to Windows).
for kv in "$@"; do case "$kv" in CLAUDEHEIM_WATCH_PORT=*) WATCH_PORT=${kv#*=} ;; esac; done
if [ -n "${WATCH_PORT:-}" ]; then
    # WSLg exports WAYLAND_DISPLAY, and x11vnc refuses to run when it sees one.
    env -u WAYLAND_DISPLAY x11vnc -display ":$D" -viewonly -shared -forever -nopw -localhost -rfbport "$WATCH_PORT" -quiet -bg -o "$HOME/claudeheim/x11vnc.log" >/dev/null 2>&1
fi
# Launched outside Steam: SteamAppId keeps SteamAPI_RestartAppIfNecessary from relaunching through Steam.
export SteamAppId=892970 SteamGameId=892970
# No sound on the user's speakers (WSLg would route PulseAudio to Windows).
export PULSE_SERVER=unix:/nonexistent SDL_AUDIODRIVER=dummy
API=-force-glcore
if [ "$RENDER" = "software" ]; then
    export GALLIUM_DRIVER=llvmpipe LIBGL_ALWAYS_SOFTWARE=1
elif [ "$RENDER" = "vulkan" ]; then
    # Vulkan like a real Linux player; in WSL the only Vulkan device is Mesa's lavapipe (CPU).
    API=-force-vulkan
    export VK_ICD_FILENAMES=/usr/share/vulkan/icd.d/lvp_icd.json
else
    export GALLIUM_DRIVER=d3d12
fi
for kv in "$@"; do export "$kv"; done

cd "$GAME" || exit 10
echo "display :$D renderer $RENDER ($(glxinfo -B 2>/dev/null | sed -n 's/^OpenGL renderer string: //p'))"
PLAYERLOG="$HOME/.config/unity3d/IronGate/Valheim/Player.log"
rm -f "$PLAYERLOG"
timeout -k 15 "$TIMEOUT" ./run_bepinex.sh ./valheim.x86_64 "$API" -screen-fullscreen 0 -screen-width "$W" -screen-height "$H" >/dev/null 2>&1 &
GAMEPID=$!
# Without Steam the game hangs at the loading screen until the timeout. Stop it as soon as SteamAPI_Init fails
# (typically: the WSL account borrows Valheim via Steam Family Sharing and the owner is playing right now).
while kill -0 $GAMEPID 2>/dev/null; do
    if grep -q "SteamAPI_Init() failed" "$PLAYERLOG" 2>/dev/null; then
        echo "STEAM INIT FAILED - is Valheim running on the owner account (Windows game / Family Sharing lock)? stopping"
        pkill -f valheim.x86_64; sleep 3
        break
    fi
    grep -q "Steam initialized" "$PLAYERLOG" 2>/dev/null && break
    sleep 1
done
wait $GAMEPID
rc=$?
[ $rc -eq 124 ] && echo "TIMEOUT after $TIMEOUT s"
echo "game exit code $rc"
exit $rc
