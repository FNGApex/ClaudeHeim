using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace ClaudeHeim
{
    /// <summary>
    /// ClaudeHeim: unattended, scripted testing of Valheim - vanilla or with any set of mods.
    ///
    /// It references the game only. Anything mod specific is driven from the scenario through reflection
    /// commands (set / call / invoke / tab), so the mod under test needs no test code of its own and
    /// ClaudeHeim needs no reference to it.
    ///
    /// The plugin does nothing at all unless the game was started with CLAUDEHEIM=1 - no patches, no
    /// per-frame work - so leaving the dll installed is harmless for normal play.
    ///   CLAUDEHEIM_SCRIPT  path of the scenario file to run (required)
    ///   CLAUDEHEIM_OUT     output folder for screenshots, dumps, log.txt and result.json
    ///                      (default: BepInEx/ClaudeHeim/run)
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("valheim.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.fngapex.claudeheim";
        public const string PluginName = "ClaudeHeim";
        public const string PluginVersion = "0.1.0";

        internal static ManualLogSource Log;
        internal static Plugin Instance;

        private Runner _runner;

        /// <summary>True while the startup logos / main menu are held silent (see Update). Scenario command: audio on|off.</summary>
        internal static bool MuteAudio;
        private bool _logosSkipped;

        private void Awake()
        {
            Log = Logger;
            Instance = this;

            if (Environment.GetEnvironmentVariable("CLAUDEHEIM") != "1")
            {
                Log.LogInfo($"{PluginName} {PluginVersion} dormant (start the game with CLAUDEHEIM=1 to run a scenario).");
                enabled = false;
                return;
            }

            var script = Environment.GetEnvironmentVariable("CLAUDEHEIM_SCRIPT");
            var outDir = Environment.GetEnvironmentVariable("CLAUDEHEIM_OUT");
            if (string.IsNullOrEmpty(outDir))
            {
                outDir = Path.Combine(Paths.BepInExRootPath, "ClaudeHeim", "run");
            }

            if (string.IsNullOrEmpty(script) || !File.Exists(script))
            {
                Log.LogError($"CLAUDEHEIM=1 but CLAUDEHEIM_SCRIPT does not point at a file ('{script}'). Nothing to run.");
                enabled = false;
                return;
            }

            _runner = new Runner(script, outDir);
            Application.logMessageReceived += _runner.OnUnityLog;
            // Unattended launches should not blast the logo / main menu music through the speakers. In-world audio matters
            // for testing, so this lifts by itself the moment the local player exists. CLAUDEHEIM_MENU_AUDIO=1 keeps it on.
            MuteAudio = Environment.GetEnvironmentVariable("CLAUDEHEIM_MENU_AUDIO") != "1";
            Log.LogInfo($"{PluginName} {PluginVersion} armed: scenario '{script}', output '{outDir}'.");
        }

        private void Update()
        {
            if (_runner != null)
            {
                StartupQuiet();
            }

            // The scenario starts once the main menu exists; from there one coroutine carries it across scene loads.
            if (_runner != null && !_runner.Started && FejdStartup.instance != null)
            {
                _runner.Started = true;
                StartCoroutine(_runner.Run());
            }
        }

        private void StartupQuiet()
        {
            // Skip the publisher logos: the loader honours these flags between and during the fades.
            if (!_logosSkipped)
            {
                var loader = FindFirstObjectByType<SceneLoader>();
                if (loader != null)
                {
                    loader._logosSkippable = true;
                    loader._skipAllAtOnce = true;
                    loader._skipEnabled = true;
                    loader._skipped = true;
                    _logosSkipped = true;
                }
            }

            if (!MuteAudio)
            {
                return;
            }

            if (Player.m_localPlayer != null)
            {
                MuteAudio = false;
                Unmute();
                return;
            }

            Silence();
        }

        private void LateUpdate()
        {
            // Again after everyone else's Update/Start this frame: a source started this frame must not get a frame of sound.
            if (_runner != null && MuteAudio)
            {
                Silence();
            }
        }

        private static readonly System.Collections.Generic.HashSet<AudioSource> Muted = new System.Collections.Generic.HashSet<AudioSource>();

        private static void Silence()
        {
            // Every scene's AudioMan.Start puts the listener back to 1, so hold it down per frame...
            AudioListener.volume = 0f;
            // ...and sources can be flagged ignoreListenerVolume in their prefab (menu music), so mute each one as well.
            foreach (var source in FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!source.mute)
                {
                    source.mute = true;
                    Muted.Add(source);
                }
            }
        }

        internal static void Unmute()
        {
            AudioListener.volume = 1f;
            foreach (var source in Muted)
            {
                if (source != null)
                {
                    source.mute = false;
                }
            }

            Muted.Clear();
        }

        private void OnDestroy()
        {
            if (_runner != null)
            {
                Application.logMessageReceived -= _runner.OnUnityLog;
            }
        }
    }
}
