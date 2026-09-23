# ClaudeHeim test saves: the characters and worlds scenarios create for themselves (newchar / newworld).
# They are LOCAL saves only (characters_local / worlds_local, never Steam Cloud), and every one is tracked in the
# registry TEST_SAVES.json in the folder that contains ClaudeHeim (next to .game-lock; machine-specific, not in git).
# Run-ClaudeHeim.ps1 adds new ones to the registry after each run. At the end of a coding session the user decides
# whether they are archived (moved out of the game's save folder, restorable) or wiped (sent to the Recycle Bin).
#
#   -List                         registry with status and whether the files are on disk (default action)
#   -Archive  [-Name a,b | -All]  move the save files to TestSavesArchive\<timestamp>\ next to the registry
#   -Wipe     [-Name a,b | -All]  send the save files to the Recycle Bin
#   -Restore  -Name a,b           move archived saves back into the game's save folder
#   -Protect  -Name a -Kind character|world -Source cloud|local -Note "..."   record a save that must never be touched
#   -SaveGolden -Name <world> -Character <name>   store the lab world + its test character + marks as the golden copy
#              (TestSavesGolden\<world>\ next to the registry); Run-ClaudeHeim.ps1 -Golden <world>:<character> restores it
# Only registry entries with source Local are ever moved or deleted; protected entries never are (matched by kind,
# name AND platform). Entries carry "platform": windows (default) or linux. Linux saves are reached through
# \\wsl.localhost\<distro> from the entry's "path" (default: the WSL user's ~/.config/unity3d/IronGate/Valheim);
# a Linux wipe deletes for good (no Recycle Bin on WSL paths). Refuses while a game on that platform is running.
param(
    [switch]$List,
    [switch]$Archive,
    [switch]$Wipe,
    [switch]$Restore,
    [switch]$Protect,
    [switch]$SaveGolden,
    [string]$Character = "",
    [string]$ValheimDir = "D:\SteamLibrary\steamapps\common\Valheim",
    [string[]]$Name = @(),
    [switch]$All,
    [string]$Kind = "",
    [string]$Source = "",
    [string]$Note = ""
)

$root = Split-Path $PSScriptRoot -Parent
$registryPath = Join-Path $root "TEST_SAVES.json"
$archiveRoot = Join-Path $root "TestSavesArchive"
$windowsSaveRoot = Join-Path $env:USERPROFILE "AppData\LocalLow\IronGate\Valheim"
$wslDistro = "Ubuntu"
$linuxDefaultRoot = $null   # resolved from the WSL user's $HOME when a Linux entry needs it

function Get-Platform($entry) { if ($entry.platform) { return $entry.platform.ToLowerInvariant() } return "windows" }

# The game's save folder for this entry, as a path Windows can open.
function Get-SaveRoot($entry) {
    if ((Get-Platform $entry) -ne "linux") { return $windowsSaveRoot }
    if (-not $script:linuxDefaultRoot) { $script:linuxDefaultRoot = ((wsl.exe -d $wslDistro -- sh -c 'echo $HOME') -join "").Trim() + "/.config/unity3d/IronGate/Valheim" }
    $linux = $script:linuxDefaultRoot
    if ($entry.path -and $entry.path -match '^(.*?)/(characters_local|worlds_local)(/|$)') { $linux = $Matches[1] }
    return "\\wsl.localhost\$wslDistro" + $linux.Replace('/', '\')
}

function Test-Protected($reg, $entry) {
    $platform = Get-Platform $entry
    return [bool]($reg.protected | Where-Object { $_.name -eq $entry.name -and $_.kind -eq $entry.kind -and (Get-Platform $_) -eq $platform })
}

function Read-Registry {
    if (Test-Path $registryPath) { return Get-Content $registryPath -Raw | ConvertFrom-Json }
    return [pscustomobject]@{ saves = @(); protected = @() }
}

function Write-Registry($reg) {
    $reg.saves = @($reg.saves)
    $reg.protected = @($reg.protected)
    $reg | ConvertTo-Json -Depth 6 | Set-Content $registryPath -Encoding utf8
}

# Every file and folder that belongs to one local save: the save itself, its .old / backup copies. A 1.0 world is one
# folder (chunks, database, minimap cache).
function Get-SaveItems($entry) {
    if ($entry.kind -eq "character") {
        $dir = Join-Path (Get-SaveRoot $entry) "characters_local"
        $stem = $entry.name.ToLowerInvariant()
    } else {
        $dir = Join-Path (Get-SaveRoot $entry) "worlds_local"
        $stem = $entry.name
    }
    if (-not (Test-Path $dir)) { return @() }
    return @(Get-ChildItem $dir -Force | Where-Object {
        $_.Name -eq $stem -or $_.Name -like "$stem.*" -or $_.Name -like "${stem}_backup_*"
    })
}

function Select-Entries($reg) {
    $active = @($reg.saves | Where-Object { $_.status -eq "active" -and $_.source -eq "Local" })
    if ($All) { return $active }
    if (-not $Name) { throw "pass -Name <name>[,<name>] or -All" }
    $picked = @($active | Where-Object { $Name -contains $_.name })
    $missing = @($Name | Where-Object { $n = $_; -not ($picked | Where-Object { $_.name -eq $n }) })
    if ($missing) { throw "not an active local test save in the registry: $($missing -join ', ')" }
    return $picked
}

$reg = Read-Registry
if (-not ($Archive -or $Wipe -or $Restore -or $Protect -or $SaveGolden)) { $List = $true }

$windowsGameRunning = [bool](Get-Process valheim -ErrorAction SilentlyContinue)
$linuxGameRunning = $false
if ($Archive -or $Wipe -or $Restore) {
    try { $linuxGameRunning = [bool]((wsl.exe -d $wslDistro -- pgrep -f valheim.x86_64 2>$null) -join "") } catch {}
}
function Assert-NotRunning($entry) {
    if ((Get-Platform $entry) -eq "linux" -and $linuxGameRunning) { throw "the Linux (WSL) game is running - close it first" }
    if ((Get-Platform $entry) -ne "linux" -and $windowsGameRunning) { throw "Valheim is running - close it first (save files may be open)" }
}

if ($SaveGolden) {
    if ($Name.Count -ne 1 -or -not $Character) { "-SaveGolden needs -Name <world> -Character <name>"; exit 1 }
    if (Get-Process valheim -ErrorAction SilentlyContinue) { "Valheim is running - close it first (save files may be open)"; exit 2 }
    $worldName = $Name[0]; $charName = $Character.ToLowerInvariant()
    $w = $reg.saves | Where-Object { $_.kind -eq "world" -and $_.name -eq $worldName -and $_.status -eq "active" -and $_.source -eq "Local" -and (Get-Platform $_) -eq "windows" }
    $c = $reg.saves | Where-Object { $_.kind -eq "character" -and $_.name -eq $charName -and $_.status -eq "active" -and $_.source -eq "Local" -and (Get-Platform $_) -eq "windows" }
    if (-not $w -or -not $c) { "both must be active local Windows test saves in the registry: world $worldName, character $charName"; exit 1 }
    $dest = Join-Path $root "TestSavesGolden\$worldName"
    if (Test-Path $dest) { Remove-Item $dest -Recurse -Force }
    [IO.Directory]::CreateDirectory("$dest\world") | Out-Null
    [IO.Directory]::CreateDirectory("$dest\character") | Out-Null
    Copy-Item (Join-Path $windowsSaveRoot "worlds_local\$worldName") "$dest\world\" -Recurse
    Get-ChildItem (Join-Path $windowsSaveRoot "characters_local") -File | Where-Object { $_.Name -eq "$charName.fch" -or $_.Name -eq "$charName.png" } | Copy-Item -Destination "$dest\character\"
    $marks = Join-Path $ValheimDir "BepInEx\ClaudeHeim\marks\$worldName.txt"
    if (Test-Path $marks) { Copy-Item $marks "$dest\marks.txt" }
    $stamp = Get-Date -Format s
    [pscustomobject]@{ world = $worldName; character = $charName; saved = $stamp } | ConvertTo-Json | Set-Content "$dest\golden.json" -Encoding utf8
    $w | Add-Member -Force golden $dest
    $w | Add-Member -Force goldenSaved $stamp
    $w | Add-Member -Force goldenCharacter $charName
    Write-Registry $reg
    "golden copy saved: $dest (world folder, $charName.fch, marks: $(Test-Path "$dest\marks.txt"))"
}

if ($Protect) {
    if (-not $Name -or -not $Kind -or -not $Source) { "-Protect needs -Name, -Kind and -Source"; exit 1 }
    foreach ($n in $Name) {
        $reg.protected = @($reg.protected | Where-Object { -not ($_.name -eq $n -and $_.kind -eq $Kind) }) +
            [pscustomobject]@{ kind = $Kind; name = $n; source = $Source; note = $Note }
    }
    Write-Registry $reg
    "protected: $($Name -join ', ')"
}

if ($Archive -or $Wipe) {
    $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
    Add-Type -AssemblyName Microsoft.VisualBasic
    foreach ($entry in (Select-Entries $reg)) {
        if (Test-Protected $reg $entry) { "skip protected $($entry.kind) $($entry.name) ($(Get-Platform $entry))"; continue }
        Assert-NotRunning $entry
        $items = Get-SaveItems $entry
        if ($Archive) {
            $dest = Join-Path $archiveRoot "$stamp\$(Get-Platform $entry)\$($entry.kind)s"
            [IO.Directory]::CreateDirectory($dest) | Out-Null
            foreach ($i in $items) { Move-Item $i.FullName (Join-Path $dest $i.Name) }
            $entry | Add-Member -Force status "archived"
            $entry | Add-Member -Force archivedTo $dest
            "archived $($entry.kind) $($entry.name): $($items.Count) item(s) -> $dest"
        } else {
            foreach ($i in $items) {
                if ((Get-Platform $entry) -eq "linux") { Remove-Item $i.FullName -Recurse -Force; continue }
                if ($i.PSIsContainer) { [Microsoft.VisualBasic.FileIO.FileSystem]::DeleteDirectory($i.FullName, 'OnlyErrorDialogs', 'SendToRecycleBin') }
                else { [Microsoft.VisualBasic.FileIO.FileSystem]::DeleteFile($i.FullName, 'OnlyErrorDialogs', 'SendToRecycleBin') }
            }
            $entry | Add-Member -Force status "wiped"
            "wiped $($entry.kind) $($entry.name) ($(Get-Platform $entry)): $($items.Count) item(s) " + $(if ((Get-Platform $entry) -eq "linux") { "deleted" } else { "to the Recycle Bin" })
        }
        $entry | Add-Member -Force changed (Get-Date -Format s)
    }
    Write-Registry $reg
}

if ($Restore) {
    if (-not $Name) { "pass -Name"; exit 1 }
    foreach ($entry in @($reg.saves | Where-Object { $Name -contains $_.name -and $_.status -eq "archived" })) {
        Assert-NotRunning $entry
        $dir = Join-Path (Get-SaveRoot $entry) $(if ($entry.kind -eq "character") { "characters_local" } else { "worlds_local" })
        [IO.Directory]::CreateDirectory($dir) | Out-Null
        $stem = if ($entry.kind -eq "character") { $entry.name.ToLowerInvariant() } else { $entry.name }
        $items = @(Get-ChildItem $entry.archivedTo -Force | Where-Object { $_.Name -eq $stem -or $_.Name -like "$stem.*" -or $_.Name -like "${stem}_backup_*" })
        foreach ($i in $items) { Move-Item $i.FullName (Join-Path $dir $i.Name) }
        $entry | Add-Member -Force status "active"
        $entry | Add-Member -Force changed (Get-Date -Format s)
        "restored $($entry.kind) $($entry.name): $($items.Count) item(s)"
    }
    Write-Registry $reg
}

if ($List) {
    "Registry: $registryPath"
    "Protected (never touched):"
    $reg.protected | ForEach-Object { "  {0,-9} {1,-16} {2,-7} {3,-6} {4}" -f $_.kind, $_.name, (Get-Platform $_), $_.source, $_.note }
    "Test saves:"
    $reg.saves | ForEach-Object {
        $onDisk = if ($_.status -eq "active") { (Get-SaveItems $_).Count } else { "-" }
        "  {0,-9} {1,-16} {2,-7} {3,-6} {4,-8} files:{5,-3} by {6} ({7}) {8}" -f $_.kind, $_.name, (Get-Platform $_), $_.source, $_.status, $onDisk, $_.createdBy, $_.created, $_.scenario
    }
}
