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
#   -ValheimDir game folder (default: env VALHEIM_DIR, then the usual Steam folders)
# The lock file and -Mods projects are looked up in the folder that contains ClaudeHeim (e.g. Auga cloned next to it).
param(
    [Parameter(Mandatory = $true)][string]$Scenario,
    [string]$OutDir = "$env:TEMP\claudeheim_run",
    [string[]]$Mods = @(),
    [switch]$Vanilla,
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
    $p = Start-Process (Join-Path $ValheimDir "valheim.exe") -ArgumentList "-screen-fullscreen", "0", "-screen-width", ($Resolution -split "x")[0], "-screen-height", ($Resolution -split "x")[1] -WorkingDirectory $ValheimDir -PassThru
    if (-not $p.WaitForExit($TimeoutSeconds * 1000)) { "TIMEOUT after $TimeoutSeconds s - stopping the test instance"; try { Stop-Process -Id $p.Id -Force } catch {}; Start-Sleep 3 }

    [IO.Directory]::CreateDirectory($OutDir) | Out-Null
    Get-ChildItem $OutDir -File -ErrorAction SilentlyContinue | ForEach-Object { [IO.File]::Delete($_.FullName) }
    Copy-Item (Join-Path $ValheimDir "BepInEx\LogOutput.log") (Join-Path $OutDir "LogOutput.log") -Force
    if (Test-Path $runOut) { Copy-Item (Join-Path $runOut "*") $OutDir -Force }
}
finally {
    $env:CLAUDEHEIM = $null
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
"--- output in $OutDir"
