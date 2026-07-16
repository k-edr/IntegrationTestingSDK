using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    ///     talks to the in-game <c>HttpApiServer</c> (port 9980) for spawn/upload/run/lcd.
    /// </summary>
    internal sealed class PbTestHarnessHttp : IPbTestHarness
    {
        private const string ApiBase = "http://localhost:9980/api/v1";
        private static readonly TimeSpan HealthPollInterval = TimeSpan.FromSeconds(2);

        private readonly GameProcessManager _game;
        private readonly WorldManager _worlds;
        private readonly string _scriptCode;
        private readonly HttpClient _http;

        private string _workingCopyName;
        private bool _disposed;

        public PbTestHarnessHttp(
            GameProcessManager game,
            WorldManager worlds,
            string scriptCode)
        {
            _game = game;
            _worlds = worlds;
            _scriptCode = scriptCode;
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        }

        // ── World lifecycle ──────────────────────────────────────

        public void StartWorld(string templateWorldName, string workingCopyName)
        {
            _workingCopyName = workingCopyName;
            _worlds.CopyWorld(templateWorldName, workingCopyName);
            WriteAutoWorldLoaderConfig(workingCopyName);
            _game.Launch(workingCopyName);
            WaitForSession(TimeSpan.FromMinutes(3));
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
            var body = $"{{\"blueprint\":\"{EscapeJson(blueprintName)}\",\"x\":{x},\"y\":{y},\"z\":{z}}}";
            var resp = Post($"{ApiBase}/spawn", body);

            // Parse {gridIds:[1,2,3]}
            using var doc = JsonDocument.Parse(resp);
            var ids = new List<long>();
            if (doc.RootElement.TryGetProperty("gridIds", out var arr))
            {
                foreach (var el in arr.EnumerateArray())
                    ids.Add(el.GetInt64());
            }
            return ids;
        }

        public void RemoveTestGrid(long gridId)
        {
            Delete($"{ApiBase}/grids/{gridId}");
        }

        public void UploadScript(long gridId)
        {
            if (string.IsNullOrEmpty(_scriptCode))
                throw new InvalidOperationException("No script code provided.");

            var body = $"{{\"code\":\"{EscapeJson(_scriptCode)}\"}}";
            Put($"{ApiBase}/grids/{gridId}/script", body);
        }

        public string RunScript(long gridId, string argument = null)
        {
            var body = argument != null
                ? $"{{\"argument\":\"{EscapeJson(argument)}\"}}"
                : "{}";
            var resp = Post($"{ApiBase}/grids/{gridId}/run", body);

            using var doc = JsonDocument.Parse(resp);
            return doc.RootElement.TryGetProperty("output", out var outProp)
                ? outProp.GetString() ?? ""
                : "";
        }

        public string GetLcdContent(long gridId)
        {
            // Give the game a tick to flush any pending WriteText.
            Thread.Sleep(200);

            for (int attempt = 0; attempt < 5; attempt++)
            {
                var resp = Get($"{ApiBase}/grids/{gridId}/lcd");

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
            var resp = Get($"{ApiBase}/grids/{gridId}/blocks");

            using var doc = JsonDocument.Parse(resp);
            var result = new List<BlockState>();
            if (doc.RootElement.TryGetProperty("blocks", out var arr))
            {
                foreach (var el in arr.EnumerateArray())
                {
                    result.Add(new BlockState
                    {
                        EntityId = el.TryGetProperty("entityId", out var id) ? id.GetInt64() : 0,
                        Type = el.TryGetProperty("type", out var t) ? t.GetString() : "?",
                        Subtype = el.TryGetProperty("subtype", out var s) ? s.GetString() : "?",
                        Enabled = el.TryGetProperty("enabled", out var e) && e.GetBoolean()
                    });
                }
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
                    var resp = _http.GetAsync($"{ApiBase}/health").Result;
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

                Thread.Sleep(HealthPollInterval);
            }

            throw new TimeoutException(
                $"Timed out waiting for game session after {timeout.TotalSeconds:N0}s. " +
                "Is AutoWorldLoader + IntegrationTestingSDK.Plugin installed?");
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
