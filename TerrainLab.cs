using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using UnityEngine;

namespace ClaudeHeim
{
    /// <summary>
    /// Exact terrain edits for test worlds (PLAN_TERRAIN_TESTWORLD.md, section A). Works like the hoe underneath - the
    /// per-vertex data of each zone's TerrainComp - but writes it directly: take ownership of the zone's TerrainComp,
    /// set levelDelta = target - base height per vertex (smoothDelta 0), paint per mask texel, then one Save() per zone,
    /// which syncs as the ordinary TerrainComp ZDO data every client loads. Approach as in World Edit Commands
    /// (JereKuusela, Unlicense). No Harmony patches. Limits kept from vanilla: heights stay within base +-8 m (vertices
    /// beyond are refused and counted), and wards / no-build locations are refused like the hoe refuses them.
    /// </summary>
    internal static class TerrainLab
    {
        internal const float MaxDelta = 8f;

        /// <summary>Target height for a vertex: null = leave it, NaN = reset it to generated ground.</summary>
        internal delegate float? HeightRule(float x, float z, float baseHeight, float currentHeight);

        internal sealed class Result
        {
            public int Vertices;
            public int Refused;
            public int Texels;
            public int Zones;

            public override string ToString() => $"{Vertices} vertices, {Texels} paint texels, {Zones} zone(s)" + (Refused > 0 ? $", {Refused} REFUSED (beyond base +-{MaxDelta} m)" : "");
        }

        internal static readonly Color Dirt = new Color(1f, 0f, 0f, 1f);
        internal static readonly Color Cultivated = new Color(0f, 1f, 0f, 1f);
        internal static readonly Color Paved = new Color(0f, 0f, 1f, 1f);

        /// <summary>Checks that every zone the square (center, radius) touches is loaded and not protected.</summary>
        internal static void CheckArea(Vector3 center, float radius)
        {
            foreach (var corner in Corners(center, radius))
            {
                if (!ZoneSystem.instance.IsZoneLoaded(corner))
                {
                    throw new Exception($"zone at ({corner.x:0}, {corner.z:0}) is not loaded - teleport closer first");
                }
            }

            if (!PrivateArea.CheckAccess(center, radius, false, false))
            {
                throw new Exception($"a ward covers ({center.x:0}, {center.z:0}) r {radius:0}");
            }

            if (Location.IsInsideNoBuildLocation(center))
            {
                throw new Exception($"({center.x:0}, {center.z:0}) is inside a no-build location");
            }
        }

        private static IEnumerable<Vector3> Corners(Vector3 c, float r)
        {
            yield return c;
            yield return new Vector3(c.x - r, 0f, c.z - r);
            yield return new Vector3(c.x + r, 0f, c.z - r);
            yield return new Vector3(c.x - r, 0f, c.z + r);
            yield return new Vector3(c.x + r, 0f, c.z + r);
        }

        /// <summary>
        /// One edit over every heightmap within <paramref name="bound"/> of <paramref name="center"/> (square bound).
        /// <paramref name="height"/> decides per vertex; <paramref name="paintArea"/> + <paramref name="paint"/> per
        /// mask texel (paint null = reset the texel to generated ground). <paramref name="checkArea"/> false = edit whatever
        /// heightmaps are loaded (megaflatten walks a large area zone by zone and checks no-build per vertex itself).
        /// </summary>
        internal static Result Edit(Vector3 center, float bound, HeightRule height, Func<float, float, bool> paintArea = null, Color? paint = null, bool checkArea = true)
        {
            if (checkArea) CheckArea(center, bound);
            var result = new Result();
            var heightmaps = new List<Heightmap>();
            Heightmap.FindHeightmap(center, bound * 1.5f + 2f, heightmaps);
            foreach (var hm in heightmaps)
            {
                if (hm == null || hm.m_isDistantLod || hm.m_buildData == null)
                {
                    continue;
                }

                var tc = hm.GetAndCreateTerrainCompiler();
                if (tc == null || tc.m_nview == null || !tc.m_nview.IsValid())
                {
                    continue;
                }

                if (!tc.m_nview.IsOwner())
                {
                    tc.m_nview.ClaimOwnership();
                }

                if (!tc.m_initialized)
                {
                    tc.Initialize();
                }

                var width = hm.m_width;
                var pitch = width + 1;
                var scale = hm.m_scale;
                var origin = hm.transform.position;
                var changed = false;

                if (height != null)
                {
                    for (var y = 0; y < pitch; y++)
                    {
                        for (var x = 0; x < pitch; x++)
                        {
                            var wx = origin.x + (x - width / 2) * scale;
                            var wz = origin.z + (y - width / 2) * scale;
                            if (Mathf.Abs(wx - center.x) > bound + scale || Mathf.Abs(wz - center.z) > bound + scale)
                            {
                                continue;
                            }

                            var index = y * pitch + x;
                            var baseHeight = hm.m_buildData.m_baseHeights[index] + origin.y;
                            var current = baseHeight + tc.m_levelDelta[index] + tc.m_smoothDelta[index];
                            var target = height(wx, wz, baseHeight, current);
                            if (!target.HasValue)
                            {
                                continue;
                            }

                            if (float.IsNaN(target.Value))
                            {
                                tc.m_levelDelta[index] = 0f;
                                tc.m_smoothDelta[index] = 0f;
                                tc.m_modifiedHeight[index] = false;
                            }
                            else
                            {
                                var delta = target.Value - baseHeight;
                                if (Mathf.Abs(delta) > MaxDelta + 0.001f)
                                {
                                    result.Refused++;
                                    continue;
                                }

                                tc.m_levelDelta[index] = delta;
                                tc.m_smoothDelta[index] = 0f;
                                tc.m_modifiedHeight[index] = true;
                            }

                            result.Vertices++;
                            changed = true;
                        }
                    }
                }

                if (paintArea != null)
                {
                    var baseMask = hm.m_buildData.m_baseMask;
                    for (var y = 0; y < pitch; y++)
                    {
                        for (var x = 0; x < pitch; x++)
                        {
                            var p = hm.VertexMaskToWorld(x, y);
                            if (!paintArea(p.x, p.z))
                            {
                                continue;
                            }

                            var index = y * pitch + x;
                            if (paint.HasValue)
                            {
                                var c = paint.Value;
                                // Vanilla keeps the texel's alpha (vegetation mask) when it paints.
                                tc.m_paintMask[index] = new Color(c.r, c.g, c.b, tc.m_paintMask[index].a);
                                tc.m_modifiedPaint[index] = true;
                            }
                            else
                            {
                                tc.m_modifiedPaint[index] = false;
                                if (baseMask != null && index < baseMask.Length)
                                {
                                    tc.m_paintMask[index] = baseMask[index];
                                }
                            }

                            result.Texels++;
                            changed = true;
                        }
                    }
                }

                if (changed)
                {
                    tc.Save();
                    hm.Poke(0);
                    result.Zones++;
                }
            }

            ClutterSystem.instance?.ResetGrass(center, bound + 2f);
            return result;
        }

        /// <summary>Height of the nearest vertex (includes edits once the heightmap has rebuilt).</summary>
        internal static float? Height(float x, float z)
        {
            return Heightmap.GetHeight(new Vector3(x, 0f, z), out var h) ? h : (float?)null;
        }

        // ------------------------------------------------------------------ objects

        /// <summary>
        /// clear: vegetation and loose world objects only. nuke: every networked object except players, the zone's
        /// TerrainComp and its spawn controller. Counts per kind.
        /// </summary>
        internal static Dictionary<string, int> RemoveObjects(Vector3 center, float radius, bool nuke)
        {
            var counts = new Dictionary<string, int>();
            var victims = new List<ZNetView>();
            foreach (var nview in ZNetScene.instance.m_instances.Values)
            {
                if (nview == null || !nview.IsValid())
                {
                    continue;
                }

                var p = nview.transform.position;
                if ((new Vector2(p.x, p.z) - new Vector2(center.x, center.z)).magnitude > radius)
                {
                    continue;
                }

                var kind = Kind(nview.gameObject, nuke);
                if (kind == null)
                {
                    continue;
                }

                counts[kind] = counts.TryGetValue(kind, out var n) ? n + 1 : 1;
                victims.Add(nview);
            }

            foreach (var nview in victims)
            {
                if (nview == null || !nview.IsValid())
                {
                    continue;
                }

                nview.ClaimOwnership();
                ZNetScene.instance.Destroy(nview.gameObject);
            }

            return counts;
        }

        private static string Kind(GameObject go, bool nuke)
        {
            if (go.GetComponent<Player>() != null || go.GetComponent<TerrainComp>() != null || go.GetComponent<SpawnSystem>() != null)
            {
                return null;
            }

            if (go.GetComponent<TreeBase>() != null) return "tree";
            if (go.GetComponent<TreeLog>() != null) return "log";
            if (go.GetComponent<Pickable>() != null) return "pickable";
            if (go.GetComponent<MineRock5>() != null || go.GetComponent<MineRock>() != null) return "rock";
            if (go.GetComponent<ItemDrop>() != null) return "item";
            if (go.GetComponent<Destructible>() != null && go.GetComponent<Piece>() == null) return "destructible";
            if (!nuke) return null;

            if (go.GetComponent<TombStone>() != null) return "tombstone";
            if (go.GetComponent<Ship>() != null) return "ship";
            if (go.GetComponent<Vagon>() != null) return "cart";
            if (go.GetComponent<Character>() != null) return "creature";
            if (go.GetComponent<Piece>() != null) return "piece";
            if (go.GetComponent<LocationProxy>() != null) return "location";
            return "other:" + Utils.GetPrefabName(go);
        }

        // ------------------------------------------------------------------ snapshots

        private static string SnapshotPath(string name)
        {
            var world = ZNet.instance != null ? ZNet.instance.GetWorldName() : "world";
            return Path.Combine(Paths.BepInExRootPath, "ClaudeHeim", "snapshots", $"{world}_{name}.txt");
        }

        /// <summary>Saves the TerrainComp data of every zone touching the area (the only persisted terrain state).</summary>
        internal static int Snapshot(string name, Vector3 center, float radius)
        {
            CheckArea(center, radius);
            var heightmaps = new List<Heightmap>();
            Heightmap.FindHeightmap(center, radius * 1.5f + 2f, heightmaps);
            var lines = new List<string>();
            foreach (var hm in heightmaps.Where(h => h != null && !h.m_isDistantLod))
            {
                var tc = TerrainComp.FindTerrainCompiler(hm.transform.position);
                var data = tc != null && tc.m_nview != null && tc.m_nview.IsValid() ? tc.m_nview.GetZDO().GetByteArray(ZDOVars.s_TCData) : null;
                var pos = hm.transform.position;
                lines.Add($"{pos.x:R} {pos.z:R} {(data != null ? Convert.ToBase64String(data) : "-")}");
            }

            var path = SnapshotPath(name);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllLines(path, lines);
            return lines.Count;
        }

        /// <summary>Puts snapshotted zones back exactly; zones that had no edits get all edits removed.</summary>
        internal static int Restore(string name)
        {
            var path = SnapshotPath(name);
            if (!File.Exists(path))
            {
                throw new Exception("no snapshot " + path);
            }

            var restored = 0;
            foreach (var line in File.ReadAllLines(path))
            {
                var parts = line.Split(' ');
                if (parts.Length < 3)
                {
                    continue;
                }

                var pos = new Vector3(float.Parse(parts[0]), 0f, float.Parse(parts[1]));
                var hm = Heightmap.FindHeightmap(pos);
                if (hm == null)
                {
                    throw new Exception($"zone at ({pos.x:0}, {pos.z:0}) is not loaded - teleport closer first");
                }

                var tc = hm.GetAndCreateTerrainCompiler();
                if (!tc.m_nview.IsOwner())
                {
                    tc.m_nview.ClaimOwnership();
                }

                if (parts[2] == "-")
                {
                    if (!tc.m_initialized)
                    {
                        tc.Initialize();
                    }

                    for (var i = 0; i < tc.m_levelDelta.Length; i++)
                    {
                        tc.m_levelDelta[i] = 0f;
                        tc.m_smoothDelta[i] = 0f;
                        tc.m_modifiedHeight[i] = false;
                        tc.m_modifiedPaint[i] = false;
                    }

                    tc.Save();
                }
                else
                {
                    // Setting the data bumps the ZDO revision; TerrainComp.CheckLoad then reloads it and pokes the heightmap.
                    tc.m_nview.GetZDO().Set(ZDOVars.s_TCData, Convert.FromBase64String(parts[2]));
                }

                hm.Poke(0);
                restored++;
            }

            return restored;
        }

        // ------------------------------------------------------------------ marks

        private static string MarksPath()
        {
            var world = ZNet.instance != null ? ZNet.instance.GetWorldName() : "world";
            return Path.Combine(Paths.BepInExRootPath, "ClaudeHeim", "marks", world + ".txt");
        }

        /// <summary>Named spots per world, kept across runs (BepInEx/ClaudeHeim/marks/&lt;world&gt;.txt).</summary>
        internal static Dictionary<string, Vector3> LoadMarks()
        {
            var marks = new Dictionary<string, Vector3>(StringComparer.OrdinalIgnoreCase);
            var path = MarksPath();
            if (!File.Exists(path))
            {
                return marks;
            }

            foreach (var line in File.ReadAllLines(path))
            {
                var p = line.Split(' ');
                if (p.Length == 4)
                {
                    marks[p[0]] = new Vector3(float.Parse(p[1]), float.Parse(p[2]), float.Parse(p[3]));
                }
            }

            return marks;
        }

        internal static void SaveMarks(Dictionary<string, Vector3> marks)
        {
            var path = MarksPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllLines(path, marks.OrderBy(m => m.Key).Select(m => $"{m.Key} {m.Value.x:R} {m.Value.y:R} {m.Value.z:R}"));
        }
    }
}
