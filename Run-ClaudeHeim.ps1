# ClaudeHeim: one unattended scenario run.
# Takes the shared .game-lock, deploys ClaudeHeim (+ optional mods under test), starts the game windowed with
# CLAUDEHEIM=1, waits for it to quit, collects BepInEx log + screenshots + result.json into -OutDir, then removes
# everything it deployed and releases the lock.
#
#   -Scenario  path of a .chs scenario file (relative paths resolve against ClaudeHeim/scenarios)
#   -Mods      extra mods to deploy for the run and remove afterwards. Known names: Auga.
#              (ValheimCreative / ValheimSurvival are normally installed already and are left alone.)
#   -Resolution WxH window size for the run (default 1600x900; e.g. 2560x1080 for ultrawide layouts)
#   -Vanilla   moves every other plugin folder aside for the run (BepInEx\plugins_claudeheim_aside) and restores it
#              afterwards, so the game runs with ClaudeHeim + -Mods only.
#   -Foreground run in a normal window. By default the run stays out of the user's way (background mode): the window
#              sits off-screen with no taskbar button, focus goes straight back, audio stays off, 30 fps cap, and the
#              process runs at below-normal priority. The runner reports how long the game held focus.
#   -Monitor   hardware id (or part of it) of the monitor the background window goes on, e.g. GSM5B09 (list them with
#              Get-CimInstance -Namespace root\wmi WmiMonitorID). Matched by id, not by index, so it still works when
#              another monitor is switched off. Default: "monitor" in runner.local.json next to this script (git-ignored).
#              Not set or not connected: the window goes off-screen.
#   -ValheimDir game folder (default: env VALHEIM_DIR, then the usual Steam folders)
# The lock file and -Mods projects are looked up in the folder that contains ClaudeHeim (e.g. Auga cloned next to it).
param(
    [Parameter(Mandatory = $true)][string]$Scenario,
    [string]$OutDir = "$env:TEMP\claudeheim_run",
    [string[]]$Mods = @(),
    [switch]$Vanilla,
    [switch]$Foreground,
    [string]$Monitor = "",
    [int]$TimeoutSeconds = 600,
    [string]$Owner = "claudeheim",
    [string]$Resolution = "1600x900",
    [string]$ValheimDir = ""
)

if (-not $ValheimDir) {
    $ValheimDir = @($env:VALHEIM_DIR, "C:\Program Files (x86)\Steam\steamapps\common\Valheim", "D:\SteamLibrary\steamapps\common\Valheim") |
        Where-Object { $_ -and (Test-Path (Join-Path $_ "valheim.exe")) } | Select-Object -First 1
    if (-not $ValheimDir) { "Valheim not found: pass -ValheimDir or set VALHEIM_DIR"; exit 1 }
}

$here = $PSScriptRoot
$root = Split-Path $here -Parent
$lock = Join-Path $root ".game-lock"
$plugins = Join-Path $ValheimDir "BepInEx\plugins"
$aside = Join-Path $ValheimDir "BepInEx\plugins_claudeheim_aside"

if (-not (Test-Path $Scenario)) { $Scenario = Join-Path $here "scenarios\$Scenario" }
if (-not (Test-Path $Scenario)) { "scenario not found: $Scenario"; exit 1 }
$Scenario = (Resolve-Path $Scenario).Path

if (Get-Process valheim -ErrorAction SilentlyContinue) { "valheim is already running - not starting a test"; exit 2 }
if ((Test-Path $lock) -and ((Get-Date) - (Get-Item $lock).LastWriteTime).TotalMinutes -lt 15) { "fresh .game-lock held by: " + (Get-Content $lock -Raw); exit 3 }
"$Owner $(Get-Date -Format s) claudeheim $(Split-Path $Scenario -Leaf)" | Set-Content $lock

$runOut = Join-Path $ValheimDir "BepInEx\ClaudeHeim\run"
$moved = @()
try {
    $build = dotnet build (Join-Path $here "ClaudeHeim.csproj") -c Release -v q -p:DeployClaudeHeim=true "-p:ValheimDir=$ValheimDir" 2>&1
    if ($LASTEXITCODE -ne 0) { $build | Select-String "error" | Select-Object -First 10; "ClaudeHeim build failed"; exit 4 }

    foreach ($mod in $Mods) {
        switch ($mod) {
            "Auga" {
                $build = dotnet build (Join-Path $root "Auga\Auga\Auga.csproj") -c Release -v q -p:DeployAuga=true "-p:ValheimDir=$ValheimDir" 2>&1
                if ($LASTEXITCODE -ne 0) { $build | Select-String "error" | Select-Object -First 10; "Auga build failed"; exit 4 }
            }
            default { "unknown mod '$mod' (known: Auga)"; exit 5 }
        }
    }

    if ($Vanilla) {
        [IO.Directory]::CreateDirectory($aside) | Out-Null
        foreach ($d in Get-ChildItem $plugins) {
            if ($d.Name -eq "ClaudeHeim" -or $Mods -contains $d.Name) { continue }
            Move-Item $d.FullName (Join-Path $aside $d.Name)
            $moved += $d.Name
        }
    }

    $env:CLAUDEHEIM = "1"
    $env:CLAUDEHEIM_SCRIPT = $Scenario
    $env:CLAUDEHEIM_OUT = $runOut
    $gameArgs = @("-screen-fullscreen", "0", "-screen-width", ($Resolution -split "x")[0], "-screen-height", ($Resolution -split "x")[1])
    if (-not $Foreground) {
        Add-Type -Namespace ClaudeHeimRun -Name User32 -MemberDefinition @'
[DllImport("user32.dll")] public static extern System.IntPtr GetForegroundWindow();
[DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(System.IntPtr hWnd, out uint pid);
[DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(System.IntPtr hWnd, System.IntPtr unused);
[DllImport("user32.dll")] public static extern bool SetForegroundWindow(System.IntPtr hWnd);
[DllImport("user32.dll")] public static extern bool AttachThreadInput(uint a, uint b, bool attach);
[DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
[DllImport("user32.dll")] public static extern int GetSystemMetrics(int i);
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct DisplayDevice {
    public int cb;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string DeviceName;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceString;
    public int StateFlags;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceID;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceKey;
}
[DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern bool EnumDisplayDevices(string device, uint index, ref DisplayDevice dd, uint flags);
// Hardware id of the monitor on an adapter output such as DISPLAY2, e.g. "DISPLAY#GSM5B09#...".
public static string MonitorId(string output) {
    var dd = new DisplayDevice(); dd.cb = Marshal.SizeOf(dd);
    return EnumDisplayDevices(output, 0, ref dd, 1) ? dd.DeviceID : "";
}
// Give focus back to 'back' when process 'pid' holds it. Borrowing the foreground thread's input state is what
// lets a background process do this. Returns true when it acted.
public static bool ReturnFocus(uint pid, System.IntPtr back) {
    System.IntPtr fg = GetForegroundWindow(); uint owner;
    GetWindowThreadProcessId(fg, out owner);
    if (owner != pid || back == System.IntPtr.Zero) return false;
    uint fgThread = GetWindowThreadProcessId(fg, System.IntPtr.Zero), me = GetCurrentThreadId();
    AttachThreadInput(me, fgThread, true);
    bool ok = SetForegroundWindow(back);
    AttachThreadInput(me, fgThread, false);
    return ok;
}
'@
        if (-not $Monitor) {
            $localCfg = Join-Path $here "runner.local.json"
            if (Test-Path $localCfg) { $Monitor = (Get-Content $localCfg -Raw | ConvertFrom-Json).monitor }
        }
        $winPos = $null
        if ($Monitor) {
            Add-Type -AssemblyName System.Windows.Forms
            $screen = [System.Windows.Forms.Screen]::AllScreens | Where-Object { [ClaudeHeimRun.User32]::MonitorId($_.DeviceName) -like "*$Monitor*" } | Select-Object -First 1
            if ($screen) {
                $w = [int]($Resolution -split "x")[0]; $h = [int]($Resolution -split "x")[1]
                $b = $screen.Bounds
                $winPos = @([int]($b.X + [Math]::Max(0, ($b.Width - $w) / 2)), [int]($b.Y + [Math]::Max(0, ($b.Height - $h) / 2)))
                "--- background: window on monitor $Monitor ($($screen.DeviceName) $($b.Width)x$($b.Height) at $($b.X),$($b.Y))"
            } else { "--- background: monitor '$Monitor' not connected - window goes off-screen" }
        }
        if ($winPos) { $env:CLAUDEHEIM_WINDOW_POS = "$($winPos[0]),$($winPos[1])" }
        Add-Type -Path (Join-Path $here "runner\AudioPeak.cs")
        $env:CLAUDEHEIM_BACKGROUND = "1"
        $env:CLAUDEHEIM_RETURN_FOCUS = [string][ClaudeHeimRun.User32]::GetForegroundWindow().ToInt64()
        $gameArgs += "-popupwindow"
    }
    # The test instance saves its window mode/size/position into the user's own Valheim prefs on quit: snapshot the
    # whole key now and put it back after the run.
    $prefsKey = "HKCU\Software\IronGate\Valheim"
    $prefsBackup = Join-Path $env:TEMP "claudeheim_prefs_$PID.reg"
    reg.exe export $prefsKey $prefsBackup /y | Out-Null
    if (-not $Foreground) {
        # Unity opens the window where it last was: start it past the right edge of the desktop (SM_XVIRTUALSCREEN + SM_CXVIRTUALSCREEN).
        # (or straight onto the chosen monitor). A negative x (monitor left of the primary) goes in as a two's-complement DWORD.
        $startX = if ($winPos) { $winPos[0] } else { [ClaudeHeimRun.User32]::GetSystemMetrics(76) + [ClaudeHeimRun.User32]::GetSystemMetrics(78) + 64 }
        $startY = if ($winPos) { $winPos[1] } else { 0 }
        $unityKey = "HKCU:\Software\IronGate\Valheim"
        Set-ItemProperty $unityKey "Screenmanager Window Position X_h4088080503" ([int]$startX) -Type DWord
        Set-ItemProperty $unityKey "Screenmanager Window Position Y_h4088080502" ([int]$startY) -Type DWord
        # No BepInEx console window for the run (restored below).
        $bepCfg = Join-Path $ValheimDir "BepInEx\config\BepInEx.cfg"
        $bepCfgText = [IO.File]::ReadAllText($bepCfg)
        [IO.File]::WriteAllText($bepCfg, [regex]::Replace($bepCfgText, "(\[Logging\.Console\][^\[]*?\r?\nEnabled = )true", '${1}false'))
    }
    $p = Start-Process (Join-Path $ValheimDir "valheim.exe") -ArgumentList $gameArgs -WorkingDirectory $ValheimDir -PassThru
    $focusSamples = 0; $focusFirst = $null; $soundSamples = 0; $soundFirst = $null; $soundMax = 0.0; $soundSessions = 0
    if (-not $Foreground) {
        try { $p.PriorityClass = "BelowNormal" } catch {}
        # Sample who holds focus every 100 ms: the report says whether the test instance ever got in the user's way.
        $sw = [Diagnostics.Stopwatch]::StartNew()
        while (-not $p.HasExited -and $sw.Elapsed.TotalSeconds -lt $TimeoutSeconds) {
            $fgPid = 0
            [ClaudeHeimRun.User32]::GetWindowThreadProcessId([ClaudeHeimRun.User32]::GetForegroundWindow(), [ref]$fgPid) | Out-Null
            if ($fgPid -eq $p.Id) {
                $focusSamples++; if ($null -eq $focusFirst) { $focusFirst = $sw.Elapsed.TotalSeconds }
                [ClaudeHeimRun.User32]::ReturnFocus([uint32]$p.Id, [IntPtr][long]$env:CLAUDEHEIM_RETURN_FOCUS) | Out-Null
            }
            # Windows' own peak meter for the game's audio sessions: did anything reach the speakers?
            $sessions = 0
            $peak = [ClaudeHeimRun.AudioPeak]::Peak($p.Id, [ref]$sessions)
            if ($sessions -gt $soundSessions) { $soundSessions = $sessions }
            if ($peak -gt $soundMax) { $soundMax = $peak }
            if ($peak -gt 0.001) { $soundSamples++; if ($null -eq $soundFirst) { $soundFirst = $sw.Elapsed.TotalSeconds } }
            Start-Sleep -Milliseconds 50
        }
    }
    if (-not $p.WaitForExit([Math]::Max(1, $TimeoutSeconds * 1000 - $(if ($sw) { [int]$sw.Elapsed.TotalMilliseconds } else { 0 })))) { "TIMEOUT after $TimeoutSeconds s - stopping the test instance"; try { Stop-Process -Id $p.Id -Force } catch {}; Start-Sleep 3 }

    [IO.Directory]::CreateDirectory($OutDir) | Out-Null
    Get-ChildItem $OutDir -File -ErrorAction SilentlyContinue | ForEach-Object { [IO.File]::Delete($_.FullName) }
    Copy-Item (Join-Path $ValheimDir "BepInEx\LogOutput.log") (Join-Path $OutDir "LogOutput.log") -Force
    if (Test-Path $runOut) { Copy-Item (Join-Path $runOut "*") $OutDir -Force }
}
finally {
    $env:CLAUDEHEIM = $null
    $env:CLAUDEHEIM_BACKGROUND = $null
    $env:CLAUDEHEIM_WINDOW_POS = $null
    if ($bepCfgText) { [IO.File]::WriteAllText($bepCfg, $bepCfgText) }
    if ($prefsBackup -and (Test-Path $prefsBackup)) {
        if (-not $p -or $p.HasExited) {
            reg.exe delete $prefsKey /f | Out-Null
            reg.exe import $prefsBackup 2>&1 | Out-Null
            [IO.File]::Delete($prefsBackup)
        } else { "game still running: Valheim prefs NOT restored, backup kept at $prefsBackup" }
    }
    $env:CLAUDEHEIM_RETURN_FOCUS = $null
    foreach ($name in $moved) { Move-Item (Join-Path $aside $name) (Join-Path $plugins $name) }
    if ((Test-Path $aside) -and -not (Get-ChildItem $aside)) { [IO.Directory]::Delete($aside) }
    $deployed = @("ClaudeHeim") + $Mods
    foreach ($name in $deployed) {
        $dir = Join-Path $plugins $name
        if (Test-Path $dir) { [IO.Directory]::Delete($dir, $true) }
    }
    if (Test-Path $lock) { [IO.File]::Delete($lock) }
}

"cleaned up: plugins now = " + ((Get-ChildItem $plugins).Name -join ", ") + " ; lock released: " + (-not (Test-Path $lock))
$result = Join-Path $OutDir "result.json"
if (Test-Path $result) {
    $r = Get-Content $result -Raw | ConvertFrom-Json
    "--- result: $($r.commands) commands, $($r.failures.Count) failed, $($r.errors.Count) distinct game errors, $($r.screenshots) screenshots"
    $r.failures | ForEach-Object { "FAIL  $_" }
    $r.errors | ForEach-Object { "ERROR {0}x {1}" -f $_.count, $_.text.Substring(0, [Math]::Min(700, $_.text.Length)) }
} else {
    "--- no result.json: the scenario did not finish. Last ClaudeHeim lines:"
    Select-String -Path (Join-Path $OutDir "LogOutput.log") -Pattern "ClaudeHeim" | Select-Object -Last 15 | ForEach-Object { $_.Line }
}
if (-not $Foreground) {
    if ($focusSamples -eq 0) { "--- background: the game never held focus" }
    else { "--- background: the game held focus for ~{0:0.00} s (first at {1:0.0} s after launch)" -f ($focusSamples / 20), $focusFirst }
    if ($soundSessions -eq 0) { "--- background: audio not metered (the game opened no audio session on the default output device)" }
    elseif ($soundSamples -eq 0) { "--- background: silent (Windows mixer peak never above 0.001; max {0:0.0000}; {1} session(s) metered)" -f $soundMax, $soundSessions }
    else { "--- background: SOUND for ~{0:0.00} s (first at {1:0.0} s after launch, max peak {2:0.000})" -f ($soundSamples / 20), $soundFirst, $soundMax }
}
"--- output in $OutDir"
