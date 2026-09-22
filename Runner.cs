using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ClaudeHeim
{
    /// <summary>
    /// Reads a scenario file and runs it line by line. One line = one command; '#' starts a comment; arguments are
    /// separated by spaces, "double quotes" keep spaces together. Every command is isolated: a failing command is
    /// recorded and the scenario goes on, so one broken screen never hides the ones after it.
    /// See README.md for the command list.
    /// </summary>
    internal sealed partial class Runner
    {
        private readonly string _scriptPath;
        private readonly string _outDir;
        private readonly StringBuilder _log = new StringBuilder();
        private readonly List<string> _failures = new List<string>();
        private readonly List<string> _errors = new List<string>();
        private readonly Dictionary<string, int> _errorCounts = new Dictionary<string, int>();
        private readonly Dictionary<string, GameObject> _refs = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
        private readonly List<GameObject> _created = new List<GameObject>();
        private int _shot;
        private int _commands;
        private string _current = "";
        private bool _quitRequested;

        internal bool Started;

        internal Runner(string scriptPath, string outDir)
        {
            _scriptPath = scriptPath;
            _outDir = outDir;
        }

        // ------------------------------------------------------------------ logging

        internal void Info(string message)
        {
            var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            _log.AppendLine(line);
            Plugin.Log.LogInfo(message);
        }

        private void Fail(string message)
        {
            _failures.Add($"line {_current}: {message}");
            var line = $"[{DateTime.Now:HH:mm:ss}] FAIL {message}";
            _log.AppendLine(line);
            Plugin.Log.LogError("FAIL " + message);
        }

        /// <summary>Every error or exception the game logs while the scenario runs, grouped by message + first frames.</summary>
        internal void OnUnityLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
            {
                return;
            }

            // Our own FAIL lines come back through the Unity log; they are already counted.
            if (condition.Contains("[ClaudeHeim]") || condition.StartsWith("FAIL "))
            {
                return;
            }

            var frames = string.Join(" <- ", (stackTrace ?? "").Split('\n').Select(s => s.Trim()).Where(s => s.Length > 0).Take(4));
            var key = condition + " @ " + frames;
            if (_errorCounts.TryGetValue(key, out var count))
            {
                _errorCounts[key] = count + 1;
                return;
            }

            _errorCounts[key] = 1;
            _errors.Add($"[during: {_current}] {key}");
        }

        // ------------------------------------------------------------------ main loop

        internal IEnumerator Run()
        {
            Directory.CreateDirectory(_outDir);
            foreach (var old in Directory.GetFiles(_outDir))
            {
                File.Delete(old);
            }

            string[] lines;
            try
            {
                lines = File.ReadAllLines(_scriptPath);
            }
            catch (Exception e)
            {
                Fail("cannot read scenario: " + e.Message);
                Finish();
                yield break;
            }

            Info($"scenario {_scriptPath} ({lines.Length} lines)");
            yield return new WaitForSecondsRealtime(4f);

            for (var i = 0; i < lines.Length && !_quitRequested; i++)
            {
                var raw = lines[i].Trim();
                if (raw.Length == 0 || raw.StartsWith("#"))
                {
                    continue;
                }

                var args = Tokenize(raw);
                if (args.Count == 0)
                {
                    continue;
                }

                _current = $"{i + 1} '{raw}'";
                _commands++;
                Info($"> {raw}");

                IEnumerator routine = null;
                try
                {
                    routine = Dispatch(args);
                }
                catch (Exception caught)
                {
                    var e = caught;
                    while (e is System.Reflection.TargetInvocationException && e.InnerException != null)
                    {
                        e = e.InnerException;
                    }

                    Fail(e.GetType().Name + ": " + e.Message + " @ " + (e.StackTrace ?? "").Split('\n').FirstOrDefault()?.Trim());
                }

                if (routine != null)
                {
                    // Step the command by hand so an exception inside it is caught here instead of killing the scenario.
                    // Nested IEnumerators are stepped here too (a stack), never handed to Unity, for the same reason.
                    var stack = new Stack<IEnumerator>();
                    stack.Push(routine);
                    while (stack.Count > 0)
                    {
                        object yielded;
                        try
                        {
                            if (!stack.Peek().MoveNext())
                            {
                                stack.Pop();
                                continue;
                            }

                            yielded = stack.Peek().Current;
                            if (yielded is IEnumerator nested)
                            {
                                stack.Push(nested);
                                continue;
                            }
                        }
                        catch (Exception caught)
                        {
                            // Reflection commands wrap the real error; report what the game/mod actually threw.
                            var e = caught;
                            while (e is System.Reflection.TargetInvocationException && e.InnerException != null)
                            {
                                e = e.InnerException;
                            }

                            Fail(e.GetType().Name + ": " + e.Message + " @ " + (e.StackTrace ?? "").Split('\n').FirstOrDefault()?.Trim());
                            break;
                        }

                        yield return yielded;
                    }
                }
            }

            Finish();
            if (_quitRequested)
            {
                yield return new WaitForSecondsRealtime(0.5f);
                Application.Quit();
            }
        }

        private void Finish()
        {
            Info($"finished: {_commands} commands, {_failures.Count} failed, {_errors.Count} distinct game errors, {_shot} screenshots");
            try
            {
                File.WriteAllText(Path.Combine(_outDir, "log.txt"), _log.ToString());
                var sb = new StringBuilder();
                sb.Append("{\n");
                sb.Append($"  \"scenario\": {Json(_scriptPath)},\n  \"commands\": {_commands},\n  \"screenshots\": {_shot},\n");
                sb.Append("  \"failures\": [" + string.Join(", ", _failures.Select(Json)) + "],\n");
                sb.Append("  \"errors\": [" + string.Join(", ", _errors.Select(e => "{\"count\": " + _errorCounts.Where(k => e.EndsWith(k.Key)).Select(k => k.Value).FirstOrDefault() + ", \"text\": " + Json(e) + "}")) + "]\n");
                sb.Append("}\n");
                File.WriteAllText(Path.Combine(_outDir, "result.json"), sb.ToString());
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("could not write results: " + e);
            }
        }

        private static string Json(string s)
        {
            var sb = new StringBuilder("\"");
            foreach (var c in s ?? "")
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append(c < ' ' ? ' ' : c); break;
                }
            }

            return sb.Append('"').ToString();
        }

        private static List<string> Tokenize(string line)
        {
            var result = new List<string>();
            var sb = new StringBuilder();
            var inQuotes = false;
            var had = false;
            foreach (var c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    had = true;
                }
                else if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    if (sb.Length > 0 || had)
                    {
                        result.Add(sb.ToString());
                        sb.Clear();
                        had = false;
                    }
                }
                else if (c == '#' && !inQuotes && sb.Length == 0)
                {
                    break;
                }
                else
                {
                    sb.Append(c);
                }
            }

            if (sb.Length > 0 || had)
            {
                result.Add(sb.ToString());
            }

            return result;
        }

        private static float F(string s) => float.Parse(s, CultureInfo.InvariantCulture);

        private static string Arg(List<string> args, int index, string fallback = null) => index < args.Count ? args[index] : fallback;

        /// <summary>A relative path in a scenario is relative to the scenario file's folder.</summary>
        private string ScenarioRelative(string path) =>
            string.IsNullOrEmpty(path) || Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(_scriptPath), path));
    }
}
