using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ClaudeHeim
{
    /// <summary>
    /// Survival-loop commands: eating, dying, respawning. Death goes through the same HitData path as the console
    /// "die" command, so tombstone, death pin, food loss and the respawn timer all behave as in normal play.
    /// </summary>
    internal sealed partial class Runner
    {
        private List<ItemDrop.ItemData> _deathInventory;
        private Vector3 _deathPosition;
        private int _deadPlayerId;

        /// <summary>eat &lt;item&gt; [ok|fail]: give one if needed, then Player.ConsumeItem (the eat path the inventory uses). Default: must succeed.</summary>
        private IEnumerator Eat(string prefabName, string expectation)
        {
            var player = Player.m_localPlayer;
            Give(prefabName, 1);
            var item = player.GetInventory().GetAllItems().FirstOrDefault(i => i.m_dropPrefab != null && i.m_dropPrefab.name == prefabName)
                       ?? throw new Exception("not in inventory: " + prefabName);
            var before = player.GetFoods().Count;
            var ate = player.ConsumeItem(player.GetInventory(), item);
            yield return new WaitForSecondsRealtime(0.5f);
            Info($"eat {prefabName}: {(ate ? "ok" : "refused")}; foods {before} -> {player.GetFoods().Count}: {FoodList()}");
            var wantFail = (expectation ?? "ok").Equals("fail", StringComparison.OrdinalIgnoreCase);
            if (ate == wantFail)
            {
                throw new Exception($"eat {prefabName} {(ate ? "succeeded" : "was refused")}, expected {(wantFail ? "refusal" : "success")}");
            }
        }

        /// <summary>split &lt;item&gt; [open|close]: open the split-stack dialog for a stack in the player inventory (the shift-click path), or close it.</summary>
        private IEnumerator SplitDialog(string prefabName, string action)
        {
            var gui = InventoryGui.instance ?? throw new Exception("no InventoryGui");
            if (action.Equals("close", StringComparison.OrdinalIgnoreCase))
            {
                typeof(InventoryGui).GetMethod("HideSplitDialog", Any).Invoke(gui, null);
                yield return new WaitForSecondsRealtime(0.5f);
                yield break;
            }

            var player = Player.m_localPlayer;
            var item = player.GetInventory().GetAllItems().FirstOrDefault(i => i.m_dropPrefab != null && i.m_dropPrefab.name == prefabName)
                       ?? throw new Exception("not in inventory: " + prefabName);
            if (item.m_stack < 2) throw new Exception($"{prefabName} stack is {item.m_stack}; need at least 2 to split");
            var show = typeof(InventoryGui).GetMethod("ShowSplitDialog", Any) ?? throw new Exception("no InventoryGui.ShowSplitDialog");
            show.Invoke(gui, new object[] { item, player.GetInventory() });
            yield return new WaitForSecondsRealtime(0.5f);
            var dialog = gui.m_splitDialog;
            Info($"split: dialog={(dialog != null ? PathOf(dialog.transform) : "null")} active={(dialog != null && dialog.IsActive)} slider={(dialog != null && dialog.m_splitSlider != null ? dialog.m_splitSlider.value + "/" + dialog.m_splitSlider.maxValue : "-")}");
        }

        private static string FoodList()
        {
            var player = Player.m_localPlayer;
            if (player == null) return "-";
            return string.Join(", ", player.GetFoods().Select(f => $"{Localization.instance.Localize(f.m_item.m_shared.m_name)} ({f.m_time:0}s hp{f.m_health:0} st{f.m_stamina:0} ei{f.m_eitr:0})"));
        }

        /// <summary>die: kill the local player the way the console command does. Snapshots the inventory so respawn can hand it back.</summary>
        private IEnumerator Die()
        {
            var player = Player.m_localPlayer ?? throw new Exception("no local player");
            player.m_godMode = false;
            _deadPlayerId = player.GetInstanceID();
            _deathPosition = player.transform.position;
            _deathInventory = player.GetInventory().GetAllItems().Select(i => i.Clone()).ToList();
            Info($"die: at {_deathPosition}, {_deathInventory.Count} item stacks snapshotted, foods: {FoodList()}");
            player.Damage(new HitData { m_damage = { m_damage = 99999f }, m_hitType = HitData.HitType.Self });

            var waited = 0f;
            while (waited < 15f && Player.m_localPlayer != null && Player.m_localPlayer.GetInstanceID() == _deadPlayerId && !Player.m_localPlayer.IsDead())
            {
                waited += 0.25f;
                yield return new WaitForSecondsRealtime(0.25f);
            }

            if (Player.m_localPlayer != null && Player.m_localPlayer.GetInstanceID() == _deadPlayerId && !Player.m_localPlayer.IsDead())
            {
                throw new Exception("player did not die");
            }

            Info($"die: dead after {waited:0.0}s (vanilla respawn request follows in ~10 s)");

            // The tombstone is created synchronously in OnDeath; remove it now, while its zone is still loaded (the
            // respawn point can be hundreds of metres away). The snapshot above is what respawn hands back.
            yield return new WaitForSecondsRealtime(1f);
            Info($"die: {RemoveOwnTombstones()} tombstone(s) removed at the death point");
        }

        /// <summary>type "&lt;text&gt;": put text into the focused input field (TextInput's, or the selected TMP_InputField), honouring characterLimit like typing would.</summary>
        private void TypeText(string text)
        {
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            var field = selected != null ? selected.GetComponent<TMPro.TMP_InputField>() : null;
            if (field == null && TextInput.instance != null && TextInput.instance.m_panel != null && TextInput.instance.m_panel.activeInHierarchy)
            {
                field = TextInput.instance.m_inputField;
            }

            if (field == null) throw new Exception("no focused input field");
            var limit = field.characterLimit;
            var typed = limit > 0 && text.Length > limit ? text.Substring(0, limit) : text;
            field.text = typed;
            field.caretPosition = typed.Length;
            Info($"type: '{typed}' into {PathOf(field.transform)} (limit {limit}, wanted {text.Length} chars, field now '{field.text}')");
        }

        /// <summary>move &lt;item&gt; player|container [n]: move a stack (or n of it) between the player inventory and the open container.</summary>
        private void Move(string prefabName, string target, int amount)
        {
            var player = Player.m_localPlayer.GetInventory();
            var container = InventoryGui.instance != null && InventoryGui.instance.m_currentContainer != null ? InventoryGui.instance.m_currentContainer.GetInventory() : null;
            if (container == null) throw new Exception("no container is open");
            var toContainer = !target.Equals("player", StringComparison.OrdinalIgnoreCase);
            var from = toContainer ? player : container;
            var to = toContainer ? container : player;
            var item = from.GetAllItems().FirstOrDefault(i => i.m_dropPrefab != null && i.m_dropPrefab.name == prefabName)
                       ?? throw new Exception($"{prefabName} not in the {(toContainer ? "player" : "container")} inventory");
            var n = amount <= 0 || amount > item.m_stack ? item.m_stack : amount;
            var moving = item.Clone();
            moving.m_stack = n;
            if (!to.AddItem(moving)) throw new Exception($"no room for {n} {prefabName} in the {(toContainer ? "container" : "player")} inventory");
            from.RemoveItem(item, n);
            Info($"move: {n} {prefabName} -> {(toContainer ? "container" : "player")} (player {player.NrOfItems()} stacks, container {container.NrOfItems()} stacks)");
        }

        /// <summary>remove &lt;item&gt; &lt;n&gt;: take n of that item out of the player inventory (teardown).</summary>
        private void RemoveItems(string prefabName, int amount)
        {
            var inventory = Player.m_localPlayer.GetInventory();
            var removed = 0;
            foreach (var item in inventory.GetAllItems().Where(i => i.m_dropPrefab != null && i.m_dropPrefab.name == prefabName).ToList())
            {
                var n = Math.Min(amount - removed, item.m_stack);
                if (n <= 0) break;
                inventory.RemoveItem(item, n);
                removed += n;
            }

            Info($"remove: {removed} {prefabName} removed");
        }

        /// <summary>fill &lt;ref&gt; &lt;item&gt; &lt;n&gt; [quality]: put a stack into a placed container (quality = upgrade level).</summary>
        private void Fill(string refName, string prefabName, int amount, int quality)
        {
            var container = Resolve(refName).GetComponentInChildren<Container>() ?? throw new Exception(refName + " has no Container");
            var prefab = ObjectDB.instance.GetItemPrefab(prefabName) ?? throw new Exception("no item prefab " + prefabName);
            var inventory = container.GetInventory();
            if (quality <= 1)
            {
                inventory.AddItem(prefab, amount);
            }
            else
            {
                var added = inventory.AddItem(prefabName, amount, quality, 0, 0L, "", true);
                if (added == null) throw new Exception($"could not add {prefabName} q{quality} to {refName}");
            }

            Info($"fill: {amount} {prefabName} q{quality} -> {refName} ({inventory.NrOfItems()} stacks)");
        }

        /// <summary>quality &lt;item&gt; &lt;n&gt;: set the upgrade level of that stack in the player inventory.</summary>
        private void Quality(string prefabName, int quality)
        {
            var item = Player.m_localPlayer.GetInventory().GetAllItems().FirstOrDefault(i => i.m_dropPrefab != null && i.m_dropPrefab.name == prefabName)
                       ?? throw new Exception("not in inventory: " + prefabName);
            item.m_quality = quality;
            Info($"quality: {prefabName} -> {quality}");
        }

        /// <summary>recipe &lt;item&gt;: select that recipe in the open crafting panel (by item prefab name or localized name).</summary>
        private IEnumerator Recipe(string itemName)
        {
            var gui = InventoryGui.instance ?? throw new Exception("no InventoryGui");
            var listField = typeof(InventoryGui).GetField("m_availableRecipes", Any) ?? throw new Exception("no m_availableRecipes");
            var list = listField.GetValue(gui) as IList ?? throw new Exception("m_availableRecipes is not a list");
            var index = -1;
            var names = new List<string>();
            for (var i = 0; i < list.Count; i++)
            {
                var pair = list[i];
                var recipe = pair.GetType().GetProperty("Recipe", Any)?.GetValue(pair) as Recipe ?? pair.GetType().GetField("Recipe", Any)?.GetValue(pair) as Recipe;
                var item = recipe != null ? recipe.m_item : null;
                if (item == null) continue;
                var localized = Localization.instance.Localize(item.m_itemData.m_shared.m_name);
                names.Add(item.name);
                if (item.name.Equals(itemName, StringComparison.OrdinalIgnoreCase) || localized.Equals(itemName, StringComparison.OrdinalIgnoreCase))
                {
                    index = i;
                    break;
                }
            }

            if (index < 0) throw new Exception($"recipe '{itemName}' not in the {list.Count} available: {string.Join(", ", names.Take(30))}");
            var setRecipe = typeof(InventoryGui).GetMethod("SetRecipe", Any) ?? throw new Exception("no InventoryGui.SetRecipe");
            setRecipe.Invoke(gui, new object[] { index, true });
            yield return new WaitForSecondsRealtime(0.6f);
            Info($"recipe: selected #{index} {itemName}; name shown: '{(gui.m_recipeName != null ? gui.m_recipeName.text : "-")}'");
        }

        /// <summary>teleport &lt;x&gt; &lt;z&gt; [timeout]: Player.TeleportTo (distant), waits until the zone is loaded and the player stands there.</summary>
        private IEnumerator Teleport(float x, float z, float timeout)
        {
            var player = Player.m_localPlayer ?? throw new Exception("no local player");
            var target = new Vector3(x, 80f, z);
            if (!player.TeleportTo(target, player.transform.rotation, true))
            {
                throw new Exception("TeleportTo refused (already teleporting?)");
            }

            var waited = 0f;
            while (waited < timeout && (player.IsTeleporting() || Vector2.Distance(new Vector2(player.transform.position.x, player.transform.position.z), new Vector2(x, z)) > 10f))
            {
                waited += 0.5f;
                yield return new WaitForSecondsRealtime(0.5f);
            }

            yield return new WaitForSecondsRealtime(2f);
            Info($"teleport: at {player.transform.position} after {waited:0.0}s (target {x}, {z})");
            if (Vector2.Distance(new Vector2(player.transform.position.x, player.transform.position.z), new Vector2(x, z)) > 10f)
            {
                throw new Exception($"teleport did not arrive at {x}, {z} within {timeout}s");
            }
        }

        /// <summary>tombstones list|clear [radius]: the local player's own tombstones within the radius (world cleanup after death tests).</summary>
        private void Tombstones(string verb, float radius)
        {
            var owner = Game.instance.GetPlayerProfile().GetName();
            var here = Player.m_localPlayer.transform.position;
            var own = UnityEngine.Object.FindObjectsByType<TombStone>(FindObjectsSortMode.None)
                .Where(t => t.GetOwnerName() == owner && Vector3.Distance(t.transform.position, here) <= radius).ToList();
            Info($"tombstones: {own.Count} of {owner} within {radius} m: {string.Join("; ", own.Select(t => t.transform.position.ToString()))}");
            if (!verb.Equals("clear", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            foreach (var tomb in own)
            {
                ZNetScene.instance.Destroy(tomb.gameObject);
            }

            Info($"tombstones: {own.Count} removed");
        }

        private int RemoveOwnTombstones()
        {
            var removed = 0;
            var owner = Game.instance.GetPlayerProfile().GetName();
            foreach (var tomb in UnityEngine.Object.FindObjectsByType<TombStone>(FindObjectsSortMode.None))
            {
                if (Vector3.Distance(tomb.transform.position, _deathPosition) < 8f && tomb.GetOwnerName() == owner)
                {
                    ZNetScene.instance.Destroy(tomb.gameObject);
                    removed++;
                }
            }

            return removed;
        }

        /// <summary>respawn [timeout]: wait for the new player after a death, restore the snapshotted items and clear the tombstone this run made.</summary>
        private IEnumerator Respawn(float timeout)
        {
            var waited = 0f;
            while (waited < timeout && (Player.m_localPlayer == null || Player.m_localPlayer.GetInstanceID() == _deadPlayerId || Player.m_localPlayer.IsDead() || Hud.instance == null))
            {
                waited += 0.5f;
                yield return new WaitForSecondsRealtime(0.5f);
            }

            var player = Player.m_localPlayer;
            if (player == null || player.GetInstanceID() == _deadPlayerId || player.IsDead())
            {
                throw new Exception($"no respawned player after {timeout}s");
            }

            // Let the spawn settle (valkyrie / fade, HUD bars, zone load).
            yield return new WaitForSecondsRealtime(3f);
            Info($"respawn: new player after {waited:0.0}s at {player.transform.position}, hp {player.GetHealth():0}/{player.GetMaxHealth():0}, foods: {FoodList()}");

            if (_deathInventory != null)
            {
                var inventory = player.GetInventory();
                var restored = 0;
                foreach (var item in _deathInventory)
                {
                    if (inventory.AddItem(item))
                    {
                        restored++;
                    }
                }

                Info($"respawn: {restored}/{_deathInventory.Count} item stacks handed back");
                _deathInventory = null;

                // Fallback for a respawn next to the death point (bed): the stone may only load now.
                var removed = RemoveOwnTombstones();
                if (removed > 0)
                {
                    Info($"respawn: {removed} tombstone(s) removed near the death point");
                }
            }
        }
    }
}
