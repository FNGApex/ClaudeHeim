using System;
using System.Collections;
using System.IO;
using System.Linq;
using Splatform;
using UnityEngine;

namespace ClaudeHeim
{
    /// <summary>
    /// Test saves: characters and worlds a scenario creates for itself. They are always LOCAL saves (characters_local /
    /// worlds_local), never Steam Cloud, and every one is recorded in testsaves.jsonl in the output folder so the
    /// runner can add it to the machine's test-save registry (see Manage-TestSaves.ps1).
    /// </summary>
    internal sealed partial class Runner
    {
        /// <summary>newchar &lt;name&gt;: create a local character through the game's own New Character flow (forceLocal). Reuses an existing local one; refuses a cloud one.</summary>
        private IEnumerator NewCharacter(string name)
        {
            var fejd = FejdStartup.instance ?? throw new Exception("not in the main menu");
            var existing = SaveSystem.GetAllPlayerProfiles().FirstOrDefault(p => string.Equals(p.GetName(), name, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                if (!existing.m_fileSource.IsLocal())
                {
                    throw new Exception($"character '{name}' exists as a {existing.m_fileSource} save; test characters must be local");
                }

                Info($"newchar {name}: already exists (local), reused");
                RecordTestSave("character", existing.GetFilename(), existing.m_fileSource.ToString(), false);
                yield break;
            }

            fejd.OnStartGame();
            yield return new WaitForSecondsRealtime(1f);
            fejd.OnCharacterNew();
            yield return new WaitForSecondsRealtime(1f);
            fejd.m_csNewCharacterName.text = name;
            fejd.OnNewCharacterDone(true);
            yield return new WaitForSecondsRealtime(1f);

            SaveSystem.InvalidateCache(SaveDataType.Character);
            var created = SaveSystem.GetAllPlayerProfiles().FirstOrDefault(p => string.Equals(p.GetName(), name, StringComparison.OrdinalIgnoreCase))
                          ?? throw new Exception($"newchar {name}: the game did not create it (name taken or invalid?)");
            if (!created.m_fileSource.IsLocal())
            {
                throw new Exception($"newchar {name}: created as {created.m_fileSource}, expected Local");
            }

            Info($"newchar {name}: created as a local save ({created.GetFilename()})");
            RecordTestSave("character", created.GetFilename(), created.m_fileSource.ToString(), true);
        }

        /// <summary>newworld &lt;name&gt; [seed]: create a local world the way the New World panel does (forceLocal). Reuses an existing local one; refuses a cloud one.</summary>
        private IEnumerator NewWorld(string name, string seed)
        {
            if (FejdStartup.instance == null) throw new Exception("not in the main menu");
            var existing = SaveSystem.GetWorldList().FirstOrDefault(w => string.Equals(w.m_name, name, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                if (!existing.m_fileSource.IsLocal())
                {
                    throw new Exception($"world '{name}' exists as a {existing.m_fileSource} save; test worlds must be local");
                }

                Info($"newworld {name}: already exists (local), reused");
                RecordTestSave("world", existing.m_name, existing.m_fileSource.ToString(), false);
                yield break;
            }

            var world = new World(name, string.IsNullOrEmpty(seed) ? World.GenerateSeed() : seed)
            {
                m_fileSource = FileHelpers.FileSource.Local,
                m_needsDB = false,
            };
            var mounted = FileHelpers.CloudStorageSupported && FileHelpers.Mount(SaveDataAccess.ReadWrite);
            try
            {
                AltBiomeWorldData.RemoveCache(name);
                SaveSystem.SetSaveNumber(0u);
                world.SaveWorldFWLData(DateTime.Now);
                SaveSystem.InvalidateCache(SaveDataType.World);
            }
            finally
            {
                if (mounted) FileHelpers.Unmount();
            }

            yield return new WaitForSecondsRealtime(0.5f);
            CreatedThisRun.Add(world.m_name);
            Info($"newworld {name}: created as a local save (seed {world.m_seedName})");
            RecordTestSave("world", world.m_name, world.m_fileSource.ToString(), true);
        }

        /// <summary>selectworld &lt;name&gt;: select a world in the start-game list by name (the list order changes with last play).</summary>
        private void SelectWorld(string name)
        {
            var fejd = FejdStartup.instance ?? throw new Exception("not in the main menu");
            var worlds = fejd.m_worlds ?? throw new Exception("world list not loaded (open the start-game panel first)");
            var index = worlds.FindIndex(w => string.Equals(w.m_name, name, StringComparison.OrdinalIgnoreCase));
            if (index < 0) throw new Exception($"world '{name}' not in the list (have: {string.Join(", ", worlds.Select(w => w.m_name))})");
            fejd.SetSelectedWorld(index, true);
            Info($"selectworld {name}: index {index} ({worlds[index].m_fileSource})");
        }

        private void RecordTestSave(string kind, string name, string source, bool created)
        {
            var line = "{\"kind\":\"" + kind + "\",\"name\":\"" + name.Replace("\"", "") + "\",\"source\":\"" + source +
                       "\",\"created\":" + (created ? "true" : "false") + ",\"at\":\"" + DateTime.Now.ToString("s") + "\"}";
            Directory.CreateDirectory(_outDir);
            File.AppendAllText(Path.Combine(_outDir, "testsaves.jsonl"), line + Environment.NewLine);
        }
    }
}
