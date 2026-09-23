using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace ClaudeHeim
{
    /// <summary>Item tooltips: hover an inventory slot the way the mouse does.</summary>
    internal sealed partial class Runner
    {
        /// <summary>
        /// hoveritem &lt;prefab&gt;: with the inventory open, point at the slot holding that item and start the hover, so
        /// the game shows its item tooltip. The pointer is moved in the Input System's mouse STATE only (the OS cursor
        /// is not touched): vanilla's UITooltip hides the tooltip unless ZInput.pointerPosition is over the slot.
        /// </summary>
        private IEnumerator HoverItem(string prefabName)
        {
            var gui = InventoryGui.instance ?? throw new Exception("no InventoryGui");
            var inventory = Player.m_localPlayer.GetInventory();
            var item = inventory.GetAllItems().FirstOrDefault(i => i.m_dropPrefab != null && i.m_dropPrefab.name == prefabName)
                       ?? throw new Exception("not in inventory: " + prefabName);
            var element = gui.m_playerGrid.GetElement(item.m_gridPos.x, item.m_gridPos.y, inventory.GetWidth())
                          ?? throw new Exception($"no grid element at {item.m_gridPos}");
            var rect = element.GetElementRectTransform();
            var screen = RectTransformUtility.WorldToScreenPoint(null, rect.position);
            SetPointer(screen);
            yield return null;

            // UITooltip.OnHoverStart keeps eventData.pointerEnter as the hovered object; it must be the slot.
            var data = new PointerEventData(EventSystem.current) { position = screen, pointerEnter = rect.gameObject };
            ExecuteEvents.ExecuteHierarchy(rect.gameObject, data, ExecuteEvents.pointerEnterHandler);
            yield return new WaitForSecondsRealtime(1.2f);
            SetPointer(screen);
            yield return new WaitForSecondsRealtime(0.3f);
            // The game's own tooltip text for the item, localized: what vanilla shows, whatever UI mod is installed.
            File.WriteAllText(Path.Combine(_outDir, $"tooltip_{prefabName}.txt"), Localization.instance.Localize(item.GetTooltip()));
            var tooltip = UITooltip.m_tooltip;
            Info($"hoveritem {prefabName}: slot {item.m_gridPos} at {screen} (game pointer {ZInput.pointerPosition}), tooltip {(tooltip != null && tooltip.activeInHierarchy ? PathOf(tooltip.transform) : "not shown")}");
        }

        /// <summary>
        /// finditems &lt;filter&gt; [max]: log item prefabs from ObjectDB. Filter: "type:&lt;ItemType&gt;", "set" (has a set
        /// effect), "mods" (any of 1.0's equipment modifiers), "se" (attack/consume status effect), or a name substring.
        /// </summary>
        private void FindItems(string filter, int max)
        {
            var modFields = typeof(ItemDrop.ItemData.SharedData).GetFields()
                .Where(f => f.FieldType == typeof(float) && f.Name.EndsWith("Modifier", StringComparison.Ordinal) || f.Name == "m_maxAdrenaline")
                .ToList();
            var matches = ObjectDB.instance.m_items
                .Select(go => go != null ? go.GetComponent<ItemDrop>() : null)
                .Where(drop => drop != null)
                .Where(drop =>
                {
                    var shared = drop.m_itemData.m_shared;
                    if (filter.StartsWith("type:", StringComparison.OrdinalIgnoreCase))
                        return shared.m_itemType.ToString().Equals(filter.Substring(5), StringComparison.OrdinalIgnoreCase);
                    switch (filter.ToLowerInvariant())
                    {
                        case "set": return shared.m_setStatusEffect != null;
                        case "mods": return modFields.Any(f => f.Name != "m_eitrRegenModifier" && (float)f.GetValue(shared) != 0f);
                        case "se": return shared.m_attackStatusEffect != null || shared.m_consumeStatusEffect != null;
                        default: return drop.name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
                    }
                })
                .Select(drop => $"{drop.name} ({drop.m_itemData.m_shared.m_itemType})")
                .ToList();
            Info($"finditems {filter}: {matches.Count} match(es): {string.Join(", ", matches.Take(max))}");
        }

        private static void SetPointer(Vector2 screen)
        {
            if (Mouse.current != null)
            {
                InputState.Change(Mouse.current.position, screen);
            }
        }
    }
}
