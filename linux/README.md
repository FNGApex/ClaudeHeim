# ClaudeHeim on Linux (WSL2)

`Run-ClaudeHeim-Linux.ps1` runs a scenario on the native Linux Valheim inside WSL2, on a private Xvfb display: no
window, no focus, no sound on the Windows desktop, and the Windows game stays free. Parameters match
`Run-ClaudeHeim.ps1` (`-Scenario -Mods -Vanilla -OutDir -Owner -Resolution`) plus `-Renderer gpu|software`.

```powershell
ClaudeHeim\Run-ClaudeHeim-Linux.ps1 -Scenario hp-regen.chs -Mods Auga
```

## One-time setup

1. WSL packages (as root): `dpkg --add-architecture i386`, multiverse enabled, then
   `apt install xvfb mesa-utils libgl1-mesa-dri libgl1-mesa-dri:i386 libgl1:i386 steam-installer libpulse-mainloop-glib0 x11vnc`
   (`libpulse-mainloop-glib0`: PlayFab's libparty.so needs it; `x11vnc`: only for `-Watch`).
   For `-Watch` on Windows: `winget install TigerVNC.TigerVNC`.
2. Start `steam` once inside WSL (its window shows on the desktop through WSLg), log in with "Remember me".
   The game needs a running, logged-in Steam client (SteamAPI_Init); the runner starts `steam -silent` when none runs.
3. Install Valheim (app 892970) through that client. The native Linux build lands in
   `~/.steam/debian-installation/steamapps/common/Valheim`.
4. BepInEx 5.4.23.5 (`BepInEx_linux_x64`, unix doorstop) is installed by `linux/setup-bepinex.sh`, which the
   runner calls on every run (it does nothing when BepInEx is already there).
5. The test character and world must exist for the Linux game: Steam Cloud brings them along when the WSL Steam
   account is the one that owns them; otherwise copy `RETEP.fch` to `~/.config/unity3d/IronGate/Valheim/characters_local`
   and the `TestWorld.*` files to `.../worlds_local`.

## How a run works

- Builds ClaudeHeim (and `-Mods`) on Windows, copies them plus the installed ValheimCreative / ValheimSurvival
  (unless `-Vanilla`) into the WSL game's `BepInEx/plugins`, and removes them afterwards.
- `linux/claudeheim-run.sh` picks a free X display from `:90` up, starts Xvfb at `-Resolution`, and launches
  `run_bepinex.sh ./valheim.x86_64 -force-glcore` with `GALLIUM_DRIVER=d3d12` (GPU through `/dev/dxg`) or
  `llvmpipe` (`-Renderer software`). `SteamAppId=892970` keeps Steam from relaunching the game; PulseAudio is
  pointed nowhere so nothing reaches the speakers.
- Output (`LogOutput.log`, Unity's `Player.log`, screenshots, dumps, `result.json`) is copied to `-OutDir`
  (default `%TEMP%\claudeheim_run_linux`).
- Lock: `.game-lock-linux` in the folder above ClaudeHeim. The Windows `.game-lock` is not involved.
