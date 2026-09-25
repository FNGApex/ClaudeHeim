using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ClaudeHeim
{
    internal sealed partial class Runner
    {
        private bool _noMobs;
        private float _noMobsRadius = 60f;
        private float _noMobsNext;
        private readonly Dictionary<string, int> _noMobsRemoved = new Dictionary<string, int>();

        /// <summary>nomobs on [radius] | off: keep wild creatures away from the test (the user saw a Greyling attack during a
        /// lab run). While on, every second removes wild AI creatures within the radius of the player - never players, tamed
        /// creatures or anything this scenario spawned (spawn/place), so boss/trader/boar tests keep their subjects.</summary>
        private void NoMobs(string mode, string radius)
        {
            _noMobs = !string.Equals(mode, "off", StringComparison.OrdinalIgnoreCase);
            if (radius != null) _noMobsRadius = F(radius);
            Info(_noMobs ? $"nomobs: on, radius {_noMobsRadius} m" : $"nomobs: off; removed so far: {NoMobsSummary()}");
            if (_noMobs) NoMobsTick(true);
        }

        /// <summary>pickup [radius]: the local player picks up every loose item within the radius (default 5 m), the way
        /// walking over it would (Humanoid.Pickup, no auto-equip) - e.g. magic items a console command dropped at the feet.</summary>
        private void PickupAround(float radius)
        {
            var player = Player.m_localPlayer ?? throw new Exception("no local player");
            var here = player.transform.position;
            var names = new List<string>();
            foreach (var drop in UnityEngine.Object.FindObjectsByType<ItemDrop>(FindObjectsSortMode.None))
            {
                if (drop == null || Vector3.Distance(drop.transform.position, here) > radius) continue;
                var name = drop.m_itemData?.m_shared?.m_name ?? drop.name;
                if (player.Pickup(drop.gameObject, false, false)) names.Add(Localization.instance.Localize(name));
            }

            Info($"pickup: {names.Count} item(s) within {radius} m" + (names.Count > 0 ? ": " + string.Join(", ", names) : ""));
        }

        private string NoMobsSummary() => _noMobsRemoved.Count == 0 ? "none" : string.Join(", ", _noMobsRemoved.Select(kv => $"{kv.Key} x{kv.Value}"));

        /// <summary>Called from the plugin's Update.</summary>
        internal void NoMobsTick(bool now = false)
        {
            if (!_noMobs || (!now && Time.unscaledTime < _noMobsNext)) return;
            _noMobsNext = Time.unscaledTime + 1f;
            var player = Player.m_localPlayer;
            if (player == null || ZNetScene.instance == null) return;
            var created = new HashSet<GameObject>(_created.Where(g => g != null));
            var here = player.transform.position;
            foreach (var c in Character.GetAllCharacters().ToList())
            {
                if (c == null || c is Player || c.IsTamed() || created.Contains(c.gameObject)) continue;
                var distance = Vector3.Distance(c.transform.position, here);
                if (c.GetComponent<BaseAI>() == null || distance > _noMobsRadius) continue;
                var view = c.GetComponent<ZNetView>();
                if (view == null || !view.IsValid()) continue;
                var name = c.gameObject.name.Replace("(Clone)", "");
                view.ClaimOwnership();
                ZNetScene.instance.Destroy(c.gameObject);
                _noMobsRemoved[name] = _noMobsRemoved.TryGetValue(name, out var n) ? n + 1 : 1;
                Info($"nomobs: removed wild {name} at {distance:0} m");
            }
        }
    }
}
