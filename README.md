# ClaudeHeim

A BepInEx plugin that lets Claude (or anyone) run **scripted, unattended tests of Valheim 1.0** - vanilla or with any set
of mods - and get back screenshots, UI dumps, hover texts, and a machine-readable pass/fail result.

- References the **game only**. It knows nothing about Auga, ValheimCreative or ValheimSurvival; mod-specific hooks are
  reached from the scenario through reflection commands, so the mod under test carries no test code.
- **Dormant** unless the game is started with `CLAUDEHEIM=1`: no Harmony patches, no per-frame work.
- Every command is isolated: a failing command is recorded and the scenario continues.

Written by FNGApex together with Claude (Anthropic's AI coding assistant), hence the name. Unofficial: not affiliated with
Iron Gate or Anthropic.

## Building

Needs Valheim 1.0 with BepInEx 5 installed, and the .NET SDK. The project references the game's own assemblies from your
install; nothing from the game is included in this repo.

```powershell
dotnet build ClaudeHeim.csproj -c Release -p:ValheimDir="D:\SteamLibrary\steamapps\common\Valheim"
```

Without `-p:ValheimDir` it uses the env var `VALHEIM_DIR`, then Steam's default folder. Add `-p:DeployClaudeHeim=true` to copy
the dll into `BepInEx\plugins\ClaudeHeim` (the runner script does that for you and removes it afterwards).

## Running

```powershell
.\Run-ClaudeHeim.ps1 -Scenario stations.chs -OutDir C:\temp\run1                 # with whatever mods are installed
.\Run-ClaudeHeim.ps1 -Scenario auga-tour.chs -Mods Auga -OutDir C:\temp\run2     # deploy Auga for this run only
.\Run-ClaudeHeim.ps1 -Scenario ui-census.chs -Vanilla -OutDir C:\temp\run3       # move all other plugins aside for the run
```

The runner takes a `.game-lock` file in the folder that contains ClaudeHeim, so several people or agents sharing one game
install take turns (it refuses if the game is running or the lock is fresh). It builds + deploys
ClaudeHeim (and `-Mods`), starts the game windowed 1600x900, waits for it to quit (`-TimeoutSeconds`, default 600, then
kills *its own* instance), copies everything to `-OutDir`, removes what it deployed, restores `-Vanilla` moves, releases
the lock. Pass `-Owner <session name>` so the lock says who holds it. `-ValheimDir` picks the game folder (default: env
`VALHEIM_DIR`, then the usual Steam folders). `-Mods Auga` builds `..\Auga\Auga\Auga.csproj`, i.e. the
[Auga 1.0 port](https://github.com/FNGApex/Auga) cloned next to ClaudeHeim.

**Background mode (default)** keeps the run out of the way of the person using the PC:
- **Focus:** the game never keeps focus. The runner and the plugin hand it straight back to whatever had it at launch, and
  the runner reports whether the game ever held it.
- **Window:** a borderless window with no taskbar button. It goes on the monitor named by `-Monitor <hardware id>`, or by
  `"monitor"` in the git-ignored `runner.local.json`, and is matched by id, so switching another screen off doesn't move
  it. List the ids with `Get-CimInstance -Namespace root\wmi WmiMonitorID`. With no monitor set, or the monitor not
  connected, the window goes past the right edge of the desktop.
- **Console, sound, load:** the BepInEx console is off for the run, and sound stays off the whole run, including the intro
  cinematic's VideoPlayer direct audio. The runner checks this with Windows' own peak meter for the game process and reports
  it: silent, or SOUND with the time it started. The game runs at below-normal priority and is capped at 30 fps.
- **Settings:** the game saves its window mode, size and position into the user's own Valheim settings (registry key
  `HKCU\Software\IronGate\Valheim`) when it quits. The runner exports that key before the run and restores it afterwards,
  and restores `BepInEx.cfg` too.
- `-Foreground` gives the old behaviour: a normal window, in-world audio on.

Output folder: `NN_name.png` screenshots (readable by Claude as images), `*.txt` dumps / hover texts, `log.txt`
(scenario log), `result.json` (`commands`, `failures[]`, `errors[]` = every distinct error/exception the game logged,
with count and the command during which it first happened), `LogOutput.log` (full BepInEx log).

Manual use: set `CLAUDEHEIM=1`, `CLAUDEHEIM_SCRIPT=<file>`, optionally `CLAUDEHEIM_OUT=<dir>`, start the game.

Use a throwaway character and world: scenarios give items, place pieces, spawn creatures and kill the player. The included
scenarios use character **RETEP** and single-player world **TestWorld**; edit their `enter` lines to match yours. `enter`
always switches the "open server"/"public" toggles off.

## Test saves

Scenarios can create their own characters and worlds with `newchar` / `newworld`. These are always local saves
(`characters_local` / `worlds_local`), so Steam Cloud never syncs them. After each run, `Run-ClaudeHeim.ps1` records
every save the run created in `TEST_SAVES.json` in the folder that contains ClaudeHeim, next to `.game-lock`. That
file is machine-specific and not in git. `Manage-TestSaves.ps1` lists that registry and archives, wipes or restores
saves from it:
- It only touches local saves in the registry.
- Entries marked protected (`-Protect`) are never touched.
- Entries can be `"platform": "linux"` saves in WSL.
- It refuses while the game on that platform is running.

## Scenario files (`scenarios/*.chs`)

One command per line, `#` comments, `"quotes"` keep spaces. The scenario starts at the main menu.

| Command | What it does |
|---|---|
| `enter <character> [world] [nowait]` | main menu -> single-player world (default world: first containing "test"); waits for the player, or with `nowait` returns during the loading screen |
| `terrain info\|height <pt>` | terrain op prefabs + heightmap resolution / ground height, biome and water level at a point. Points: `x z`, `@mark`, `@player`, `@name+dx,dz` |
| `terrain pad <name> <pt> <size> [h]`, `level <pt> <r> [h]`, `raise\|lower <pt> <r> <d>`, `slope <pt1> <h1> <pt2> <h2> <width>`, `smooth <pt> <r>`, `water <pt> <r> <depth>`, `paint <pt> <r> dirt\|cultivated\|paved\|reset`, `reset <pt> <r>` | exact terrain edits (per-vertex, like the hoe's own data; base +-8 m limit, refused vertices are counted). Heights: absolute, or `+1.5` / `-2` relative to the point. `pad` also clears vegetation and records a mark; a pad already in place is skipped |
| `terrain clear <pt> <r>` / `terrain nuke <pt> <r>` | remove vegetation and loose objects / EVERY networked object except players (and reset the terrain edits there) |
| `terrain megaflatten <name> <pt> <radius> [h\|auto]` | a nuke over a large circle (up to 300 m; everything but players) and every vertex set to one exact height, zone by zone (unloaded zones are visited). Vertices beyond the +-8 m limit and in no-build locations are counted, not edited. `auto` = mid-relief when it fits the limit (nothing refused), else the median. Records a mark |
| `findflatland <radius> [min] [max] [mark] [biome]` | the dry spot (default Meadows) whose generated relief inside the radius is smallest; stops at the first that fits the +-8 m limit, so `megaflatten ... auto` refuses nothing there |
| `floor <name> <pt> <size> [piece]`, `floor clear <name>` | a size x size grid of floor tiles (default `stone_floor_2x2`) laid on flat ground, silently (no placement effects), each resting 5 cm into the ground; not removed by `despawn`. Mark `<name>` = the floor's top; `place`/`spawn @<name>+dx,dz` land on the tiles |
| `terrain snapshot <name> <pt> <r>` / `terrain restore <name>` | save / restore the terrain data of the zones in an area, exactly |
| `mark <name> [pt]`, `marks`, `findbiome <Biome> [min] [max] [mark]`, `findshore [min] [max] [mark]`, `teleport @mark` | named spots per world (BepInEx/ClaudeHeim/marks), nearest dry spot of a biome, nearest beach with water 3+ m deep 12 m out (marks `<mark>` and `<mark>_sea`) |
| `expect height <pt> [h] [tol]`, `expect biome <pt> <Biome>`, `expectfail <command...>` | terrain checks; a negative test that passes when the command is refused |

| `learn all` | the player knows every material and crafting station level, so every recipe and piece is available (test characters) |
| `seedprobe <prefix> <count> [radius]` / `seedprobe seeds <s1> ...` | main menu: rank candidate world seeds by the biomes around the world centre (no world is created); writes `seedprobe.txt` |

**Lab world + golden copy:** `scenarios/lab-setup.chs` builds the ClaudeLab test lab once (seed CLAUDELA16, character
LABTEST, pads and marks 100 m from spawn). `Manage-TestSaves.ps1 -SaveGolden -Name ClaudeLab -Character labtest` stores
the world folder, the character and the marks in `TestSavesGolden\ClaudeLab`; `Run-ClaudeHeim.ps1 -Golden
ClaudeLab:labtest` puts them back before the run (game closed, under the lock, registered local saves only), so every
run starts on identical ground, objects and character. `scenarios/lab-check.chs` verifies it. `scenarios/lab-flat.chs` (run once, then save only the world + marks) added the megaflattened site `flatland` and the 24 m stone floor `flatfloor` where the building scenarios place their pieces.

**Terrain edits only run in registered local test worlds:** the runner passes `CLAUDEHEIM_TERRAIN_WORLDS` (the active,
unprotected, local worlds in `TEST_SAVES.json`) and every editing command refuses any other world, and any cloud save.
Wards and no-build locations (e.g. the spawn stones) are refused like the hoe refuses them.

| `selectworld <name>` | select a world in the open start-game list by name |
| `newchar <name>`, `newworld <name> [seed]` | create a test character (through the game's New Character flow) / world as a **local** save (never Steam Cloud); an existing local one is reused, a cloud one is refused. Each is written to `testsaves.jsonl`, and the runner adds it to the registry, see [Test saves](#test-saves) |
| `wait <s>` / `waitfor player\|mainmenu\|<member path> [timeout]` | pause / poll until non-null or true |
| `shot <name>` | screenshot after 1 s settle |
| `log <text>` | line in log.txt |
| `quit` | write results, quit the game |
| `audio on\|off` | ClaudeHeim runs skip the publisher logos and hold the game silent until the local player exists (in-world audio is untouched). `audio on` lifts that early for main-menu tests; env `CLAUDEHEIM_MENU_AUDIO=1` disables the muting |
| `ui inventory\|menu\|settings\|build open\|close`, `ui map large\|small`, `ui crafttab\|upgradetab\|skills\|texts\|trophies` | vanilla screens through the same methods the game's buttons call |
| `ui settingstab <n>`, `ui buildtab <n>`, `tabs <member path> <shotprefix>` | one tab / screenshot every tab of a `TabHandler` |
| `ui textinput open "<topic>"`, `ui text rune\|intro\|raven "<topic>" "<body>"`, `ui text close` | text dialogs |
| `ui store open <ref>`, `ui container open <ref>` | trader / chest UI for a spawned or placed object |
| `give <item> <n>`, `equip <item>`, `unequip` | inventory (give tops up to n). **While a build tool is equipped the game hovers nothing.** |
| `eat <item> [ok\|fail]`, `foods`, `clearfood` | eat through `Player.ConsumeItem` (gives one first); `fail` asserts the game refuses (same food again, 3 foods). `foods` logs name / time left / hp / stamina / eitr |
| `regen <hp/s> <s>` | heal a little every frame, the way regen mods do, then fail if a visible smooth-filling `GuiBar` stayed frozen while health rose |
| `split <item> [open\|close]` | open the split-stack dialog for a stack (needs 2+), or close it; logs which SplitDialog object is live |
| `fill <ref> <item> <n> [quality]`, `quality <item> <n>` | container stacks with an upgrade level; upgrade level of a stack in the player inventory |
| `recipe <item>` | select a recipe in the open crafting panel (prefab or localized name) |
| `tame [radius]` | Tameable.TameAllInArea around the player |
| `teleport <x> <z> [timeout]` | Player.TeleportTo (distant teleport: waits for the zone, lands on the ground) |
| `tombstones list\|clear [radius]` | the local player's own tombstones near the player (world cleanup after death tests) |
| `die`, `respawn [timeout]` | die like the console `die` command (tombstone, death pin, foods cleared, 10 s respawn timer), then wait for the new player. `respawn` hands the snapshotted items back and removes the run's tombstone; the 5% skill loss of a normal death still applies |
| `spawn <prefab> <dist>\|<@point> [as <ref>]` | Instantiate a networked prefab in front of the player, or exactly at a point facing the player (a mark keeps its height, e.g. `@harbor_sea` = the sea surface) |
| `place <piece> <dist>\|<@point> [as <ref>]` | `Player.PlacePiece` - the hammer's own call (placed-by-player flag, effects, unlock messages), no resource cost. `<dist>` = in front of the player; `@mark+dx,dz` = exactly there, facing the player, on top of whatever is there (a floor tile), never below the point |
| `fill <ref> <item> <n>` | put items in a container |
| `despawn` | remove everything spawned/placed by this scenario |
| `goto <ref> [child] <dist>` | stand that far from the object, on the side of the named child (e.g. `add_ore`), aim at it |
| `lookat <ref> [child]` | aim the camera (the hover ray starts at the camera); tries the object's colliders until the hover lands on it with hover text (portal rings, windmills) |
| `moveto <ref> [child] [dy]` | put the player exactly on an object, dy m above it (default 0.5), no snap to the ground - e.g. onto a ship's deck |
| `nomobs on [radius]\|off` | while on (default 60 m), every second removes wild AI creatures near the player; never players, tamed creatures or anything the scenario spawned/placed |
| `achievementpopup [id\|name]` | show the achievement unlock popup only; nothing is unlocked (the game's own AchievementEvent unlocks on Steam) |
| `hover [file]` | log the hovered object + hover text (optionally save the raw text) |
| `use` / `interact <ref> [child]` | `Player.Interact` on the hovered object / `Interactable.Interact` directly |
| `effect add\|remove <name>`, `effect list` | status effects |
| `message center\|topleft "<text>"`, `npctext "<topic>" "<text>"` | HUD messages, speech bubble |
| `console <command...>` | run a console command |
| `set <Type.member.path> <value>` / `get <path>` | static-rooted reflection, e.g. `set AugaUnity.AugaHealthBar.DebugAdrenalineOverride 65`, `get InventoryGui.instance.m_craftingStationName.text` |
| `setc <ref> <Component> <field> <value>` | field on a component of a spawned/placed object |
| `call <Type.path.Method> [args]` | static method, or instance method at the end of a static path. Colours / vectors: `r,g,b[,a]`, `x,y[,z]`. An argument written `@Type.path` passes the object at that static path (e.g. `call Hud.instance.m_radialMenu.Open @Hud.instance.m_config null`) |
| `loadasm <path>` | load an extra assembly (e.g. a mod API stub); its types are reachable as `asm:<AssemblyName>\|<Type>`. A relative path is relative to the scenario file |
| `invoke <ComponentType>[@pathSuffix] <Method> [args]` | method on the first live component of that type (inactive included) |
| `click <uiNameOrPath> [left\|middle\|right] [x y]` / `hoverui <uiNameOrPath>` | uGUI pointer events on an active UI object (buttons, tooltips); optional button and screen position |
| `hoveritem <prefab>` | with the inventory open, hover the slot holding that item so its tooltip shows (moves only the Input System mouse state, never the OS cursor) |
| `finditems <filter> [max]` | list item prefabs: `type:<ItemType>`, `set`, `mods` (1.0 equipment modifiers), `se` (status effect), or a name substring |
| `type "<text>"` | text into the focused input field (TextInput's or the selected TMP_InputField), truncated at characterLimit like typing |
| `move <item> player\|container [n]`, `remove <item> <n>`, `clearinv` | move a stack between the player inventory and the open container; remove n; empty the player inventory |
| `last`, `@last` | the previous `call` / `invoke` result: `get last.m_checked`, `set last.m_checked true`, or `@last` as an argument. `call` also fills a method's optional parameters with their defaults |
| `dump ui\|<ref>\|<member path>\|comp:<Type>[@suffix]\|ui:<nameOrPath> <file>` | hierarchy dump: components, sizes, sprites, fonts, texts. `dump ui` = every root canvas |
| `describe <same targets>` | why-can't-I-see-it: active flags, alpha, canvas order, rect/anchors up the parent chain |
| `expect noerrors` / `expect hover contains "<t>"` / `expect value <path> contains "<t>"` / `expect active\|inactive <path or ui:<nameOrPath>>` / `expect ui <name>` | assertions -> `failures[]` |

Type names resolve by full name first, then by simple name across all loaded assemblies; private members are reachable.

## Scenarios included

General (vanilla or any mod set):
- `stations.chs` - place workbench / forge / chest / smelter / kiln, open station UIs, hover the input switches.
- `texts-enemies.chs` - text viewers, messages and the enemy hud; run vanilla and with mods and compare the shots.
- `food-respawn.chs` - eat three foods (HUD food panels, refusal rules), die, respawn, check the HUD comes back with the new player.
- `hp-regen.chs` - per-frame healing (regen mods): the health bar fill must keep moving.
- `ui-census.chs` - dump every UI canvas; run with `-Vanilla` and with mods, then diff.
- `testsaves.chs` - create (or reuse) the local test character AUGATEST and world AugaTest, and enter for the first spawn.
- `s5-*.chs` - UI behaviour flows: split dialog bounds (04), portal tag (05), container cycling (07), map pins (09),
  hammer/hoe build menus (11), trader coins (13), GUI scale 60/100/150% (43).

Auga port (run with `-Mods Auga`):
- `auga-tour.chs` - every screen the Auga port touches.
- `mainmenu.chs` - the Augafied main menu through FejdStartup's handlers, then `enter`.
- `augafy.chs` - Auga hover rows, Auga split dialog, build-piece readout.
- `audit3.chs` - regression for the audit fixes (radial hints, rune close hint, messages, pause menu, settings tabs, map, store).
- `auga-api.chs` - the AugaAPI stub a consumer mod carries, calling into the real Auga.
- `issues.chs` - reproductions for the upstream Auga issues still plausible on 1.0 (`ISSUES_1.0.md` in the Auga port).
- `issue-62-inventory-rows.chs` - #62/#228: extra inventory rows vs the container panel (a fixed bug).

One-off scenarios whose results are recorded elsewhere were removed on 2026-09-22. They are still in git history, in
commit `ae46ad1`.

## Adding a command

`Commands.cs` -> `Dispatch`. Return `null` for an instant command or an `IEnumerator` for one that spans frames; throw to
fail. Nested enumerators and `WaitForSecondsRealtime` are stepped by the runner itself so exceptions never kill the run.

## License

MIT, see [LICENSE](LICENSE). Valheim, BepInEx and Harmony are not part of this repo; they keep their own terms.
