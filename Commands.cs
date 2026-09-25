using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ClaudeHeim
{
    internal sealed partial class Runner
    {
        private IEnumerator Dispatch(List<string> a)
        {
            switch (a[0].ToLowerInvariant())
            {
                case "enter": return Enter(Arg(a, 1, "RETEP"), Arg(a, 2), Arg(a, 3, "") == "nowait");
                case "newchar": return NewCharacter(Arg(a, 1));
                case "newworld": return NewWorld(Arg(a, 1), Arg(a, 2));
                case "selectworld": SelectWorld(Arg(a, 1)); return null;
                case "terrain": return Terrain(a);
                case "expectfail": ExpectFail(a.Skip(1).ToList()); return null;
                case "mark": Mark(a); return null;
                case "marks": Info("marks: " + (Marks.Count == 0 ? "none" : string.Join(", ", Marks.Select(m => $"{m.Key} ({m.Value.x:0.#}, {m.Value.y:0.#}, {m.Value.z:0.#})")))); return null;
                case "findbiome": FindBiome(a); return null;
                case "findshore": FindShore(a); return null;
                case "seedprobe": SeedProbe(a); return null;
                case "learn": LearnAll(); return null;
                case "wait": return Wait(F(Arg(a, 1, "1")));
                case "waitfor": return WaitFor(Arg(a, 1), F(Arg(a, 2, "30")));
                case "shot": return Shot(Arg(a, 1, "shot"));
                case "log": Info(string.Join(" ", a.Skip(1))); return null;
                case "ui": return Ui(a);
                case "give": Give(Arg(a, 1), int.Parse(Arg(a, 2, "1"))); return null;
                case "equip": return Equip(Arg(a, 1));
                case "unequip": Player.m_localPlayer.UnequipAllItems(); return Wait(1.5f);
                case "eat": return Eat(Arg(a, 1), Arg(a, 2, "ok"));
                case "foods": Info("foods: " + FoodList()); return null;
                case "clearfood": Player.m_localPlayer.ClearFood(); return null;
                case "die": return Die();
                case "regen": return Regen(F(Arg(a, 1, "5")), F(Arg(a, 2, "3")));
                case "respawn": return Respawn(F(Arg(a, 1, "90")));
                case "split": return SplitDialog(Arg(a, 1), Arg(a, 2, "open"));
                case "tombstones": Tombstones(Arg(a, 1, "list"), F(Arg(a, 2, "30"))); return null;
                case "teleport":
                    if ((Arg(a, 1) ?? "").StartsWith("@"))
                    {
                        var ti = 1;
                        var target = Point(a, ref ti);
                        return Teleport(target.x, target.z, F(Arg(a, ti, "60")));
                    }

                    return Teleport(F(Arg(a, 1)), F(Arg(a, 2)), F(Arg(a, 3, "60")));
                case "spawn":
                    if ((Arg(a, 2) ?? "").StartsWith("@"))
                    {
                        var si = 2;
                        Spawn(Arg(a, 1), 0f, RefName(a), Point(a, ref si));
                        return null;
                    }

                    Spawn(Arg(a, 1), F(Arg(a, 2, "3")), RefName(a));
                    return null;
                case "place":
                    if ((Arg(a, 2) ?? "").StartsWith("@"))
                    {
                        var pi = 2;
                        return Place(Arg(a, 1), Point(a, ref pi), 0f, RefName(a));
                    }

                    return Place(Arg(a, 1), null, F(Arg(a, 2, "3")), RefName(a));
                case "despawn": Despawn(); return null;
                case "fill": Fill(Arg(a, 1), Arg(a, 2), int.Parse(Arg(a, 3, "1")), int.Parse(Arg(a, 4, "1"))); return null;
                case "quality": Quality(Arg(a, 1), int.Parse(Arg(a, 2, "1"))); return null;
                case "recipe": return Recipe(Arg(a, 1));
                case "tame": Tameable.TameAllInArea(Player.m_localPlayer.transform.position, F(Arg(a, 1, "20"))); Info("tame: all tameables within " + Arg(a, 1, "20") + " m"); return null;
                case "npctext": Chat.instance.SetNpcText(Player.m_localPlayer.gameObject, Vector3.up * 2f, 20f, 5f, Arg(a, 1, "Topic"), Arg(a, 2, "Text"), false); return null;
                case "tabs": return TabTour(Arg(a, 1), Arg(a, 2, "tab"));
                case "goto": return GoTo(a);
                case "moveto": return MoveTo(a);
                case "floor": Floor(a); return null;
                case "findflatland": FindFlatLand(a); return null;
                case "nomobs": NoMobs(Arg(a, 1, "on"), Arg(a, 2)); return null;
                case "achievementpopup": AchievementPopup(Arg(a, 1)); return null;
                case "lookat": return LookAt(Arg(a, 1), Arg(a, 2));
                case "hover": Hover(a.Skip(1).ToList()); return null;
                case "use": Use(); return null;
                case "interact": Interact(Arg(a, 1), Arg(a, 2)); return null;
                case "console": Console.instance.TryRunCommand(string.Join(" ", a.Skip(1)), false, true); return null;
                case "effect": Effect(Arg(a, 1), Arg(a, 2)); return null;
                case "message": Player.m_localPlayer.Message(Arg(a, 1) == "center" ? MessageHud.MessageType.Center : MessageHud.MessageType.TopLeft, Arg(a, 2, "ClaudeHeim")); return null;
                case "set": ReflectSet(Arg(a, 1), Arg(a, 2)); return null;
                case "setc": SetOnRef(Arg(a, 1), Arg(a, 2), Arg(a, 3), Arg(a, 4)); return null;
                case "loadasm": Info("loaded " + Assembly.LoadFrom(ScenarioRelative(Arg(a, 1))).FullName); return null;
                case "get": Info(Arg(a, 1) + " = " + Describe(ReflectGet(Arg(a, 1)))); return null;
                case "call": _last = ReflectCallStatic(Arg(a, 1), a.Skip(2).ToList()); Info("-> " + Describe(_last)); return null;
                case "invoke": _last = InvokeOnComponent(Arg(a, 1), Arg(a, 2), a.Skip(3).ToList()); Info("-> " + Describe(_last)); return null;
                case "type": TypeText(Arg(a, 1, "")); return null;
                case "move": Move(Arg(a, 1), Arg(a, 2, "container"), int.Parse(Arg(a, 3, "0"))); return null;
                case "remove": RemoveItems(Arg(a, 1), int.Parse(Arg(a, 2, "1"))); return null;
                case "clearinv": Player.m_localPlayer.GetInventory().RemoveAll(); Info("clearinv: player inventory emptied"); return null;
                case "click": UiPointer(Arg(a, 1), true, Arg(a, 2, "left"), a.Count > 4 ? F(Arg(a, 3)) : (float?)null, a.Count > 4 ? F(Arg(a, 4)) : (float?)null); return null;
                case "hoverui": UiPointer(Arg(a, 1), false); return null;
                case "hoveritem": return HoverItem(Arg(a, 1));
                case "finditems": FindItems(Arg(a, 1), int.Parse(Arg(a, 2, "40"))); return null;
                case "dump": Dump(Arg(a, 1), Arg(a, 2)); return null;
                case "describe": DescribeObject(Arg(a, 1)); return null;
                case "expect": Expect(a); return null;
                case "audio":
                    if (Arg(a, 1) == "list") { AudioSources(); return null; }
                    Plugin.MuteAudio = Arg(a, 1) == "off";
                    if (!Plugin.MuteAudio) Plugin.Unmute();
                    return null;
                case "quit": _quitRequested = true; return null;
                default: throw new Exception("unknown command '" + a[0] + "'");
            }
        }

        /// <summary>audio list: every AudioSource that is playing right now, and whether anything could make it audible.</summary>
        private void AudioSources()
        {
            Info($"listener volume={AudioListener.volume} pause={AudioListener.pause}");
            foreach (var s in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(s => s.isPlaying))
            {
                Info($"  playing: {PathOf(s.transform)} clip={(s.clip != null ? s.clip.name : "-")} vol={s.volume:0.##} mute={s.mute} ignoreListenerVolume={s.ignoreListenerVolume} mixer={(s.outputAudioMixerGroup != null ? s.outputAudioMixerGroup.name : "-")}");
            }
        }

        private static string RefName(List<string> a)
        {
            var i = a.FindIndex(x => x.Equals("as", StringComparison.OrdinalIgnoreCase));
            return i >= 0 && i + 1 < a.Count ? a[i + 1] : null;
        }

        // ------------------------------------------------------------------ flow

        private IEnumerator Wait(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
        }

        /// <summary>waitfor player | waitfor mainmenu | waitfor &lt;Type.StaticMember&gt; (non-null / true)</summary>
        private IEnumerator WaitFor(string what, float timeout)
        {
            var waited = 0f;
            while (waited < timeout)
            {
                bool ok;
                switch ((what ?? "").ToLowerInvariant())
                {
                    case "player": ok = Player.m_localPlayer != null && Hud.instance != null; break;
                    case "mainmenu": ok = FejdStartup.instance != null; break;
                    default:
                        var value = ReflectGet(what);
                        ok = value is bool b ? b : value != null && !(value is UnityEngine.Object o && o == null);
                        break;
                }

                if (ok)
                {
                    yield break;
                }

                waited += 0.5f;
                yield return new WaitForSecondsRealtime(0.5f);
            }

            throw new Exception($"timed out after {timeout}s waiting for {what}");
        }

        private IEnumerator Shot(string name)
        {
            // Let layout, fades and hover text settle; capture happens at the end of the next frame.
            yield return new WaitForSecondsRealtime(1.0f);
            var file = $"{++_shot:00}_{Sanitize(name)}.png";
            ScreenCapture.CaptureScreenshot(Path.Combine(_outDir, file));
            yield return new WaitForSecondsRealtime(0.4f);
            Info("shot " + file);
        }

        private static string Sanitize(string s) => new string((s ?? "x").Select(c => char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_').ToArray());

        /// <summary>From the main menu: pick a character by display name, a world by name (default: first whose name contains "test"), start single player.</summary>
        private IEnumerator Enter(string characterName, string worldName, bool noWait = false)
        {
            var fejd = FejdStartup.instance;
            if (fejd == null)
            {
                throw new Exception("not in the main menu");
            }

            var profiles = SaveSystem.GetAllPlayerProfiles();
            var profile = profiles.FirstOrDefault(p => string.Equals(p.GetName(), characterName, StringComparison.OrdinalIgnoreCase));
            if (profile == null)
            {
                throw new Exception($"character '{characterName}' not found (have: {string.Join(", ", profiles.Select(p => p.GetName()))})");
            }

            fejd.OnStartGame();
            fejd.SetSelectedProfile(profile.GetFilename());
            yield return new WaitForSecondsRealtime(1f);
            fejd.OnCharacterStart();
            yield return new WaitForSecondsRealtime(2f);

            var worlds = fejd.m_worlds ?? new List<World>();
            var index = worldName != null
                ? worlds.FindIndex(w => string.Equals(w.m_name, worldName, StringComparison.OrdinalIgnoreCase))
                : worlds.FindIndex(w => w.m_name != null && w.m_name.ToLowerInvariant().Contains("test"));
            if (index < 0)
            {
                throw new Exception($"world '{worldName ?? "*test*"}' not found (have: {string.Join(", ", worlds.Select(w => w.m_name))})");
            }

            Info($"entering world '{worlds[index].m_name}' ({worlds[index].m_fileSource}) as '{profile.GetName()}' ({profile.m_fileSource})");
            fejd.SetSelectedWorld(index, false);
            // Single player only: never open or list the session.
            fejd.m_openServerToggle.isOn = false;
            fejd.m_publicServerToggle.isOn = false;
            fejd.OnWorldStart();
            if (noWait)
            {
                // Loading-screen tests: return while the world is still loading (follow with waitfor player).
                yield return new WaitForSecondsRealtime(1f);
                Info("world start requested (nowait)");
                yield break;
            }

            var waited = 0f;
            while ((Player.m_localPlayer == null || Hud.instance == null || InventoryGui.instance == null) && waited < 240f)
            {
                waited += 1f;
                yield return new WaitForSecondsRealtime(1f);
            }

            if (Player.m_localPlayer == null)
            {
                throw new Exception("no local player after 240 s");
            }

            // Spawn animation, loading fade, first zone load.
            yield return new WaitForSecondsRealtime(12f);
            Info("in world");
        }

        // ------------------------------------------------------------------ vanilla UI

        private IEnumerator Ui(List<string> a)
        {
            var screen = (Arg(a, 1) ?? "").ToLowerInvariant();
            var verb = (Arg(a, 2) ?? "open").ToLowerInvariant();
            switch (screen)
            {
                case "inventory":
                    if (verb == "close") InventoryGui.instance.Hide(); else InventoryGui.instance.Show(null);
                    break;
                case "crafttab": InventoryGui.instance.OnTabCraftPressed(); break;
                case "upgradetab": InventoryGui.instance.OnTabUpgradePressed(); break;
                case "skills": InventoryGui.instance.OnOpenSkills(); break;
                case "texts": InventoryGui.instance.OnOpenTexts(); break;
                case "trophies": InventoryGui.instance.OnOpenTrophies(); break;
                case "map":
                    Minimap.instance.SetMapMode(verb == "large" || verb == "open" ? Minimap.MapMode.Large : Minimap.MapMode.Small);
                    break;
                case "menu":
                    if (verb == "close") Menu.instance.Hide(); else Menu.instance.Show();
                    break;
                case "settings":
                    if (verb == "close")
                    {
                        if (Settings.instance != null) Settings.instance.OnBack();
                    }
                    else if (Menu.instance != null && Player.m_localPlayer != null)
                    {
                        Menu.instance.Show();
                        Menu.instance.OnSettings();
                    }
                    else
                    {
                        FejdStartup.instance.OnButtonSettings();
                    }

                    break;
                case "settingstab":
                    Settings.instance.m_tabHandler.SetActiveTab(int.Parse(verb));
                    break;
                case "build":
                    if (Hud.IsPieceSelectionVisible() != (verb != "close")) Hud.instance.TogglePieceSelection();
                    break;
                case "buildtab":
                    Hud.instance.m_buildUi.GetComponent<TabHandler>().SetActiveTab(int.Parse(verb));
                    break;
                case "textinput":
                    if (verb == "close") TextInput.instance.Hide(); else TextInput.instance.RequestText(new NullTextReceiver(), Arg(a, 3, "ClaudeHeim"), 30);
                    break;
                case "text":
                    // ui text rune|intro|raven "topic" "body" / ui text close
                    if (verb == "close") TextViewer.instance.Hide();
                    else TextViewer.instance.ShowText((TextViewer.Style)Enum.Parse(typeof(TextViewer.Style), verb, true), Arg(a, 3, "Topic"), Arg(a, 4, "Body"), false);
                    break;
                case "store":
                    if (verb == "close") StoreGui.instance.Hide();
                    else StoreGui.instance.Show(Resolve(Arg(a, 3)).GetComponentInChildren<Trader>());
                    break;
                case "container":
                    InventoryGui.instance.Show(Resolve(Arg(a, 3)).GetComponentInChildren<Container>());
                    break;
                default:
                    throw new Exception("unknown ui screen '" + screen + "'");
            }

            yield return null;
        }

        /// <summary>tabs &lt;member path to a TabHandler or its owner&gt; &lt;shot prefix&gt;: activates every tab in turn and screenshots it.</summary>
        private IEnumerator TabTour(string path, string prefix)
        {
            var value = ReflectGet(path);
            var tabs = value as TabHandler ?? (value as Component)?.GetComponentInChildren<TabHandler>(true) ?? (value as GameObject)?.GetComponentInChildren<TabHandler>(true);
            if (tabs == null)
            {
                throw new Exception("no TabHandler at " + path);
            }

            for (var i = 0; i < tabs.m_tabs.Count; i++)
            {
                tabs.SetActiveTab(i);
                var page = tabs.m_tabs[i].m_page;
                yield return Shot($"{prefix}_{i}_{(page != null ? page.name : "tab")}");
            }
        }

        private sealed class NullTextReceiver : TextReceiver
        {
            public string GetText() => "";

            public void SetText(string text)
            {
            }
        }

        // ------------------------------------------------------------------ world

        private void Give(string prefabName, int amount)
        {
            var prefab = ObjectDB.instance.GetItemPrefab(prefabName);
            if (prefab == null)
            {
                throw new Exception("no item prefab " + prefabName);
            }

            var inventory = Player.m_localPlayer.GetInventory();
            var have = inventory.GetAllItems().Where(i => i.m_dropPrefab != null && i.m_dropPrefab.name == prefabName).Sum(i => i.m_stack);
            if (have < amount)
            {
                inventory.AddItem(prefab, amount - have);
                Info($"gave {amount - have} {prefabName}");
            }
        }

        private IEnumerator Equip(string prefabName)
        {
            var player = Player.m_localPlayer;
            var item = player.GetInventory().GetAllItems().FirstOrDefault(i => i.m_dropPrefab != null && i.m_dropPrefab.name == prefabName);
            if (item == null)
            {
                throw new Exception("not in inventory: " + prefabName);
            }

            if (!player.IsItemEquiped(item))
            {
                player.EquipItem(item);
            }

            yield return new WaitForSecondsRealtime(2.5f);
        }

        private Vector3 InFront(float distance)
        {
            var player = Player.m_localPlayer;
            var position = player.transform.position + player.transform.forward * distance;
            if (ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(position, out var height))
            {
                position.y = height;
            }

            return position;
        }

        /// <summary>spawn &lt;prefab&gt; &lt;dist&gt;|&lt;@point&gt;: in front of the player, or at an exact point (a mark keeps its height, e.g. the sea surface).</summary>
        private void Spawn(string prefabName, float distance, string refName, Vector3? at = null)
        {
            var prefab = ZNetScene.instance.GetPrefab(prefabName);
            if (prefab == null)
            {
                throw new Exception("no prefab " + prefabName);
            }

            var player = Player.m_localPlayer;
            var position = at ?? InFront(distance);
            if (at.HasValue)
            {
                // on a floor tile if there is one; a sea mark keeps its water-surface height (the ray would hit the seabed)
                position.y = Mathf.Max(at.Value.y, SurfaceHeight(at.Value.x, at.Value.z, at.Value.y) ?? at.Value.y);
            }

            var toPlayer = player.transform.position - position;
            toPlayer.y = 0f;
            var rotation = at.HasValue && toPlayer.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toPlayer) : Quaternion.LookRotation(-player.transform.forward);
            var go = UnityEngine.Object.Instantiate(prefab, position, rotation);
            _created.Add(go);
            _refs[refName ?? prefabName] = go;
        }

        /// <summary>Places a build piece through Player.PlacePiece - the same call the hammer makes - without charging resources.
        /// place &lt;piece&gt; &lt;dist&gt;|&lt;@point&gt;: in front of the player, or at an exact point (a lab mark) facing the player.</summary>
        private IEnumerator Place(string pieceName, Vector3? at, float distance, string refName)
        {
            var player = Player.m_localPlayer;
            var prefab = ZNetScene.instance.GetPrefab(pieceName);
            var piece = prefab != null ? prefab.GetComponent<Piece>() : null;
            if (piece == null)
            {
                throw new Exception("no build piece " + pieceName);
            }

            var position = at ?? InFront(distance);
            if (at.HasValue)
            {
                // on top of whatever is there (a floor tile), never below the point
                position.y = Mathf.Max(at.Value.y, SurfaceHeight(at.Value.x, at.Value.z, at.Value.y) ?? at.Value.y);
            }

            var toPlayer = player.transform.position - position;
            toPlayer.y = 0f;
            var rotation = at.HasValue && toPlayer.sqrMagnitude > 0.01f
                ? Quaternion.LookRotation(toPlayer)
                : Quaternion.LookRotation(-player.transform.forward);
            var before = new HashSet<Piece>(UnityEngine.Object.FindObjectsByType<Piece>(FindObjectsSortMode.None));
            var noCost = player.m_noPlacementCost;
            player.m_noPlacementCost = true;
            try
            {
                player.PlacePiece(piece, position, rotation, false);
            }
            finally
            {
                player.m_noPlacementCost = noCost;
            }

            yield return new WaitForSecondsRealtime(1f);
            var placed = UnityEngine.Object.FindObjectsByType<Piece>(FindObjectsSortMode.None)
                .Where(p => !before.Contains(p) && Vector3.Distance(p.transform.position, position) < 3f)
                .OrderBy(p => Vector3.Distance(p.transform.position, position)).FirstOrDefault();
            if (placed == null)
            {
                throw new Exception("PlacePiece produced nothing for " + pieceName);
            }

            _created.Add(placed.gameObject);
            _refs[refName ?? pieceName] = placed.gameObject;
            Info($"placed {pieceName} at {placed.transform.position}");
        }

        private void Despawn()
        {
            foreach (var go in _created)
            {
                if (go == null)
                {
                    continue;
                }

                var view = go.GetComponent<ZNetView>();
                if (view != null && view.IsValid())
                {
                    view.ClaimOwnership();
                    ZNetScene.instance.Destroy(go);
                }
                else
                {
                    UnityEngine.Object.Destroy(go);
                }
            }

            Info($"despawned {_created.Count} objects");
            _created.Clear();
            _refs.Clear();
        }

        private GameObject Resolve(string nameOrRef)
        {
            if (nameOrRef != null && _refs.TryGetValue(nameOrRef, out var go) && go != null)
            {
                return go;
            }

            throw new Exception("unknown object reference '" + nameOrRef + "' (use: spawn/place ... as <name>)");
        }

        /// <summary>
        /// goto &lt;ref&gt; [childName] &lt;distance&gt;: stands the player that far from the object, on the side the named child
        /// faces (a smelter's add_ore switch is on one side, add_wood on the other), looking at it.
        /// </summary>
        private IEnumerator GoTo(List<string> a)
        {
            var root = Resolve(Arg(a, 1)).transform;
            var hasChild = a.Count > 3;
            var childName = hasChild ? a[2] : null;
            var distance = F(a[a.Count - 1]);
            var target = root;
            if (hasChild)
            {
                target = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.Equals(childName, StringComparison.OrdinalIgnoreCase))
                         ?? throw new Exception($"no child '{childName}' under {root.name}");
            }

            var player = Player.m_localPlayer;
            var outward = target.position - root.position;
            outward.y = 0f;
            if (outward.sqrMagnitude < 0.01f)
            {
                outward = player.transform.position - root.position;
                outward.y = 0f;
            }

            var position = target.position + outward.normalized * distance;
            if (ZoneSystem.instance.GetGroundHeight(position, out var height))
            {
                position.y = height + 0.1f;
            }

            player.transform.position = position;
            player.m_body.position = position;
            player.m_body.linearVelocity = Vector3.zero;
            yield return new WaitForSecondsRealtime(0.5f);
            yield return LookAt(Arg(a, 1), childName);
        }

        /// <summary>achievementpopup [name]: shows the game's unlock popup for an achievement WITHOUT unlocking anything
        /// (Achievements.AchievementEvent would unlock it on Steam for the user's real account).</summary>
        private void AchievementPopup(string name)
        {
            var achievements = Achievements.m_instance ?? throw new Exception("no Achievements instance");
            var all = achievements.m_achievementLists.SelectMany(l => l.m_achievements).ToList();
            var ach = (name == null ? all.FirstOrDefault(x => x.m_icon != null)
                          : all.FirstOrDefault(x => x.m_id.Equals(name, StringComparison.OrdinalIgnoreCase) || Localization.instance.Localize(x.m_name).Equals(name, StringComparison.OrdinalIgnoreCase)))
                      ?? throw new Exception($"no achievement '{name}' (of {all.Count})");
            var prefab = achievements.m_unlockAchievementPopup;
            UnityEngine.Object.Instantiate(prefab, prefab.transform.position, prefab.transform.rotation).GetComponent<AchievementUnlockPopup>().SetInfo(ach.m_icon, ach.m_name);
            Info($"achievementpopup: {ach.m_id} '{Localization.instance.Localize(ach.m_name)}' (popup only, nothing unlocked)");
        }

        /// <summary>moveto &lt;ref&gt; [child] [dy]: put the player exactly on an object (e.g. a ship deck), dy metres above it,
        /// with no snap to the terrain below (goto snaps to the ground, which is the seabed under a ship).</summary>
        private IEnumerator MoveTo(List<string> a)
        {
            var target = Resolve(Arg(a, 1)).transform;
            var dy = 0.5f;
            if (a.Count > 2 && !float.TryParse(a[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _))
            {
                target = target.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.Equals(a[2], StringComparison.OrdinalIgnoreCase))
                         ?? throw new Exception($"no child '{a[2]}' under {target.name}");
                if (a.Count > 3) dy = F(a[3]);
            }
            else if (a.Count > 2)
            {
                dy = F(a[2]);
            }

            var player = Player.m_localPlayer;
            var position = target.position + Vector3.up * dy;
            player.transform.position = position;
            player.m_body.position = position;
            player.m_body.linearVelocity = Vector3.zero;
            player.m_maxAirAltitude = position.y;
            yield return new WaitForSecondsRealtime(1f);
            player.m_maxAirAltitude = player.transform.position.y;
            Info($"moveto: at {player.transform.position} (target {target.name} {target.position}), on ship: {(player.GetStandingOnShip() != null)}");
        }

        /// <summary>lookat &lt;ref&gt; [childName]: turns the character and the camera at an object (or a named child, e.g. a smelter's add-ore switch).</summary>
        private IEnumerator LookAt(string refName, string childName)
        {
            var root = Resolve(refName).transform;
            var target = root;
            if (!string.IsNullOrEmpty(childName))
            {
                var child = target.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.Equals(childName, StringComparison.OrdinalIgnoreCase));
                if (child == null)
                {
                    throw new Exception($"no child '{childName}' under {target.name} (children: {string.Join(", ", target.GetComponentsInChildren<Collider>().Select(c => c.name).Distinct().Take(20))})");
                }

                target = child;
            }

            // Aim at collider centres until the hover lands on the object with hover text: a hollow shape (a portal ring) has
            // nothing at its centre, and a body collider (a windmill's) hovers nothing. Colliders that carry their own
            // hover/interaction (a smelter's add_ore switch) go first.
            var colliders = target.GetComponentsInChildren<Collider>().Where(c => c.enabled && !c.isTrigger)
                .OrderBy(c => c.GetComponent<Hoverable>() != null || c.GetComponent<Interactable>() != null ? 0 : 1).ToList();
            var points = colliders.Select(c => c.bounds.center).ToList();
            if (points.Count == 0) points.Add(target.position);
            var found = false;
            for (var p = 0; p < points.Count && p < 40; p++)
            {
                yield return Aim(points[p], p == 0 ? 40 : 15);
                var hover = Player.m_localPlayer.GetHoverObject();
                // on the object AND showing hover text (a bare body collider of a windmill hovers nothing)
                if (hover != null && hover.transform.IsChildOf(root) && !string.IsNullOrEmpty(CurrentHoverText(out _)))
                {
                    if (p > 0) Info($"lookat: aimed at collider {colliders[p].name} (#{p}); the first one showed no hover");
                    found = true;
                    break;
                }
            }

            if (!found && points.Count > 1)
            {
                // nothing hoverable: fall back to the first aim point (the old behaviour)
                yield return Aim(points[0], 15);
            }

            yield return new WaitForSecondsRealtime(0.5f);
        }

        private IEnumerator Aim(Vector3 point, int frames)
        {
            var player = Player.m_localPlayer;
            // The hover ray starts at the (third person, orbiting) camera, so aim the camera, not the eyes: set the
            // mouse-look yaw/pitch the camera follows and let it settle over a few frames.
            for (var i = 0; i < frames; i++)
            {
                var origin = GameCamera.instance != null && i > 0 ? GameCamera.instance.transform.position : player.GetEyePoint();
                var direction = (point - origin).normalized;
                var flat = new Vector3(direction.x, 0f, direction.z);
                if (flat.sqrMagnitude > 0.001f)
                {
                    player.m_lookYaw = Quaternion.LookRotation(flat);
                    player.transform.rotation = player.m_lookYaw;
                }

                player.m_lookPitch = Mathf.Clamp(-Mathf.Asin(Mathf.Clamp(direction.y, -1f, 1f)) * Mathf.Rad2Deg, -89f, 89f);
                yield return null;
            }
        }

        private string CurrentHoverText(out GameObject hover)
        {
            hover = Player.m_localPlayer.GetHoverObject();
            if (hover == null)
            {
                return null;
            }

            var hoverable = hover.GetComponentInParent<Hoverable>();
            return hoverable != null ? hoverable.GetHoverText() : "";
        }

        /// <summary>hover [file]: logs what the crosshair is on and its hover text; optional file gets the raw text.</summary>
        private void Hover(List<string> rest)
        {
            var text = CurrentHoverText(out var hover);
            Info(hover == null ? "hover: nothing" : $"hover: {hover.name} -> {(text ?? "").Replace("\n", " | ")}");
            if (rest.Count > 0 && text != null)
            {
                File.WriteAllText(Path.Combine(_outDir, Sanitize(rest[0]) + ".txt"), text);
            }
        }

        private void Use()
        {
            var hover = Player.m_localPlayer.GetHoverObject();
            if (hover == null)
            {
                throw new Exception("nothing hovered");
            }

            Player.m_localPlayer.Interact(hover, false, false);
        }

        /// <summary>interact &lt;ref&gt; [childName]: Interact() on the object's (or child's) Interactable, without aiming.</summary>
        private void Interact(string refName, string childName)
        {
            var root = Resolve(refName).transform;
            if (!string.IsNullOrEmpty(childName))
            {
                root = root.GetComponentsInChildren<Transform>(true).First(t => t.name.Equals(childName, StringComparison.OrdinalIgnoreCase));
            }

            var interactable = root.GetComponentInChildren<Interactable>() ?? root.GetComponentInParent<Interactable>();
            if (interactable == null)
            {
                throw new Exception("no Interactable on " + root.name);
            }

            Info("interact -> " + interactable.Interact(Player.m_localPlayer, false, false));
        }

        private void Effect(string verb, string name)
        {
            var seman = Player.m_localPlayer.GetSEMan();
            if (verb == "remove")
            {
                seman.RemoveStatusEffect(name.GetStableHashCode(), true);
                return;
            }

            if (verb == "list")
            {
                var list = new List<StatusEffect>();
                seman.GetHUDStatusEffects(list);
                Info("effects: " + string.Join(", ", list.Select(e => $"{e.name} {e.m_time:0}/{e.m_ttl:0} '{e.GetIconText()}'")));
                return;
            }

            var added = seman.AddStatusEffect(name.GetStableHashCode(), true);
            if (added == null)
            {
                throw new Exception("status effect not added: " + name);
            }
        }

        // ------------------------------------------------------------------ reflection (mod specific hooks, no compile-time reference)

        private static Type FindType(string fullName)
        {
            // "asm:AssemblyName|Namespace.Type" pins the assembly (two loaded assemblies can declare the same type name).
            if (fullName.StartsWith("asm:"))
            {
                var bar = fullName.IndexOf('|');
                var assemblyName = fullName.Substring(4, bar - 4);
                var pinned = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.GetName().Name == assemblyName)
                             ?? throw new Exception("assembly not loaded: " + assemblyName);
                return pinned.GetType(fullName.Substring(bar + 1), false) ?? throw new Exception("type not found: " + fullName);
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName, false);
                if (type != null)
                {
                    return type;
                }
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types.Where(t => t != null).ToArray();
                }

                var match = types.FirstOrDefault(t => t.Name == fullName);
                if (match != null)
                {
                    return match;
                }
            }

            throw new Exception("type not found: " + fullName);
        }

        private const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

        /// <summary>"Type.Member.Member..." starting from a static field/property; later members are read off the value.</summary>
        private object _last;

        /// <summary>"last" / "last.member.path": the value the previous call / invoke returned.</summary>
        private bool FromLast(string path, out object root, out string[] members)
        {
            root = null;
            members = null;
            if (path != "last" && !path.StartsWith("last."))
            {
                return false;
            }

            root = _last ?? throw new Exception("no previous call/invoke result");
            members = path == "last" ? new string[0] : path.Substring(5).Split('.');
            return true;
        }

        private object ReflectGet(string path)
        {
            if (FromLast(path, out var lastRoot, out var lastMembers))
            {
                var v = lastRoot;
                foreach (var member in lastMembers)
                {
                    v = GetMember(v.GetType(), v, member);
                    if (v == null) return null;
                }

                return v;
            }

            SplitTypePath(path, out var type, out var members);
            object value = null;
            var current = type;
            for (var i = 0; i < members.Length; i++)
            {
                value = GetMember(current, value, members[i]);
                if (value == null)
                {
                    return null;
                }

                current = value.GetType();
            }

            return value;
        }

        private void ReflectSet(string path, string text)
        {
            object owner = null;
            Type current;
            string[] members;
            if (FromLast(path, out var lastRoot, out var lastMembers))
            {
                if (lastMembers.Length == 0) throw new Exception("set last.<member> <value>");
                owner = lastRoot;
                for (var i = 0; i < lastMembers.Length - 1; i++)
                {
                    owner = GetMember(owner.GetType(), owner, lastMembers[i]);
                }

                current = owner.GetType();
                members = lastMembers;
            }
            else
            {
                SplitTypePath(path, out current, out members);
                for (var i = 0; i < members.Length - 1; i++)
                {
                    owner = GetMember(current, owner, members[i]);
                    current = owner.GetType();
                }
            }

            var name = members[members.Length - 1];
            var field = current.GetField(name, Any);
            if (field != null)
            {
                field.SetValue(owner, Convert(text, field.FieldType));
                return;
            }

            var property = current.GetProperty(name, Any);
            if (property == null)
            {
                throw new Exception($"no member {name} on {current.Name}");
            }

            property.SetValue(owner, Convert(text, property.PropertyType));
        }

        /// <summary>setc &lt;ref&gt; &lt;ComponentType&gt; &lt;field&gt; &lt;value&gt;: a field on a component of a spawned/placed object.</summary>
        private void SetOnRef(string refName, string typeName, string member, string text)
        {
            var type = FindType(typeName);
            var component = Resolve(refName).GetComponentInChildren(type, true);
            if (component == null)
            {
                throw new Exception($"no {typeName} on {refName}");
            }

            var field = type.GetField(member, Any) ?? throw new Exception($"no field {member} on {typeName}");
            field.SetValue(component, Convert(text, field.FieldType));
        }

        private object ReflectCallStatic(string path, List<string> args)
        {
            SplitTypePath(path, out var type, out var members);
            object owner = null;
            var current = type;
            for (var i = 0; i < members.Length - 1; i++)
            {
                owner = GetMember(current, owner, members[i]);
                current = owner.GetType();
            }

            return CallMethod(current, owner, members[members.Length - 1], args);
        }

        /// <summary>invoke &lt;ComponentTypeName&gt; &lt;Method&gt; [args]: first live component of that type anywhere in the scene (inactive included).</summary>
        private object InvokeOnComponent(string typeName, string method, List<string> args)
        {
            // "Type@suffix" narrows to the component whose hierarchy path ends with the suffix.
            var at = typeName.IndexOf('@');
            var fragment = at >= 0 ? typeName.Substring(at + 1) : null;
            typeName = at >= 0 ? typeName.Substring(0, at) : typeName;
            var type = FindType(typeName);
            var all = UnityEngine.Object.FindObjectsByType(type, FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (all.Length > 1)
            {
                Info($"{all.Length} live {typeName}: {string.Join(" ; ", all.Take(6).Select(c => PathOf(((Component)c).transform)))}");
            }

            var component = all.FirstOrDefault(c => fragment == null || PathOf(((Component)c).transform).EndsWith(fragment, StringComparison.OrdinalIgnoreCase));
            if (component == null)
            {
                throw new Exception("no live " + typeName);
            }

            return CallMethod(type, component, method, args);
        }

        private object CallMethod(Type type, object owner, string name, List<string> args)
        {
            // Overloads: the first candidate whose parameters accept the given arguments wins (e.g. Screen.SetResolution
            // has (int,int,bool) and (int,int,FullScreenMode); "false" only converts for the first).
            var candidates = type.GetMethods(Any)
                .Where(m => m.Name == name && (m.GetParameters().Length == args.Count || (m.GetParameters().Length > args.Count && m.GetParameters().Skip(args.Count).All(p => p.HasDefaultValue))))
                .OrderBy(m => m.GetParameters().Length)
                .ToList();
            if (candidates.Count == 0) throw new Exception($"no method {type.Name}.{name}({args.Count} args)");
            Exception lastError = null;
            foreach (var method in candidates)
            {
                var parameters = method.GetParameters();
                var values = new object[parameters.Length];
                try
                {
                    for (var i = 0; i < parameters.Length; i++)
                    {
                        values[i] = i < args.Count ? Convert(args[i], parameters[i].ParameterType) : parameters[i].DefaultValue;
                    }
                }
                catch (Exception e)
                {
                    lastError = e;
                    continue;
                }

                return method.Invoke(method.IsStatic ? null : owner, values);
            }

            throw new Exception($"no overload of {type.Name}.{name} accepts ({string.Join(", ", args)}): {lastError?.Message}");
        }

        private static void SplitTypePath(string path, out Type type, out string[] members)
        {
            var prefix = "";
            if (path.StartsWith("asm:"))
            {
                prefix = path.Substring(0, path.IndexOf('|') + 1);
                path = path.Substring(prefix.Length);
            }

            var parts = path.Split('.');
            // The longest prefix that names a type wins ("AugaUnity.AugaHealthBar.DebugAdrenalineOverride").
            for (var take = parts.Length - 1; take >= 1; take--)
            {
                var candidate = prefix + string.Join(".", parts.Take(take));
                try
                {
                    type = FindType(candidate);
                    members = parts.Skip(take).ToArray();
                    return;
                }
                catch (Exception)
                {
                    // try a shorter prefix
                }
            }

            throw new Exception("cannot resolve a type in '" + path + "'");
        }

        private static object GetMember(Type type, object owner, string name)
        {
            for (var t = type; t != null; t = t.BaseType)
            {
                var field = t.GetField(name, Any | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    return field.GetValue(field.IsStatic ? null : owner);
                }

                var property = t.GetProperty(name, Any | BindingFlags.DeclaredOnly);
                if (property != null)
                {
                    return property.GetValue(property.GetMethod.IsStatic ? null : owner);
                }
            }

            throw new Exception($"no member {name} on {type.Name}");
        }

        private object Convert(string text, Type target)
        {
            if (text == "null" && !target.IsValueType) return null;
            if (text == "@last") return _last;
            // "@Type.path" passes the object at a static-rooted path (e.g. "@Hud.instance.m_config") instead of a literal.
            if (text.StartsWith("@") && text.Length > 1 && !target.IsPrimitive && target != typeof(string)) return ReflectGet(text.Substring(1));
            if (target == typeof(string)) return text;
            if (target.IsEnum) return Enum.Parse(target, text, true);
            if (target == typeof(bool)) return text == "1" || text.Equals("true", StringComparison.OrdinalIgnoreCase);
            if (target == typeof(float)) return F(text);
            if (target == typeof(Color) || target == typeof(Vector3) || target == typeof(Vector2))
            {
                // "r,g,b[,a]" / "x,y[,z]"
                var v = text.Split(',').Select(F).ToArray();
                if (target == typeof(Color)) return new Color(v[0], v[1], v[2], v.Length > 3 ? v[3] : 1f);
                if (target == typeof(Vector3)) return new Vector3(v[0], v[1], v.Length > 2 ? v[2] : 0f);
                return new Vector2(v[0], v[1]);
            }

            return System.Convert.ChangeType(text, target, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static string Describe(object value)
        {
            if (value == null) return "null";
            if (value is UnityEngine.Object o) return o == null ? "destroyed " + value.GetType().Name : $"{o.name} ({value.GetType().Name})";
            if (value is IEnumerable e && !(value is string)) return "[" + string.Join(", ", e.Cast<object>().Take(20).Select(Describe)) + "]";
            return value.ToString();
        }

        // ------------------------------------------------------------------ uGUI pointer events (tooltips, buttons) without a mouse

        private static GameObject FindUi(string nameOrPath)
        {
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!canvas.isRootCanvas)
                {
                    continue;
                }

                var byPath = canvas.transform.Find(nameOrPath);
                if (byPath != null && byPath.gameObject.activeInHierarchy)
                {
                    return byPath.gameObject;
                }

                // Several objects with one name (list rows, tiles): the first one that is a live, clickable button wins.
                var named = canvas.GetComponentsInChildren<Transform>(false).Where(t => t.name == nameOrPath).ToList();
                var byName = named.FirstOrDefault(t => t.GetComponent<Selectable>() is Selectable sel && sel.IsInteractable()) ?? named.FirstOrDefault();
                if (byName != null)
                {
                    return byName.gameObject;
                }
            }

            // A partial path ("Store/SellButton"): the first active object whose full path ends with it.
            if (nameOrPath.Contains("/"))
            {
                foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                {
                    if (!canvas.isRootCanvas) continue;
                    var bySuffix = canvas.GetComponentsInChildren<Transform>(false).FirstOrDefault(t => PathOf(t).EndsWith("/" + nameOrPath, StringComparison.OrdinalIgnoreCase));
                    if (bySuffix != null)
                    {
                        return bySuffix.gameObject;
                    }
                }
            }

            throw new Exception("no active UI object '" + nameOrPath + "'");
        }

        private void UiPointer(string nameOrPath, bool click, string button = "left", float? x = null, float? y = null)
        {
            var target = FindUi(nameOrPath);
            var pointerButton = button.Equals("middle", StringComparison.OrdinalIgnoreCase) ? PointerEventData.InputButton.Middle
                : button.Equals("right", StringComparison.OrdinalIgnoreCase) ? PointerEventData.InputButton.Right : PointerEventData.InputButton.Left;
            var position = x.HasValue && y.HasValue ? new Vector2(x.Value, y.Value) : RectTransformUtility.WorldToScreenPoint(null, target.transform.position);
            var data = new PointerEventData(EventSystem.current) { position = position, button = pointerButton };
            ExecuteEvents.ExecuteHierarchy(target, data, ExecuteEvents.pointerEnterHandler);
            if (click)
            {
                ExecuteEvents.ExecuteHierarchy(target, data, ExecuteEvents.pointerDownHandler);
                ExecuteEvents.ExecuteHierarchy(target, data, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.ExecuteHierarchy(target, data, ExecuteEvents.pointerClickHandler);
            }

            Info((click ? "clicked " : "hovering ") + PathOf(target.transform));
        }

        // ------------------------------------------------------------------ inspection

        private static string PathOf(Transform t)
        {
            var path = t.name;
            for (var p = t.parent; p != null; p = p.parent)
            {
                path = p.name + "/" + path;
            }

            return path;
        }

        /// <summary>A spawn/place reference, a static member path ("Hud.instance.m_buildUi"), or "comp:TypeName[@pathSuffix]" (first live component).</summary>
        private Transform ResolveTransform(string what)
        {
            if (_refs.TryGetValue(what, out var go) && go != null)
            {
                return go.transform;
            }

            if (what.StartsWith("comp:"))
            {
                var spec = what.Substring(5).Split('@');
                var all = UnityEngine.Object.FindObjectsByType(FindType(spec[0]), FindObjectsInactive.Include, FindObjectsSortMode.None).Cast<Component>();
                var found = all.FirstOrDefault(c => spec.Length < 2 || PathOf(c.transform).EndsWith(spec[1], StringComparison.OrdinalIgnoreCase));
                return found != null ? found.transform : throw new Exception("no live " + what);
            }

            // "ui:<nameOrPath>" - an active UI object the way click / hoverui find it (path under a root canvas, or by name).
            if (what.StartsWith("ui:"))
            {
                return FindUi(what.Substring(3)).transform;
            }

            var value = ReflectGet(what);
            return value is Component co ? co.transform : value is GameObject g ? g.transform : throw new Exception("not a Component/GameObject: " + what);
        }

        /// <summary>dump &lt;what&gt; &lt;file&gt;: what = ui (every root canvas) | a static member path | a spawn/place reference.</summary>
        private void Dump(string what, string file)
        {
            var sb = new StringBuilder();
            if (what == "ui")
            {
                foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(c => c.transform.parent == null || c.transform.parent.GetComponentInParent<Canvas>(true) == null).OrderBy(c => PathOf(c.transform)))
                {
                    sb.AppendLine($"## {PathOf(canvas.transform)}  order={canvas.sortingOrder} active={canvas.gameObject.activeInHierarchy}");
                    DumpTree(sb, canvas.transform, canvas.transform);
                }
            }
            else
            {
                var root = ResolveTransform(what);

                sb.AppendLine("## " + PathOf(root));
                DumpTree(sb, root, root);
            }

            File.WriteAllText(Path.Combine(_outDir, Sanitize(file ?? what) + ".txt"), sb.ToString());
        }

        private static void DumpTree(StringBuilder sb, Transform root, Transform t)
        {
            if (t != root)
            {
                var path = t.name;
                for (var p = t.parent; p != null && p != root; p = p.parent)
                {
                    path = p.name + "/" + path;
                }

                sb.Append(path).Append(t.gameObject.activeSelf ? "" : "  (inactive)").Append("  [");
                sb.Append(string.Join(", ", t.GetComponents<Component>().Where(c => c != null && !(c is Transform) && !(c is CanvasRenderer)).Select(c => c.GetType().Name)));
                sb.Append("]");
                if (t is RectTransform rect)
                {
                    sb.Append($"  {rect.rect.width:0}x{rect.rect.height:0}");
                }

                var image = t.GetComponent<Image>();
                if (image != null && image.sprite != null)
                {
                    sb.Append("  sprite=" + image.sprite.name);
                }

                var text = t.GetComponent<TMPro.TMP_Text>();
                if (text != null)
                {
                    sb.Append("  font=" + (text.font != null ? text.font.name : "null") + " text=\"" + (text.text ?? "").Replace("\n", " ") + "\"");
                }

                sb.AppendLine();
            }

            for (var i = 0; i < t.childCount; i++)
            {
                DumpTree(sb, root, t.GetChild(i));
            }
        }

        /// <summary>describe &lt;static member path | ref&gt;: active flags, canvas, alpha, rect up the parent chain - why can't I see it.</summary>
        private void DescribeObject(string what)
        {
            var t = ResolveTransform(what);

            var parts = new List<string>();
            for (var p = t; p != null; p = p.parent)
            {
                var group = p.GetComponent<CanvasGroup>();
                var canvas = p.GetComponent<Canvas>();
                var rect = p as RectTransform;
                parts.Add(p.name + (rect != null ? $"[{rect.rect.width:0}x{rect.rect.height:0} at {rect.position.x:0},{rect.position.y:0} anchors {rect.anchorMin.x:0.##},{rect.anchorMin.y:0.##}-{rect.anchorMax.x:0.##},{rect.anchorMax.y:0.##} pivot {rect.pivot.x:0.##},{rect.pivot.y:0.##} apos {rect.anchoredPosition.x:0},{rect.anchoredPosition.y:0} scale {rect.localScale.x:0.##}]" : "")
                          + (p.gameObject.activeSelf ? "" : "[OFF]") + (group != null ? $"[a={group.alpha:0.##}]" : "")
                          + (canvas != null ? $"[canvas order={canvas.sortingOrder}]" : ""));
            }

            Info($"describe {what}: activeInHierarchy={t.gameObject.activeInHierarchy} chain={string.Join(" < ", parts)}");
        }

        /// <summary>
        /// expect noerrors | expect hover contains "text" | expect active &lt;member path&gt; | expect inactive &lt;member path&gt; | expect ui "name"
        /// </summary>
        private void Expect(List<string> a)
        {
            switch ((Arg(a, 1) ?? "").ToLowerInvariant())
            {
                case "noerrors":
                    if (_errors.Count > 0) throw new Exception($"{_errors.Count} game errors so far; latest: {_errors[_errors.Count - 1]}");
                    break;
                case "hover":
                    var text = CurrentHoverText(out var hover) ?? "";
                    var wanted = Arg(a, 3, "");
                    if (hover == null) throw new Exception("nothing hovered");
                    if (text.IndexOf(wanted, StringComparison.OrdinalIgnoreCase) < 0) throw new Exception($"hover text of {hover.name} lacks '{wanted}': {text.Replace("\n", " | ")}");
                    break;
                case "active":
                case "inactive":
                    if ((Arg(a, 2) ?? "").StartsWith("ui:"))
                    {
                        // FindUi only sees active objects: found = active, not found = inactive (or absent).
                        GameObject uiObject = null;
                        try { uiObject = FindUi(Arg(a, 2).Substring(3)); } catch (Exception) { }
                        var wantActive = a[1].ToLowerInvariant() == "active";
                        if ((uiObject != null && uiObject.activeInHierarchy) != wantActive) throw new Exception($"{Arg(a, 2)} is {(uiObject != null ? "active" : "not an active UI object")}");
                        break;
                    }

                    var value = ReflectGet(Arg(a, 2));
                    var go = value is Component c ? c.gameObject : value as GameObject;
                    if (go == null) throw new Exception(Arg(a, 2) + " is null or not an object");
                    if (go.activeInHierarchy != (a[1].ToLowerInvariant() == "active")) throw new Exception($"{Arg(a, 2)} activeInHierarchy={go.activeInHierarchy}");
                    break;
                case "value":
                    // expect value <member path> contains "text"
                    var actual = Describe(ReflectGet(Arg(a, 2)));
                    if (actual.IndexOf(Arg(a, 4, ""), StringComparison.OrdinalIgnoreCase) < 0) throw new Exception($"{Arg(a, 2)} = '{actual}', expected to contain '{Arg(a, 4)}'");
                    break;
                case "ui":
                    FindUi(Arg(a, 2));
                    break;
                case "height":
                case "ground":
                case "biome":
                    ExpectTerrain(a);
                    break;
                default:
                    throw new Exception("unknown expectation");
            }
        }
    }
}
