# ClaudeHeim: one unattended scenario run on the native Linux Valheim inside WSL2.
# Same job as Run-ClaudeHeim.ps1, but the game runs in WSL on a private Xvfb display: no window on the desktop, no
# focus, no sound (CLAUDEHEIM_BACKGROUND=1 plus no audio device), and the Windows game stays free for the user. Builds on Windows, copies ClaudeHeim (+ -Mods, and
# the installed ValheimCreative / ValheimSurvival unless -Vanilla) into the WSL game's BepInEx\plugins, runs the
# scenario, collects BepInEx log + Player.log + screenshots + result.json into -OutDir, then removes what it copied.
#
#   -Scenario   path of a .chs scenario file (relative paths resolve against ClaudeHeim/scenarios)
#   -Mods       extra mods for the run. Known names: Auga.
#   -Vanilla    only ClaudeHeim + -Mods (ValheimCreative / ValheimSurvival are not copied in)
#   -Resolution WxH virtual screen for the run (default 1600x900)
#   -Renderer   gpu (OpenGL core, Mesa d3d12 on the WSL GPU, default), software (OpenGL on llvmpipe) or vulkan
#               (Vulkan on Mesa lavapipe, CPU; the API real Linux players get)
#   -Watch      show the run in a view-only VNC window (TigerVNC vncviewer) on the monitor from runner.local.json
#               ("monitor", same as Run-ClaudeHeim.ps1; else the primary). It cannot send input; closing it does not
#               affect the run; the runner closes it at the end. -WatchPort sets the localhost port (default 5990).
#   -FreshPrefs delete the Linux game's Unity prefs and do not seed "language" (a new user's first launch)
#   -Distro     WSL distribution (default Ubuntu); -LinuxGameDir overrides the Valheim folder inside it
#   -ValheimDir Windows game folder, only used for the build references (default: env VALHEIM_DIR, then Steam folders)
#
# Separate install, so it does not take the Windows .game-lock; it holds .game-lock-linux next to it instead.
# One-time setup (see linux/README.md): Steam client in WSL logged in to an account that owns Valheim, Valheim
# installed through it; this script installs BepInEx by itself (linux/setup-bepinex.sh) when it is missing.
param(
    [Parameter(Mandatory = $true)][string]$Scenario,
    [string]$OutDir = "$env:TEMP\claudeheim_run_linux",
    [string[]]$Mods = @(),
    [switch]$Vanilla,
    [int]$TimeoutSeconds = 600,
    [string]$Owner = "claudeheim",
    [string]$Resolution = "1600x900",
    [ValidateSet("gpu", "software", "vulkan")][string]$Renderer = "gpu",
    [switch]$Watch,
    [int]$WatchPort = 5990,
    [switch]$FreshPrefs,
    [string]$Distro = "Ubuntu",
    [string]$LinuxGameDir = "",
    [string]$ValheimDir = ""
)

if (-not $ValheimDir) {
    $ValheimDir = @($env:VALHEIM_DIR, "C:\Program Files (x86)\Steam\steamapps\common\Valheim", "D:\SteamLibrary\steamapps\common\Valheim") |
        Where-Object { $_ -and (Test-Path (Join-Path $_ "valheim.exe")) } | Select-Object -First 1
    if (-not $ValheimDir) { "Windows Valheim not found (needed for build references): pass -ValheimDir or set VALHEIM_DIR"; exit 1 }
}

$here = $PSScriptRoot
$root = Split-Path $here -Parent
$lock = Join-Path $root ".game-lock-linux"

if (-not (Test-Path $Scenario)) { $Scenario = Join-Path $here "scenarios\$Scenario" }
if (-not (Test-Path $Scenario)) { "scenario not found: $Scenario"; exit 1 }
$Scenario = (Resolve-Path $Scenario).Path

# Linux side: every wsl call goes through -e (no shell re-parsing of the arguments).
function Wsl { & wsl.exe -d $Distro -e @args }
$linuxHome = (Wsl printenv HOME | Out-String).Trim()
if (-not $linuxHome) { "WSL distro '$Distro' not reachable"; exit 1 }
if (-not $LinuxGameDir) { $LinuxGameDir = "$linuxHome/.steam/debian-installation/steamapps/common/Valheim" }
function Unc([string]$linuxPath) { "\\wsl.localhost\$Distro" + ($linuxPath -replace "/", "\") }
$gameUnc = Unc $LinuxGameDir
if (-not (Test-Path (Join-Path $gameUnc "valheim.x86_64"))) { "Linux Valheim not found at $LinuxGameDir (install it through the WSL Steam client)"; exit 1 }

if (Wsl pgrep -f "valheim.x86_64") { "Linux valheim is already running - not starting a test"; exit 2 }
# The WSL Steam account borrows Valheim through Steam Family Sharing: while the owner account plays (the Windows game,
# a Windows test run or the user), SteamAPI_Init fails in WSL. So a Linux run needs the Windows game closed too.
if (Get-Process valheim -ErrorAction SilentlyContinue) { "Windows Valheim is running - the WSL account borrows Valheim via Steam Family Sharing and cannot play at the same time; not starting"; exit 2 }
$winLock = Join-Path $root ".game-lock"
if ((Test-Path $winLock) -and ((Get-Date) - (Get-Item $winLock).LastWriteTime).TotalMinutes -lt 15) { "fresh Windows .game-lock (Family Sharing: one Valheim at a time) held by: " + (Get-Content $winLock -Raw); exit 3 }
if ((Test-Path $lock) -and ((Get-Date) - (Get-Item $lock).LastWriteTime).TotalMinutes -lt 15) { "fresh .game-lock-linux held by: " + (Get-Content $lock -Raw); exit 3 }
"$Owner $(Get-Date -Format s) claudeheim-linux $(Split-Path $Scenario -Leaf)" | Set-Content $lock

# Runner scripts live in ~/claudeheim inside WSL (LF line endings, executable).
$work = "$linuxHome/claudeheim"
$workUnc = Unc $work
[IO.Directory]::CreateDirectory($workUnc) | Out-Null
foreach ($f in Get-ChildItem (Join-Path $here "linux") -Filter *.sh) {
    [IO.File]::WriteAllText((Join-Path $workUnc $f.Name), ([IO.File]::ReadAllText($f.FullName) -replace "`r`n", "`n"))
}
Wsl chmod +x "$work/claudeheim-run.sh" "$work/setup-bepinex.sh"

$plugins = Join-Path $gameUnc "BepInEx\plugins"
$runOutLinux = "$LinuxGameDir/BepInEx/ClaudeHeim/run"
$runOut = Unc $runOutLinux
$deployed = @()
try {
    $setup = Wsl bash "$work/setup-bepinex.sh" $LinuxGameDir $(if ($FreshPrefs) { "fresh" } else { "" }) 2>&1
    if ($LASTEXITCODE -ne 0) { $setup; "BepInEx setup failed"; exit 4 }

    $build = dotnet build (Join-Path $here "ClaudeHeim.csproj") -c Release -v q "-p:ValheimDir=$ValheimDir" 2>&1
    if ($LASTEXITCODE -ne 0) { $build | Select-String "error" | Select-Object -First 10; "ClaudeHeim build failed"; exit 4 }
    $files = @{ "ClaudeHeim" = @(Get-ChildItem (Join-Path $here "bin\Release") -Recurse -Filter ClaudeHeim.dll | Sort-Object LastWriteTime | Select-Object -Last 1 -ExpandProperty FullName) }

    foreach ($mod in $Mods) {
        switch ($mod) {
            "Auga" {
                $augaProj = Join-Path $root "Auga\Auga"
                $build = dotnet build (Join-Path $augaProj "Auga.csproj") -c Release -v q "-p:ValheimDir=$ValheimDir" 2>&1
                if ($LASTEXITCODE -ne 0) { $build | Select-String "error" | Select-Object -First 10; "Auga build failed"; exit 4 }
                $files["Auga"] = @((Join-Path $augaProj "bin\Release\Auga.dll"), (Join-Path $augaProj "translations.json"))
            }
            default { "unknown mod '$mod' (known: Auga)"; exit 5 }
        }
    }
    # The Windows run has the installed mods loaded; mirror them unless -Vanilla.
    if (-not $Vanilla) {
        foreach ($name in "ValheimCreative", "ValheimSurvival") {
            $src = Join-Path $ValheimDir "BepInEx\plugins\$name"
            if (Test-Path $src) { $files[$name] = @(Get-ChildItem $src -File | Select-Object -ExpandProperty FullName) }
        }
    }

    [IO.Directory]::CreateDirectory($plugins) | Out-Null
    foreach ($name in $files.Keys) {
        $dir = Join-Path $plugins $name
        if (Test-Path $dir) { [IO.Directory]::Delete($dir, $true) }
        [IO.Directory]::CreateDirectory($dir) | Out-Null
        foreach ($f in $files[$name]) { Copy-Item $f $dir -Force }
        $deployed += $name
    }
    "--- plugins for the run: " + ($deployed -join ", ")

    $scenarioLinux = "$work/" + (Split-Path $Scenario -Leaf)
    Copy-Item $Scenario (Unc $scenarioLinux) -Force
    if (Test-Path $runOut) { [IO.Directory]::Delete($runOut, $true) }

    $runArgs = @("-d", $Distro, "-e", "bash", "$work/claudeheim-run.sh", $LinuxGameDir, $Resolution, $TimeoutSeconds, $Renderer,
        "CLAUDEHEIM=1", "CLAUDEHEIM_BACKGROUND=1", "CLAUDEHEIM_SCRIPT=$scenarioLinux", "CLAUDEHEIM_OUT=$runOutLinux")
    if ($Watch) { $runArgs += "CLAUDEHEIM_WATCH_PORT=$WatchPort" }
    $runLog = Join-Path $env:TEMP "claudeheim_linux_$PID.log"
    $wslRun = Start-Process wsl.exe -ArgumentList $runArgs -NoNewWindow -PassThru -RedirectStandardOutput $runLog -RedirectStandardError "$runLog.err"
    if ($Watch) {
        $viewerExe = @("C:\Program Files\TigerVNC\vncviewer.exe", "C:\Program Files (x86)\TigerVNC\vncviewer.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
        if (-not $viewerExe) { "--- watch: TigerVNC vncviewer not found (winget install TigerVNC.TigerVNC)" }
        else {
            Add-Type -Namespace ClaudeHeimLinux -Name User32 -MemberDefinition @'
[DllImport("user32.dll")] public static extern System.IntPtr GetForegroundWindow();
[DllImport("user32.dll")] public static extern bool SetForegroundWindow(System.IntPtr hWnd);
[DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(System.IntPtr hWnd, System.IntPtr unused);
[DllImport("user32.dll")] public static extern bool AttachThreadInput(uint a, uint b, bool attach);
[DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
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
public static string MonitorId(string output) {
    var dd = new DisplayDevice(); dd.cb = Marshal.SizeOf(dd);
    return EnumDisplayDevices(output, 0, ref dd, 1) ? dd.DeviceID : "";
}
public static void GiveFocus(System.IntPtr back) {
    System.IntPtr fg = GetForegroundWindow();
    if (back == System.IntPtr.Zero || fg == back) return;
    uint fgThread = GetWindowThreadProcessId(fg, System.IntPtr.Zero), me = GetCurrentThreadId();
    AttachThreadInput(me, fgThread, true);
    SetForegroundWindow(back);
    AttachThreadInput(me, fgThread, false);
}
'@
            $focusBack = [ClaudeHeimLinux.User32]::GetForegroundWindow()
            # Same monitor choice as Run-ClaudeHeim.ps1: "monitor" hardware id from runner.local.json.
            $geometry = $null
            $localCfg = Join-Path $here "runner.local.json"
            $monitorId = if (Test-Path $localCfg) { (Get-Content $localCfg -Raw | ConvertFrom-Json).monitor } else { "" }
            if ($monitorId) {
                Add-Type -AssemblyName System.Windows.Forms
                $screen = [System.Windows.Forms.Screen]::AllScreens | Where-Object { [ClaudeHeimLinux.User32]::MonitorId($_.DeviceName) -like "*$monitorId*" } | Select-Object -First 1
                if ($screen) {
                    $w = [int]($Resolution -split "x")[0]; $h = [int]($Resolution -split "x")[1]; $b = $screen.WorkingArea
                    $geometry = "{0}x{1}+{2}+{3}" -f $w, $h, [int]($b.X + [Math]::Max(0, ($b.Width - $w) / 2)), [int]($b.Y + [Math]::Max(0, ($b.Height - $h) / 2))
                }
            }
            $sw = [Diagnostics.Stopwatch]::StartNew(); $open = $false
            while (-not $wslRun.HasExited -and $sw.Elapsed.TotalSeconds -lt 30 -and -not $open) {
                try { $tcp = New-Object Net.Sockets.TcpClient; $tcp.Connect("127.0.0.1", $WatchPort); $tcp.Close(); $open = $true } catch { Start-Sleep -Milliseconds 500 }
            }
            if ($open) {
                $viewerArgs = @("-ViewOnly", "-Shared", "-AlertOnFatalError=0", "-ReconnectOnError=0")
                if ($geometry) { $viewerArgs += "-geometry=$geometry" }
                $viewer = Start-Process $viewerExe -ArgumentList ($viewerArgs + "127.0.0.1::$WatchPort") -PassThru
                for ($i = 0; $i -lt 30; $i++) { Start-Sleep -Milliseconds 100; [ClaudeHeimLinux.User32]::GiveFocus($focusBack) }
                "--- watch: viewer open" + $(if ($geometry) { " at $geometry (monitor $monitorId)" } else { "" })
            } else { "--- watch: VNC port $WatchPort never opened" }
        }
    }
    $wslRun.WaitForExit()
    Get-Content $runLog, "$runLog.err" -ErrorAction SilentlyContinue | Where-Object { $_ } | ForEach-Object { "--- linux: $_" }
    Remove-Item $runLog, "$runLog.err" -ErrorAction SilentlyContinue
    if ($viewer -and -not $viewer.HasExited) { Stop-Process -Id $viewer.Id -Force -ErrorAction SilentlyContinue }

    [IO.Directory]::CreateDirectory($OutDir) | Out-Null
    Get-ChildItem $OutDir -File -ErrorAction SilentlyContinue | ForEach-Object { [IO.File]::Delete($_.FullName) }
    Copy-Item (Join-Path $gameUnc "BepInEx\LogOutput.log") (Join-Path $OutDir "LogOutput.log") -Force
    $playerLog = Unc "$linuxHome/.config/unity3d/IronGate/Valheim/Player.log"
    if (Test-Path $playerLog) { Copy-Item $playerLog (Join-Path $OutDir "Player.log") -Force }
    if (Test-Path $runOut) { Copy-Item (Join-Path $runOut "*") $OutDir -Force }
}
finally {
    if ($viewer -and -not $viewer.HasExited) { Stop-Process -Id $viewer.Id -Force -ErrorAction SilentlyContinue }
    if (Wsl pgrep -f "valheim.x86_64") { Wsl pkill -f "valheim.x86_64"; Start-Sleep 3 }
    foreach ($name in $deployed) {
        $dir = Join-Path $plugins $name
        if (Test-Path $dir) { [IO.Directory]::Delete($dir, $true) }
    }
    if (Test-Path $lock) { [IO.File]::Delete($lock) }
}

"cleaned up: linux plugins now = " + ((Get-ChildItem $plugins).Name -join ", ") + " ; lock released: " + (-not (Test-Path $lock))
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
# Test saves the scenario created (newchar / newworld): add them to the machine's registry (Manage-TestSaves.ps1).
$testSaves = Join-Path $OutDir "testsaves.jsonl"
if (Test-Path $testSaves) {
    $registryPath = Join-Path $root "TEST_SAVES.json"
    $reg = if (Test-Path $registryPath) { Get-Content $registryPath -Raw | ConvertFrom-Json } else { [pscustomobject]@{ saves = @(); protected = @() } }
    foreach ($line in Get-Content $testSaves) {
        $s = $line | ConvertFrom-Json
        $known = $reg.saves | Where-Object { $_.kind -eq $s.kind -and $_.name -eq $s.name -and $_.platform -eq "linux" -and $_.status -eq "active" }
        if ($known) { continue }
        $reg.saves = @($reg.saves) + [pscustomobject]@{
            kind = $s.kind; name = $s.name; source = $s.source; status = "active"; platform = "linux"
            path = "$linuxHome/.config/unity3d/IronGate/Valheim"
            created = $s.at; createdBy = $Owner; scenario = (Split-Path $Scenario -Leaf); preExisting = (-not $s.created)
        }
        "--- test save registered: $($s.kind) $($s.name) ($($s.source), linux)"
    }
    $reg.saves = @($reg.saves); $reg.protected = @($reg.protected)
    $reg | ConvertTo-Json -Depth 6 | Set-Content $registryPath -Encoding utf8
}
"--- output in $OutDir"
