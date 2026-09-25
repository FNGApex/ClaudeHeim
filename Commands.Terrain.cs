using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace ClaudeHeim
{
    /// <summary>
    /// Terrain commands for test worlds (see TerrainLab and PLAN_TERRAIN_TESTWORLD.md). Anything that changes the world
    /// only runs in a LOCAL world named in CLAUDEHEIM_TERRAIN_WORLDS, which the runner fills with the active, unprotected,
    /// local worlds of TEST_SAVES.json - so TestWorld and the user's own saves can never be reshaped by a script.
    /// Points: "x z", "@mark", "@player" or "@player+dx,dz".
    /// </summary>
    internal sealed partial class Runner
    {
        private Dictionary<string, Vector3> _marks;

        private Dictionary<string, Vector3> Marks => _marks ?? (_marks = TerrainLab.LoadMarks());

        /// <summary>Worlds this run created with newworld (fresh, local): editable before the registry knows them.</summary>
        internal static readonly HashSet<string> CreatedThisRun = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static void RequireTerrainWorld()
        {
            var world = ZNet.instance != null ? ZNet.instance.GetWorld() : null;
            if (world == null) throw new Exception("not in a world");
            if (!world.m_fileSource.IsLocal()) throw new Exception($"world '{world.m_name}' is a {world.m_fileSource} save; terrain edits only run in local test worlds");
            var allowed = (Environment.GetEnvironmentVariable("CLAUDEHEIM_TERRAIN_WORLDS") ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim());
            if (!allowed.Any(n => string.Equals(n, world.m_name, StringComparison.OrdinalIgnoreCase)) && !CreatedThisRun.Contains(world.m_name))
            {
                throw new Exception($"world '{world.m_name}' is not a registered test world (CLAUDEHEIM_TERRAIN_WORLDS); terrain edits refused");
            }
        }

        private static float Num(string s) => float.Parse(s, CultureInfo.InvariantCulture);

        /// <summary>Reads a point at a[i] ("x z" takes two tokens, "@..." one) and advances i. y = ground height (or the mark's y).</summary>
        private Vector3 Point(List<string> a, ref int i)
        {
            var token = Arg(a, i) ?? throw new Exception("missing point");
            if (token.StartsWith("@"))
            {
                i++;
                // "@name" or "@name+dx,dz" (dx/dz signed): a mark or the player, optionally offset.
                var match = System.Text.RegularExpressions.Regex.Match(token.Substring(1), @"^(.+?)([+-][0-9.]+),([+-]?[0-9.]+)$");
                var name = match.Success ? match.Groups[1].Value : token.Substring(1);
                var offset = match.Success ? new Vector3(Num(match.Groups[2].Value), 0f, Num(match.Groups[3].Value)) : Vector3.zero;
                Vector3 p;
                if (name.Equals("player", StringComparison.OrdinalIgnoreCase))
                {
                    p = (Player.m_localPlayer ?? throw new Exception("no local player")).transform.position;
                }
                else if (!Marks.TryGetValue(name, out p))
                {
                    throw new Exception($"no mark '{name}' (have: {string.Join(", ", Marks.Keys)})");
                }

                if (!match.Success && !name.Equals("player", StringComparison.OrdinalIgnoreCase))
                {
                    return p; // a mark keeps its recorded height
                }

                p += offset;
                return new Vector3(p.x, TerrainLab.Height(p.x, p.z) ?? p.y, p.z);
            }

            var x = Num(token);
            var z = Num(Arg(a, i + 1) ?? throw new Exception("point needs x and z"));
            i += 2;
            return new Vector3(x, TerrainLab.Height(x, z) ?? 0f, z);
        }

        private IEnumerator Terrain(List<string> a)
        {
            var op = (Arg(a, 1) ?? "").ToLowerInvariant();
            var i = 2;
            switch (op)
            {
                case "info":
                    TerrainInfo();
                    break;

                case "height":
                {
                    var p = Point(a, ref i);
                    var h = TerrainLab.Height(p.x, p.z);
                    var biome = WorldGenerator.instance.GetBiome(p.x, p.z);
                    var water = ZoneSystem.instance.m_waterLevel;
                    // grid = heightmap vertex (what TerrainLab writes), ground = raycast on the collider (what placement and
                    // the player stand on), tc = the zone's saved terrain data
                    var ground = ZoneSystem.instance.GetGroundHeight(new Vector3(p.x, 0f, p.z));
                    var tc = TerrainComp.FindTerrainCompiler(new Vector3(p.x, 0f, p.z));
                    var data = tc != null && tc.m_nview != null && tc.m_nview.IsValid() ? tc.m_nview.GetZDO().GetByteArray(ZDOVars.s_TCData) : null;
                    Info($"terrain height ({p.x:0.#}, {p.z:0.#}): grid {(h.HasValue ? h.Value.ToString("0.00") : "zone not loaded")}, ground {ground:0.00}, tc {(tc == null ? "none" : data == null ? "no data" : data.Length + " bytes")}, biome {biome}, water level {water}{(h < water ? " (under water)" : "")}");
                    break;
                }

                case "level":
                case "water":
                {
                    RequireTerrainWorld();
                    var p = Point(a, ref i);
                    var r = Num(Arg(a, i));
                    var target = op == "water" ? ZoneSystem.instance.m_waterLevel - Num(Arg(a, i + 1, "2")) : HeightArg(Arg(a, i + 1), p);
                    var res = TerrainLab.Edit(p, r, (x, z, b, c) => Dist(x, z, p) <= r ? target : (float?)null);
                    Info($"terrain {op} ({p.x:0.#}, {p.z:0.#}) r {r} -> {target:0.00}: {res}");
                    break;
                }

                case "pad":
                {
                    RequireTerrainWorld();
                    var name = Arg(a, i++) ?? throw new Exception("terrain pad <name> <point> <size> [height]");
                    var p = Point(a, ref i);
                    var half = Num(Arg(a, i)) / 2f;
                    var target = HeightArg(Arg(a, i + 1), p);
                    bool FlatAt(float dx, float dz) => Mathf.Abs((TerrainLab.Height(p.x + dx, p.z + dz) ?? -999f) - target) < 0.05f;
                    var corner = half - 0.5f;
                    if (Marks.TryGetValue(name, out var existing) && Mathf.Abs(existing.x - p.x) < 0.01f && Mathf.Abs(existing.z - p.z) < 0.01f
                        && Mathf.Abs(existing.y - target) < 0.05f && FlatAt(0f, 0f)
                        && FlatAt(corner, corner) && FlatAt(-corner, corner) && FlatAt(corner, -corner) && FlatAt(-corner, -corner))
                    {
                        Info($"terrain pad {name}: already in place, skipped");
                        break;
                    }

                    var cleared = TerrainLab.RemoveObjects(p, half * 1.42f + 1f, false);
                    // One extra ring of vertices: the collider interpolates between vertices, so without it the last metre
                    // inside the pad edge slopes toward the untouched ground (seen: -0.7 m at a corner 0.5 m inside).
                    var flatHalf = half + 1f;
                    var res = TerrainLab.Edit(p, flatHalf, (x, z, b, c) => Mathf.Abs(x - p.x) <= flatHalf && Mathf.Abs(z - p.z) <= flatHalf ? target : (float?)null);
                    Marks[name] = new Vector3(p.x, target, p.z);
                    TerrainLab.SaveMarks(Marks);
                    Info($"terrain pad {name} ({p.x:0.#}, {p.z:0.#}) {half * 2f} m at {target:0.00}: {res}; cleared {Counts(cleared)}");
                    break;
                }

                case "raise":
                case "lower":
                {
                    RequireTerrainWorld();
                    var p = Point(a, ref i);
                    var r = Num(Arg(a, i));
                    var delta = Num(Arg(a, i + 1)) * (op == "lower" ? -1f : 1f);
                    var res = TerrainLab.Edit(p, r, (x, z, b, c) => Dist(x, z, p) <= r ? c + delta : (float?)null);
                    Info($"terrain {op} ({p.x:0.#}, {p.z:0.#}) r {r} by {Mathf.Abs(delta)}: {res}");
                    break;
                }

                case "slope":
                {
                    RequireTerrainWorld();
                    var p1 = Point(a, ref i);
                    var h1 = HeightArg(Arg(a, i++), p1);
                    var p2 = Point(a, ref i);
                    var h2 = HeightArg(Arg(a, i++), p2);
                    var halfWidth = Num(Arg(a, i)) / 2f;
                    var from = new Vector2(p1.x, p1.z);
                    var along = new Vector2(p2.x, p2.z) - from;
                    var length = along.magnitude;
                    var dir = along / Mathf.Max(0.001f, length);
                    var mid = (p1 + p2) / 2f;
                    var bound = length / 2f + halfWidth + 1f;
                    var res = TerrainLab.Edit(mid, bound, (x, z, b, c) =>
                    {
                        var v = new Vector2(x, z) - from;
                        var t = Vector2.Dot(v, dir);
                        var side = Mathf.Abs(v.x * dir.y - v.y * dir.x);
                        return t >= 0f && t <= length && side <= halfWidth ? Mathf.Lerp(h1, h2, t / length) : (float?)null;
                    });
                    Info($"terrain slope ({p1.x:0.#}, {p1.z:0.#}) {h1} -> ({p2.x:0.#}, {p2.z:0.#}) {h2}, width {halfWidth * 2f}: {res}");
                    break;
                }

                case "smooth":
                {
                    RequireTerrainWorld();
                    var p = Point(a, ref i);
                    var r = Num(Arg(a, i));
                    var res = TerrainLab.Edit(p, r, (x, z, b, c) =>
                    {
                        if (Dist(x, z, p) > r) return null;
                        var sum = 0f;
                        var n = 0;
                        for (var dx = -1; dx <= 1; dx++)
                        for (var dz = -1; dz <= 1; dz++)
                        {
                            var h = TerrainLab.Height(x + dx, z + dz);
                            if (h.HasValue) { sum += h.Value; n++; }
                        }

                        return n > 0 ? Mathf.Lerp(c, sum / n, 0.5f) : (float?)null;
                    });
                    Info($"terrain smooth ({p.x:0.#}, {p.z:0.#}) r {r}: {res}");
                    break;
                }

                case "paint":
                {
                    RequireTerrainWorld();
                    var p = Point(a, ref i);
                    var r = Num(Arg(a, i));
                    var kind = (Arg(a, i + 1) ?? "").ToLowerInvariant();
                    Color? color;
                    switch (kind)
                    {
                        case "dirt": color = TerrainLab.Dirt; break;
                        case "cultivated": case "cultivate": color = TerrainLab.Cultivated; break;
                        case "paved": color = TerrainLab.Paved; break;
                        case "reset": case "grass": color = null; break;
                        default: throw new Exception("paint kind: dirt|cultivated|paved|reset");
                    }

                    // "square": r is half the side, e.g. a whole cultivated field on a pad.
                    var square = (Arg(a, i + 2) ?? "").Equals("square", StringComparison.OrdinalIgnoreCase);
                    var res = TerrainLab.Edit(p, r, null, (x, z) => square ? Mathf.Abs(x - p.x) <= r && Mathf.Abs(z - p.z) <= r : Dist(x, z, p) <= r, color);
                    Info($"terrain paint ({p.x:0.#}, {p.z:0.#}) {(square ? "square half" : "r")} {r} {kind}: {res}");
                    break;
                }

                case "reset":
                {
                    RequireTerrainWorld();
                    var p = Point(a, ref i);
                    var r = Num(Arg(a, i));
                    var res = TerrainLab.Edit(p, r, (x, z, b, c) => Dist(x, z, p) <= r ? float.NaN : (float?)null, (x, z) => Dist(x, z, p) <= r, null);
                    Info($"terrain reset ({p.x:0.#}, {p.z:0.#}) r {r}: {res}");
                    break;
                }

                case "megaflatten":
                    yield return MegaFlatten(a, i);
                    break;

                case "clear":
                case "nuke":
                {
                    RequireTerrainWorld();
                    var p = Point(a, ref i);
                    var r = Num(Arg(a, i));
                    TerrainLab.CheckArea(p, r);
                    var counts = TerrainLab.RemoveObjects(p, r, op == "nuke");
                    var extra = "";
                    if (op == "nuke")
                    {
                        var res = TerrainLab.Edit(p, r, (x, z, b, c) => Dist(x, z, p) <= r ? float.NaN : (float?)null, (x, z) => Dist(x, z, p) <= r, null);
                        extra = $"; terrain reset: {res}";
                    }

                    Info($"terrain {op} ({p.x:0.#}, {p.z:0.#}) r {r}: removed {Counts(counts)}{extra}");
                    break;
                }

                case "snapshot":
                {
                    RequireTerrainWorld();
                    var name = Arg(a, i++) ?? throw new Exception("terrain snapshot <name> <point> <radius>");
                    var p = Point(a, ref i);
                    Info($"terrain snapshot {name}: {TerrainLab.Snapshot(name, p, Num(Arg(a, i)))} zone(s) saved");
                    break;
                }

                case "restore":
                {
                    RequireTerrainWorld();
                    var name = Arg(a, i) ?? throw new Exception("terrain restore <name>");
                    Info($"terrain restore {name}: {TerrainLab.Restore(name)} zone(s) restored");
                    break;
                }

                default:
                    throw new Exception("terrain info|height|level|pad|raise|lower|slope|smooth|paint|water|reset|clear|nuke|snapshot|restore");
            }

            // Let the heightmaps rebuild before the next command reads heights.
            yield return new WaitForSecondsRealtime(0.5f);
        }

        private static float Dist(float x, float z, Vector3 p) => new Vector2(x - p.x, z - p.z).magnitude;

        /// <summary>A height token: "+1.5" / "-2" = relative to the point's height, "@mark" = a mark's height, anything else = absolute (Valheim heights are positive).</summary>
        private float HeightArg(string token, Vector3 point)
        {
            if (token == null) return point.y;
            if (token.StartsWith("@"))
            {
                // "@mark" = that mark's height, e.g. a pad corner must equal the pad's height
                return Marks.TryGetValue(token.Substring(1), out var mark) ? mark.y : throw new Exception($"no mark '{token.Substring(1)}'");
            }

            return token.StartsWith("+") || token.StartsWith("-") ? point.y + Num(token) : Num(token);
        }

        private static string Counts(Dictionary<string, int> counts) =>
            counts.Count == 0 ? "nothing" : string.Join(", ", counts.OrderByDescending(c => c.Value).Select(c => $"{c.Value} {c.Key}"));

        /// <summary>terrain info: heightmap resolution at the player and every vanilla terrain op prefab with its settings.</summary>
        private void TerrainInfo()
        {
            var player = Player.m_localPlayer;
            var hm = player != null ? Heightmap.FindHeightmap(player.transform.position) : null;
            Info(hm != null ? $"heightmap at player: {hm.name} width {hm.m_width} scale {hm.m_scale} pos {hm.transform.position}" : "no heightmap at the player");
            foreach (var op in ObjectDB.instance.m_terrainOps.Where(o => o != null))
            {
                var s = op.m_settings;
                Info($"  op {op.name}: level {s.m_level} r {s.m_levelRadius} offset {s.m_levelOffset} square {s.m_square} | raise {s.m_raise} r {s.m_raiseRadius} delta {s.m_raiseDelta} | smooth {s.m_smooth} r {s.m_smoothRadius} | paint {s.m_paintType} r {s.m_paintRadius} cleared {s.m_paintCleared}");
            }
        }

        /// <summary>mark &lt;name&gt; [point] - named spot for this world (default: where the player stands).</summary>
        private void Mark(List<string> a)
        {
            var name = Arg(a, 1) ?? throw new Exception("mark <name> [point]");
            Vector3 p;
            if (Arg(a, 2) != null)
            {
                var i = 2;
                p = Point(a, ref i);
            }
            else
            {
                var i = 1;
                p = Point(new List<string> { "", "@player" }, ref i);
            }

            Marks[name] = p;
            TerrainLab.SaveMarks(Marks);
            Info($"mark {name} = ({p.x:0.##}, {p.y:0.##}, {p.z:0.##})");
        }

        /// <summary>
        /// findbiome &lt;Biome&gt; [minDist] [maxDist] [markName] [flatSize] - nearest dry spot of that biome around the player,
        /// in rings. flatSize: the generated ground over that square must vary by less than 6 m (and stay dry), for labs.
        /// </summary>
        private void FindBiome(List<string> a)
        {
            var wanted = (Heightmap.Biome)Enum.Parse(typeof(Heightmap.Biome), Arg(a, 1) ?? throw new Exception("findbiome <Biome> [min] [max] [mark]"), true);
            if (Arg(a, 4) != null && Marks.TryGetValue(Arg(a, 4), out var kept))
            {
                Info($"findbiome {wanted}: mark '{Arg(a, 4)}' already set at ({kept.x:0}, {kept.z:0}), kept");
                return;
            }

            var min = Num(Arg(a, 2, "0"));
            var max = Num(Arg(a, 3, "3000"));
            var origin = Player.m_localPlayer != null ? Player.m_localPlayer.transform.position : Vector3.zero;
            var water = ZoneSystem.instance.m_waterLevel;
            for (var r = Mathf.Max(min, 16f); r <= max; r += 16f)
            {
                var steps = Mathf.Max(8, Mathf.CeilToInt(2f * Mathf.PI * r / 16f));
                for (var s = 0; s < steps; s++)
                {
                    var angle = s * 2f * Mathf.PI / steps;
                    var x = origin.x + Mathf.Cos(angle) * r;
                    var z = origin.z + Mathf.Sin(angle) * r;
                    if (WorldGenerator.instance.GetBiome(x, z) != wanted) continue;
                    var h = WorldGenerator.instance.GetHeight(x, z);
                    if (h < water + 1f) continue;
                    var flat = Arg(a, 5) != null ? Num(Arg(a, 5)) / 2f : 0f;
                    if (flat > 0f && !IsFlat(x, z, flat, wanted, water)) continue;
                    Info($"findbiome {wanted}: ({x:0}, {z:0}) height {h:0.#}, {r:0} m from ({origin.x:0}, {origin.z:0})");
                    var markName = Arg(a, 4);
                    if (markName != null)
                    {
                        Marks[markName] = new Vector3(x, h, z);
                        TerrainLab.SaveMarks(Marks);
                    }

                    return;
                }
            }

            throw new Exception($"no dry {wanted} within {max} m");
        }

        /// <summary>findshore [min] [max] [mark]: nearest beach (dry, at most 4 m above the sea) with water at least
        /// 3 m deep 12 m away; marks the beach as &lt;mark&gt; and the water as &lt;mark&gt;_sea (ships, swimming).</summary>
        private void FindShore(List<string> a)
        {
            var markName = Arg(a, 3);
            if (markName != null && Marks.TryGetValue(markName, out var kept) && Marks.ContainsKey(markName + "_sea"))
            {
                Info($"findshore: mark '{markName}' already set at ({kept.x:0}, {kept.z:0}), kept");
                return;
            }

            var min = Num(Arg(a, 1, "0"));
            var max = Num(Arg(a, 2, "3000"));
            var origin = Player.m_localPlayer != null ? Player.m_localPlayer.transform.position : Vector3.zero;
            var water = ZoneSystem.instance.m_waterLevel;
            for (var r = Mathf.Max(min, 16f); r <= max; r += 8f)
            {
                var steps = Mathf.Max(8, Mathf.CeilToInt(2f * Mathf.PI * r / 8f));
                for (var s = 0; s < steps; s++)
                {
                    var angle = s * 2f * Mathf.PI / steps;
                    var x = origin.x + Mathf.Cos(angle) * r;
                    var z = origin.z + Mathf.Sin(angle) * r;
                    var h = WorldGenerator.instance.GetHeight(x, z);
                    if (h < water + 0.5f || h > water + 4f) continue;
                    for (var d = 0; d < 16; d++)
                    {
                        var da = d * Mathf.PI / 8f;
                        var sx = x + Mathf.Cos(da) * 12f;
                        var sz = z + Mathf.Sin(da) * 12f;
                        // deep enough for a Karve, and deep all around its hull
                        if (WorldGenerator.instance.GetHeight(sx, sz) > water - 3f) continue;
                        if (WorldGenerator.instance.GetHeight(sx + Mathf.Cos(da) * 6f, sz + Mathf.Sin(da) * 6f) > water - 3f) continue;
                        Info($"findshore: beach ({x:0}, {z:0}) height {h:0.#}, sea ({sx:0}, {sz:0}) depth {water - WorldGenerator.instance.GetHeight(sx, sz):0.#}, {r:0} m from ({origin.x:0}, {origin.z:0})");
                        if (markName != null)
                        {
                            Marks[markName] = new Vector3(x, h, z);
                            Marks[markName + "_sea"] = new Vector3(sx, water, sz);
                            TerrainLab.SaveMarks(Marks);
                        }

                        return;
                    }
                }
            }

            throw new Exception($"no beach with deep water within {max} m");
        }

        /// <summary>learn all: the local player knows every material, so every recipe and piece shows (test characters).</summary>
        private void LearnAll()
        {
            var player = Player.m_localPlayer ?? throw new Exception("no local player");
            var added = 0;
            foreach (var go in ObjectDB.instance.m_items)
            {
                var drop = go != null ? go.GetComponent<ItemDrop>() : null;
                if (drop != null && player.m_knownMaterial.Add(drop.m_itemData.m_shared.m_name)) added++;
            }

            // Recipes and pieces are only discovered once their crafting station is known at the needed level.
            var stations = 0;
            foreach (var recipe in ObjectDB.instance.m_recipes)
            {
                foreach (var station in new[] { recipe.m_craftingStation, recipe.m_repairStation })
                {
                    if (station != null && (!player.m_knownStations.TryGetValue(station.m_name, out var level) || level < 10))
                    {
                        player.m_knownStations[station.m_name] = 10;
                        stations++;
                    }
                }
            }

            player.UpdateKnownRecipesList();
            Info($"learn all: {added} materials, {stations} station levels added; {player.m_knownRecipes.Count} recipes and pieces known");
        }

        /// <summary>
        /// expectfail &lt;command...&gt;: passes when the command is refused (throws before its first wait). For negative
        /// tests such as terrain edits outside a registered test world.
        /// </summary>
        private void ExpectFail(List<string> command)
        {
            try
            {
                var routine = Dispatch(command);
                routine?.MoveNext();
            }
            catch (Exception e)
            {
                Info($"expectfail {string.Join(" ", command)}: refused as expected ({e.Message})");
                return;
            }

            throw new Exception($"'{string.Join(" ", command)}' was expected to fail but did not");
        }

        private static bool IsFlat(float x, float z, float half, Heightmap.Biome biome, float water)
        {
            float low = float.MaxValue, high = float.MinValue;
            for (var dx = -half; dx <= half; dx += 4f)
            {
                for (var dz = -half; dz <= half; dz += 4f)
                {
                    if (WorldGenerator.instance.GetBiome(x + dx, z + dz) != biome) return false;
                    var h = WorldGenerator.instance.GetHeight(x + dx, z + dz);
                    if (h < water + 1f) return false;
                    low = Mathf.Min(low, h);
                    high = Mathf.Max(high, h);
                }
            }

            // pads flatten up to 8 m either way, so 6 m of spread still gives flat pads everywhere
            return high - low < 6f;
        }

        /// <summary>expect height &lt;point&gt; &lt;h&gt; [tol] / expect biome &lt;point&gt; &lt;Biome&gt; (called from Expect).</summary>
        private void ExpectTerrain(List<string> a)
        {
            var i = 2;
            var p = Point(a, ref i);
            if (a[1].Equals("ground", StringComparison.OrdinalIgnoreCase))
            {
                var want = HeightArg(Arg(a, i), p);
                var tol = Num(Arg(a, i + 1, "0.05"));
                var g = ZoneSystem.instance.GetGroundHeight(new Vector3(p.x, 0f, p.z));
                if (Mathf.Abs(g - want) > tol) throw new Exception($"ground (collider) at ({p.x:0.#}, {p.z:0.#}) is {g:0.00}, expected {want:0.00} +-{tol}");
            }
            else if (a[1].Equals("height", StringComparison.OrdinalIgnoreCase))
            {
                // no height given = the point's own height (a mark's recorded height)
                var want = HeightArg(Arg(a, i), p);
                var tol = Num(Arg(a, i + 1, "0.05"));
                var h = TerrainLab.Height(p.x, p.z) ?? throw new Exception("zone not loaded");
                if (Mathf.Abs(h - want) > tol) throw new Exception($"height at ({p.x:0.#}, {p.z:0.#}) is {h:0.00}, expected {want:0.00} +-{tol}");
            }
            else
            {
                var want = (Heightmap.Biome)Enum.Parse(typeof(Heightmap.Biome), Arg(a, i), true);
                var biome = WorldGenerator.instance.GetBiome(p.x, p.z);
                if (biome != want) throw new Exception($"biome at ({p.x:0.#}, {p.z:0.#}) is {biome}, expected {want}");
            }
        }
    }
}
