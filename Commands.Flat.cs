using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ClaudeHeim
{
    internal sealed partial class Runner
    {
        private const float MegaFlattenMaxRadius = 300f;
        private readonly Dictionary<string, List<GameObject>> _floors = new Dictionary<string, List<GameObject>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// terrain megaflatten &lt;name&gt; &lt;pt&gt; &lt;radius&gt; [h|auto]: a nuke over a large circle (every networked object
        /// except players) and then every vertex inside it set to ONE exact height, smoothing cleared. Walks the area zone by
        /// zone: a zone that is not loaded is visited first (teleport, god mode in transit). Vertices beyond the game's
        /// base +-8 m limit and vertices inside no-build locations are left and counted. "auto" = the middle of the
        /// circle's generated relief when it fits that limit (nothing refused; find such a spot with findflatland), else
        /// the median (fewest refusals). Records mark &lt;name&gt; at the centre and the flat height.
        /// </summary>
        private IEnumerator MegaFlatten(List<string> a, int i)
        {
            RequireTerrainWorld();
            var name = Arg(a, i++) ?? throw new Exception("terrain megaflatten <name> <point> <radius> [height|auto]");
            var p = Point(a, ref i);
            var r = Num(Arg(a, i) ?? throw new Exception("megaflatten needs a radius"));
            if (r <= 0f || r > MegaFlattenMaxRadius) throw new Exception($"megaflatten radius must be 0-{MegaFlattenMaxRadius} m");
            var hToken = Arg(a, i + 1);
            var target = string.Equals(hToken, "auto", StringComparison.OrdinalIgnoreCase) ? AutoHeight(p, r) : HeightArg(hToken, p);
            var water = ZoneSystem.instance.m_waterLevel;
            if (target < water + 0.5f) Info($"megaflatten: WARNING target {target:0.00} is at or below the water level {water:0.00}");

            // every zone the circle touches, nearest first
            var zones = new List<Vector2s>();
            for (var x = p.x - r; x <= p.x + r + ZoneSystem.c_ZoneSize; x += ZoneSystem.c_ZoneSize)
            {
                for (var z = p.z - r; z <= p.z + r + ZoneSystem.c_ZoneSize; z += ZoneSystem.c_ZoneSize)
                {
                    var id = ZoneSystem.GetZone(new Vector3(Mathf.Min(x, p.x + r), 0f, Mathf.Min(z, p.z + r)));
                    var c = ZoneSystem.GetZonePos(id);
                    // nearest point of the zone square to the centre must be inside the circle
                    var nx = Mathf.Clamp(p.x, c.x - ZoneSystem.c_ZoneSizeHalf, c.x + ZoneSystem.c_ZoneSizeHalf);
                    var nz = Mathf.Clamp(p.z, c.z - ZoneSystem.c_ZoneSizeHalf, c.z + ZoneSystem.c_ZoneSizeHalf);
                    if (Dist(nx, nz, p) <= r && !zones.Contains(id)) zones.Add(id);
                }
            }

            zones = zones.OrderBy(id => Dist(ZoneSystem.GetZonePos(id).x, ZoneSystem.GetZonePos(id).z, p)).ToList();
            Info($"megaflatten {name}: ({p.x:0.#}, {p.z:0.#}) r {r} -> height {target:0.00}, {zones.Count} zone(s)");

            var done = new HashSet<Vector2s>();
            var removed = new Dictionary<string, int>();
            int vertices = 0, refused = 0, protectedVerts = 0, visits = 0;
            var player = Player.m_localPlayer;
            foreach (var id in zones)
            {
                if (done.Contains(id)) continue;
                if (!ZoneSystem.instance.IsZoneLoaded(id))
                {
                    var c = ZoneSystem.GetZonePos(id);
                    visits++;
                    yield return Teleport(c.x, c.z, 60f);
                    yield return new WaitForSecondsRealtime(1f);
                }

                // everything in the circle that is loaded now goes, then every loaded zone of the list is levelled
                foreach (var kv in TerrainLab.RemoveObjects(p, r, true))
                {
                    removed[kv.Key] = removed.TryGetValue(kv.Key, out var n) ? n + kv.Value : kv.Value;
                }

                foreach (var loaded in zones.Where(z => !done.Contains(z) && ZoneSystem.instance.IsZoneLoaded(z)).ToList())
                {
                    var c = ZoneSystem.GetZonePos(loaded);
                    var res = TerrainLab.Edit(c, ZoneSystem.c_ZoneSizeHalf, (x, z, b, cur) =>
                    {
                        if (Dist(x, z, p) > r) return null;
                        if (Location.IsInsideNoBuildLocation(new Vector3(x, b, z)))
                        {
                            protectedVerts++;
                            return null;
                        }

                        return target;
                    }, null, null, false);
                    vertices += res.Vertices;
                    refused += res.Refused;
                    done.Add(loaded);
                }
            }

            if (visits > 0)
            {
                yield return Teleport(p.x, p.z, 60f);
            }

            ClutterSystem.instance?.ResetGrass(p, r + 2f);
            Marks[name] = new Vector3(p.x, target, p.z);
            TerrainLab.SaveMarks(Marks);
            Info($"megaflatten {name}: removed {Counts(removed)}; {vertices} vertices set to {target:0.00} in {done.Count} zone(s), {visits} zone visit(s)"
                 + (refused > 0 ? $", {refused} REFUSED (beyond base +-{TerrainLab.MaxDelta} m)" : "")
                 + (protectedVerts > 0 ? $", {protectedVerts} in no-build locations left alone" : ""));
            if (player != null && player.m_godMode == false) player.m_maxAirAltitude = player.transform.position.y;
        }

        /// <summary>Generated ground heights inside a circle (4 m grid).</summary>
        private static List<float> GroundHeights(Vector3 p, float r, float step = 4f)
        {
            var heights = new List<float>();
            for (var x = -r; x <= r; x += step)
            {
                for (var z = -r; z <= r; z += step)
                {
                    if (x * x + z * z <= r * r) heights.Add(WorldGenerator.instance.GetHeight(p.x + x, p.z + z));
                }
            }

            return heights;
        }

        /// <summary>
        /// "auto" height: when the relief fits the game's +-8 m edit limit, the middle of min and max (nothing refused);
        /// otherwise the median (fewest refusals).
        /// </summary>
        private static float AutoHeight(Vector3 p, float r)
        {
            var all = GroundHeights(p, r);
            if (all.Count == 0) return p.y;
            all.Sort();
            var lo = all[0];
            var hi = all[all.Count - 1];
            return hi - lo <= 2f * TerrainLab.MaxDelta - 0.2f ? (hi + lo) / 2f : all[all.Count / 2];
        }

        /// <summary>
        /// findflatland &lt;radius&gt; [min] [max] [mark] [biome]: the dry spot (default Meadows centre) min-max m from the
        /// player whose generated ground inside the radius has the least relief; a spot whose relief fits the +-8 m edit
        /// limit (and stays above the sea) is taken at once, so megaflatten ... auto refuses nothing there.
        /// </summary>
        private void FindFlatLand(List<string> a)
        {
            var r = Num(Arg(a, 1) ?? throw new Exception("findflatland <radius> [min] [max] [mark] [biome]"));
            var min = Num(Arg(a, 2, "0"));
            var max = Num(Arg(a, 3, "2000"));
            var markName = Arg(a, 4);
            var biome = (Heightmap.Biome)Enum.Parse(typeof(Heightmap.Biome), Arg(a, 5, "Meadows"), true);
            if (markName != null && Marks.TryGetValue(markName, out var kept))
            {
                Info($"findflatland: mark '{markName}' already set at ({kept.x:0}, {kept.z:0}), kept");
                return;
            }

            var origin = Player.m_localPlayer != null ? Player.m_localPlayer.transform.position : Vector3.zero;
            var water = ZoneSystem.instance.m_waterLevel;
            var limit = 2f * TerrainLab.MaxDelta - 0.2f;
            Vector3? best = null;
            var bestRelief = float.MaxValue;
            var checkedSpots = 0;
            for (var ring = Mathf.Max(min, 0f); ring <= max; ring += 32f)
            {
                var steps = ring < 1f ? 1 : Mathf.Max(8, Mathf.CeilToInt(2f * Mathf.PI * ring / 32f));
                for (var s = 0; s < steps; s++)
                {
                    var angle = s * 2f * Mathf.PI / steps;
                    var c = new Vector3(origin.x + Mathf.Cos(angle) * ring, 0f, origin.z + Mathf.Sin(angle) * ring);
                    if (WorldGenerator.instance.GetBiome(c.x, c.z) != biome) continue;
                    var heights = GroundHeights(c, r, 8f);
                    checkedSpots++;
                    var lo = heights.Min();
                    var hi = heights.Max();
                    if (lo < water + 1f) continue; // sea or lake inside: a flat floor would sit under water
                    var relief = hi - lo;
                    if (relief < bestRelief)
                    {
                        bestRelief = relief;
                        best = new Vector3(c.x, (hi + lo) / 2f, c.z);
                    }

                    if (relief <= limit) goto found;
                }
            }

            found:
            if (!best.HasValue) throw new Exception($"findflatland: no dry {biome} spot {min}-{max} m away ({checkedSpots} checked)");
            var b = best.Value;
            Info($"findflatland r {r}: ({b.x:0}, {b.z:0}) relief {bestRelief:0.0} m (limit {limit:0.0}), {Dist(b.x, b.z, origin):0} m from ({origin.x:0}, {origin.z:0}), {checkedSpots} spot(s) checked"
                 + (bestRelief > limit ? " - NOTHING fits the +-8 m limit: megaflatten will refuse vertices; try a smaller radius" : ""));
            if (markName != null)
            {
                Marks[markName] = b;
                TerrainLab.SaveMarks(Marks);
            }
        }

        /// <summary>
        /// floor &lt;name&gt; &lt;pt&gt; &lt;size&gt; [piece] | floor clear &lt;name&gt;: a size x size grid of floor tiles (default
        /// stone_floor_2x2) centred on the point, laid straight on the ground (Instantiate: no placement sounds), each tile
        /// resting 5 cm into the ground so it counts as grounded. Tiles are not scenario objects: despawn leaves them.
        /// Records mark &lt;name&gt; at the floor's top surface; place/spawn @&lt;name&gt;+dx,dz land on the tiles.
        /// </summary>
        private void Floor(List<string> a)
        {
            if (string.Equals(Arg(a, 1), "clear", StringComparison.OrdinalIgnoreCase))
            {
                var key = Arg(a, 2) ?? throw new Exception("floor clear <name>");
                var n = 0;
                if (_floors.TryGetValue(key, out var tiles))
                {
                    foreach (var go in tiles.Where(t => t != null))
                    {
                        var view = go.GetComponent<ZNetView>();
                        if (view != null && view.IsValid())
                        {
                            view.ClaimOwnership();
                            ZNetScene.instance.Destroy(go);
                            n++;
                        }
                    }

                    _floors.Remove(key);
                }

                Info($"floor clear {key}: removed {n} tile(s)");
                return;
            }

            var i = 2;
            var name = Arg(a, 1) ?? throw new Exception("floor <name> <point> <size> [piece]");
            var p = Point(a, ref i);
            var size = Num(Arg(a, i) ?? throw new Exception("floor needs a size"));
            var pieceName = Arg(a, i + 1) ?? "stone_floor_2x2";
            var prefab = ZNetScene.instance.GetPrefab(pieceName);
            if (prefab == null || prefab.GetComponent<Piece>() == null) throw new Exception("no build piece " + pieceName);
            var ground = TerrainLab.Height(p.x, p.z) ?? p.y;
            var player = Player.m_localPlayer;
            var list = _floors.TryGetValue(name, out var existing) ? existing : _floors[name] = new List<GameObject>();

            // First tile at the centre tells the tile size and how far its pivot sits above its lowest collider.
            var first = UnityEngine.Object.Instantiate(prefab, new Vector3(p.x, ground, p.z), Quaternion.identity);
            var bounds = TileBounds(first);
            var tile = Mathf.Max(0.5f, Mathf.Round(Mathf.Max(bounds.size.x, bounds.size.z) * 2f) / 2f);
            var pivotAboveBottom = first.transform.position.y - bounds.min.y;
            var thickness = bounds.size.y;
            ZNetScene.instance.Destroy(first);

            var count = Mathf.Max(1, Mathf.RoundToInt(size / tile));
            var y = ground - 0.05f + pivotAboveBottom;
            var start = -(count - 1) * tile / 2f;
            var skipped = 0;
            for (var gx = 0; gx < count; gx++)
            {
                for (var gz = 0; gz < count; gz++)
                {
                    var pos = new Vector3(p.x + start + gx * tile, y, p.z + start + gz * tile);
                    var h = TerrainLab.Height(pos.x, pos.z);
                    if (h.HasValue && Mathf.Abs(h.Value - ground) > 0.3f)
                    {
                        skipped++; // not flat here: a floating or buried tile would break or hide
                        continue;
                    }

                    var go = UnityEngine.Object.Instantiate(prefab, pos, Quaternion.identity);
                    var piece = go.GetComponent<Piece>();
                    if (piece != null && player != null) piece.SetCreator(player.GetPlayerID(), default);
                    list.Add(go);
                }
            }

            var top = ground - 0.05f + thickness;
            Marks[name] = new Vector3(p.x, top, p.z);
            TerrainLab.SaveMarks(Marks);
            Info($"floor {name}: {count}x{count} {pieceName} ({tile} m tiles) at ({p.x:0.#}, {p.z:0.#}), ground {ground:0.00}, top {top:0.00}"
                 + (skipped > 0 ? $", {skipped} tile(s) SKIPPED where the ground is not flat (megaflatten first)" : ""));
        }

        private static Bounds TileBounds(GameObject go)
        {
            var colliders = go.GetComponentsInChildren<Collider>().Where(c => c.enabled && !c.isTrigger).ToList();
            if (colliders.Count == 0) return new Bounds(go.transform.position, Vector3.one * 2f);
            var b = colliders[0].bounds;
            foreach (var c in colliders.Skip(1)) b.Encapsulate(c.bounds);
            return b;
        }

        /// <summary>Top surface under a point: terrain, floors and other solid pieces (so pieces land ON a floor).</summary>
        private static float? SurfaceHeight(float x, float z, float fromY)
        {
            var mask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain", "vehicle");
            return Physics.Raycast(new Vector3(x, fromY + 50f, z), Vector3.down, out var hit, 200f, mask, QueryTriggerInteraction.Ignore)
                ? hit.point.y
                : (float?)null;
        }
    }
}
