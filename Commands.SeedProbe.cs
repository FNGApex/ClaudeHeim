using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ClaudeHeim
{
    /// <summary>
    /// seedprobe: rank candidate world seeds for a test lab without creating or loading any world. Each seed's
    /// WorldGenerator is built in memory (the same generator a new world uses) and sampled around the world centre, where
    /// the start stones are placed: biome at the centre, and the distance to the nearest ocean and to each biome. The
    /// real spawn point is only known once locations are placed, so check the chosen seed with findbiome afterwards.
    /// </summary>
    internal sealed partial class Runner
    {
        private static readonly Heightmap.Biome[] ProbeBiomes =
        {
            Heightmap.Biome.Meadows, Heightmap.Biome.BlackForest, Heightmap.Biome.Swamp, Heightmap.Biome.Mountain,
            Heightmap.Biome.Plains, Heightmap.Biome.Mistlands, Heightmap.Biome.AshLands, Heightmap.Biome.DeepNorth, Heightmap.Biome.Ocean,
        };

        /// <summary>seedprobe &lt;prefix&gt; &lt;count&gt; [radius] | seedprobe seeds &lt;s1&gt; &lt;s2&gt; ... (main menu only)</summary>
        private void SeedProbe(List<string> a)
        {
            if (Player.m_localPlayer != null) throw new Exception("seedprobe runs in the main menu (it replaces the world generator while it works)");
            List<string> seeds;
            var radius = 3000f;
            if ((Arg(a, 1) ?? "").Equals("seeds", StringComparison.OrdinalIgnoreCase))
            {
                seeds = a.Skip(2).ToList();
            }
            else
            {
                var prefix = Arg(a, 1) ?? "CLAUDELAB";
                var count = int.Parse(Arg(a, 2, "20"));
                radius = F(Arg(a, 3, "3000"));
                // Valheim seeds are up to 10 characters.
                seeds = Enumerable.Range(1, count).Select(n => (prefix + n).Length <= 10 ? prefix + n : prefix.Substring(0, 10 - n.ToString().Length) + n).ToList();
            }

            var previous = WorldGenerator.m_instance;
            var report = new StringBuilder();
            var rows = new List<Tuple<float, string>>();
            try
            {
                foreach (var seed in seeds)
                {
                    var world = new World("claudeheim_probe", seed);
                    WorldGenerator.Initialize(world);
                    var gen = WorldGenerator.instance;
                    var nearest = ProbeBiomes.ToDictionary(b => b, b => float.MaxValue);
                    const float step = 32f;
                    for (var x = -radius; x <= radius; x += step)
                    {
                        for (var z = -radius; z <= radius; z += step)
                        {
                            var d = Mathf.Sqrt(x * x + z * z);
                            if (d > radius) continue;
                            var biome = gen.GetBiome(x, z);
                            if (nearest.TryGetValue(biome, out var best) && d < best) nearest[biome] = d;
                        }
                    }

                    var centre = gen.GetBiome(0f, 0f);
                    // Score: Meadows centre, coast and Black Forest close, the other early biomes within reach.
                    var score = (centre == Heightmap.Biome.Meadows ? 0f : 5000f)
                                + Mathf.Max(0f, nearest[Heightmap.Biome.Ocean] - 250f) * 2f
                                + Mathf.Max(0f, nearest[Heightmap.Biome.BlackForest] - 250f) * 2f
                                + Mathf.Min(nearest[Heightmap.Biome.Swamp], 5000f) + Mathf.Min(nearest[Heightmap.Biome.Mountain], 5000f)
                                + Mathf.Min(nearest[Heightmap.Biome.Plains], 5000f) + Mathf.Min(nearest[Heightmap.Biome.Mistlands], 5000f) * 0.5f;
                    var line = $"{seed,-11} score {score,7:0}  centre {centre,-11} " + string.Join("  ", ProbeBiomes.Select(b => $"{b} {(nearest[b] == float.MaxValue ? "-" : nearest[b].ToString("0"))}"));
                    rows.Add(Tuple.Create(score, line));
                }
            }
            finally
            {
                WorldGenerator.m_instance = previous;
            }

            foreach (var row in rows.OrderBy(r => r.Item1))
            {
                report.AppendLine(row.Item2);
            }

            File.WriteAllText(Path.Combine(_outDir, "seedprobe.txt"), report.ToString());
            Info($"seedprobe: {rows.Count} seed(s), nearest distance (m) from the world centre per biome, best first (seedprobe.txt):\n" + report);
        }
    }
}
