using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.Infrastructure;

namespace IntegrationTestingSDK.Client
{
    /// <summary>
    ///     HTTP-based test harness for integration tests.
    ///     Manages game lifecycle through <see cref="GameProcessManager"/> + <see cref="WorldManager"/>,
    ///     talks to the in-game GridSpawner API for spawn/upload/run/lcd.
    /// </summary>
    internal sealed class PbTestHarnessHttp : IPbTestHarness
    {
        private readonly string _apiBase;
        private readonly TimeSpan _healthPollInterval;
        private readonly TimeSpan _sessionTimeout;

        private readonly GameProcessManager _game;
        private readonly WorldManager _worlds;
        private readonly string _scriptCode;
        private readonly HttpClient _http;

        private string _workingCopyName;
        private bool _disposed;

        public PbTestHarnessHttp(
            GameProcessManager game,
            WorldManager worlds,
            string scriptCode,
            PbTestHarnessConfig config)
        {
            _game = game;
            _worlds = worlds;
            _scriptCode = scriptCode;

            _apiBase = $"http://localhost:{config.ApiPort}/api/v1";
            _healthPollInterval = TimeSpan.FromSeconds(config.HealthPollIntervalSeconds);
            _sessionTimeout = TimeSpan.FromMinutes(config.SessionTimeoutMinutes);
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(config.HttpTimeoutSeconds) };
        }

        // ── World lifecycle ──────────────────────────────────────

        public void StartWorld(string templateWorldName, string workingCopyName)
        {
            _workingCopyName = workingCopyName;
            _worlds.CopyWorld(templateWorldName, workingCopyName);
            WriteAutoWorldLoaderConfig(workingCopyName);
            _game.Launch(workingCopyName);
            WaitForSession(_sessionTimeout);
        }

        public void StopWorld()
        {
            _game.Kill();
            if (_workingCopyName != null)
            {
                _worlds.DeleteWorld(_workingCopyName);
                _workingCopyName = null;
            }
        }

        // ── In-game operations (HTTP) ────────────────────────────

        public IReadOnlyList<long> SpawnTestGrid(string blueprintName, double x, double y, double z)
        {
            // TODO: Replace manual JSON with models
            var body = $"{{\"blueprint\":\"{EscapeJson(blueprintName)}\",\"position\":{{\"x\":{x},\"y\":{y},\"z\":{z}}}}}";
            var resp = Post($"{_apiBase}/spawn", body);

            using var doc = JsonDocument.Parse(resp);
            var ids = new List<long>();
            if (doc.RootElement.TryGetProperty("grids", out var arr))
            {
                foreach (var el in arr.EnumerateArray())
                {
                    if (el.TryGetProperty("id", out var idProp))
                        ids.Add(idProp.GetInt64());
                }
            }
            return ids;
        }

        public void RemoveTestGrid(long gridId)
        {
            Delete($"{_apiBase}/grids/{gridId}");
        }

        public void UploadScript(long gridId)
        {
            if (string.IsNullOrEmpty(_scriptCode))
                throw new InvalidOperationException("No script code provided.");

            // TODO: Replace manual JSON with models
            var body = $"{{\"code\":\"{EscapeJson(_scriptCode)}\"}}";
            Put($"{_apiBase}/grids/{gridId}/script", body);
        }

        public string RunScript(long gridId, string argument = null)
        {
            // TODO: Replace manual JSON with models
            var body = argument != null
                ? $"{{\"argument\":\"{EscapeJson(argument)}\"}}"
                : "{}";
            var resp = Post($"{_apiBase}/grids/{gridId}/run", body);

            using var doc = JsonDocument.Parse(resp);
            return doc.RootElement.TryGetProperty("echo", out var echoProp)
                ? echoProp.GetString() ?? ""
                : "";
        }

        // TODO: Rework without Thread.Sleep — poll with a proper timeout instead
        public string GetLcdContent(long gridId)
        {
            // Give the game a tick to flush any pending WriteText.
            Thread.Sleep(200);

            for (int attempt = 0; attempt < 5; attempt++)
            {
                var resp = Get($"{_apiBase}/grids/{gridId}/lcd");

                using var doc = JsonDocument.Parse(resp);
                var content = doc.RootElement.TryGetProperty("content", out var cProp)
                    ? cProp.GetString() ?? ""
                    : "";

                if (!string.IsNullOrEmpty(content))
                    return content;

                if (attempt < 4)
                    Thread.Sleep(50);
            }

            return "";
        }

        public IReadOnlyList<BlockState> GetBlockStates(long gridId)
        {
            var resp = Get($"{_apiBase}/grids/{gridId}/blocks");

            using var doc = JsonDocument.Parse(resp);
            var result = new List<BlockState>();
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                var definition = el.TryGetProperty("definition", out var d) ? d.GetString() ?? "" : "";
                var type = "?";
                var subtype = "?";

                // TerminalBlockDto.Type/Definition is "MyObjectBuilder_Reactor/SmallBlockSmallGenerator"
                var slashIdx = definition.IndexOf('/');
                if (slashIdx >= 0)
                {
                    type = definition.Substring(0, slashIdx);
                    subtype = definition.Substring(slashIdx + 1);
                }
                else if (!string.IsNullOrEmpty(definition))
                {
                    subtype = definition;
                    type = el.TryGetProperty("type", out var t) ? t.GetString() ?? "?" : "?";
                }

                result.Add(new BlockState
                {
                    EntityId = el.TryGetProperty("entityId", out var id) ? id.GetInt64() : 0,
                    Type = type,
                    Subtype = subtype,
                    Enabled = el.TryGetProperty("isWorking", out var ew) && ew.GetBoolean()
                });
            }
            return result;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { StopWorld(); } catch { }
            try { _http.Dispose(); } catch { }
        }

        // ── HTTP helpers ─────────────────────────────────────────

        private string Get(string url)
        {
            var resp = _http.GetAsync(url).Result;
            var body = resp.Content.ReadAsStringAsync().Result;
            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"HTTP {resp.StatusCode}: {body}");
            return body;
        }

        private string Post(string url, string json)
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp = _http.PostAsync(url, content).Result;
            var body = resp.Content.ReadAsStringAsync().Result;
            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"HTTP {resp.StatusCode}: {body}");
            return body;
        }

        private string Put(string url, string json)
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp = _http.PutAsync(url, content).Result;
            var body = resp.Content.ReadAsStringAsync().Result;
            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"HTTP {resp.StatusCode}: {body}");
            return body;
        }

        private void Delete(string url)
        {
            var resp = _http.DeleteAsync(url).Result;
            if (!resp.IsSuccessStatusCode)
            {
                var body = resp.Content.ReadAsStringAsync().Result;
                throw new InvalidOperationException($"HTTP {resp.StatusCode}: {body}");
            }
        }

        // ── Startup helpers ──────────────────────────────────────

        private void WaitForSession(TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            SdkLog.Info($"Waiting for game session via HTTP (timeout: {timeout.TotalSeconds:N0}s)...");

            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    var resp = _http.GetAsync($"{_apiBase}/health").Result;
                    var body = resp.Content.ReadAsStringAsync().Result;
                    if (body.Contains("\"ready\":true"))
                    {
                        var elapsed = timeout.TotalSeconds - (deadline - DateTime.UtcNow).TotalSeconds;
                        SdkLog.Info($"Session ready after {elapsed:N0}s");
                        return;
                    }
                }
                catch
                {
                    // Server not up yet, keep polling
                }

                Thread.Sleep(_healthPollInterval);
            }

            throw new TimeoutException(
                $"Timed out waiting for game session after {timeout.TotalSeconds:N0}s. " +
                "Is AutoWorldLoader + GridSpawner.Plugin installed?");
        }

        private static void WriteAutoWorldLoaderConfig(string worldName)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var configPath = Path.Combine(appData, "SpaceEngineers", "AutoWorldLoader.json");
            var dir = Path.GetDirectoryName(configPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = $"{{\"worldName\": \"{worldName}\"}}";
            File.WriteAllText(configPath, json);
            SdkLog.Info($"AutoWorldLoader config written: {configPath} → {worldName}");
        }

        private static string EscapeJson(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                    .Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }
    }
}
