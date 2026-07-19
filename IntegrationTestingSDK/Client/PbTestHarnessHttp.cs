using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
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
        private readonly TimeSpan _lcdPollTimeout;
        private readonly TimeSpan _lcdPollInterval;

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

            _apiBase = $"{config.ApiScheme}://{config.ApiHost}:{config.ApiPort}/api/v1";
            _healthPollInterval = TimeSpan.FromSeconds(config.HealthPollIntervalSeconds);
            _sessionTimeout = TimeSpan.FromMinutes(config.SessionTimeoutMinutes);
            _lcdPollTimeout = TimeSpan.FromSeconds(config.LcdPollTimeoutSeconds);
            _lcdPollInterval = TimeSpan.FromMilliseconds(config.LcdPollIntervalMs);
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(config.HttpTimeoutSeconds) };
        }

        // ── URL builder ─────────────────────────────────────────

        private string Url(string route) => $"{_apiBase}/{route}";
        private string Url(string template, params object[] args) =>
            $"{_apiBase}/{string.Format(template, args)}";

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

        public IReadOnlyList<SpawnedGrid> SpawnTestGrid(string blueprintName, double x, double y, double z)
        {
            var result = Post<SpawnResponse>(Url(ApiRoutes.Spawn), new SpawnRequest
            {
                Blueprint = blueprintName,
                Position = new SpawnPosition { X = x, Y = y, Z = z }
            });

            var grids = new List<SpawnedGrid>();
            if (result?.Grids != null)
            {
                foreach (var g in result.Grids)
                    grids.Add(new SpawnedGrid(g.Id, g.Blocks, this));
            }
            return grids;
        }

        public void RemoveTestGrid(long gridId)
        {
            Delete(Url(ApiRoutes.GridById, gridId));
        }

        public void UploadScript(long gridId)
        {
            if (string.IsNullOrEmpty(_scriptCode))
                throw new InvalidOperationException("No script code provided.");

            Put(Url(ApiRoutes.GridScript, gridId), new UploadScriptRequest
            {
                Code = _scriptCode
            });
        }

        public string RunScript(long gridId, string argument = null)
        {
            var body = new RunScriptRequest { Argument = argument ?? "" };
            var result = Post<RunScriptResponse>(Url(ApiRoutes.GridRun, gridId), body);
            return result?.Echo ?? "";
        }

        public string GetLcdContent(long gridId)
        {
            Thread.Sleep(_lcdPollInterval);

            var deadline = DateTime.UtcNow + _lcdPollTimeout;
            while (DateTime.UtcNow < deadline)
            {
                var result = Get<LcdResponse>(Url(ApiRoutes.GridLcd, gridId));
                var content = result?.Content ?? "";

                if (!string.IsNullOrEmpty(content))
                    return content;

                if (DateTime.UtcNow + _lcdPollInterval < deadline)
                    Thread.Sleep(_lcdPollInterval);
            }

            return "";
        }

        public IReadOnlyList<BlockState> GetBlockStates(long gridId)
        {
            var blocks = Get<List<TerminalBlockDto>>(Url(ApiRoutes.GridBlocks, gridId))
                ?? new List<TerminalBlockDto>();

            var result = new List<BlockState>(blocks.Count);
            foreach (var el in blocks)
            {
                var type = "?";
                var subtype = "?";
                var definition = el.Definition ?? "";
                var slashIdx = definition.IndexOf('/');
                if (slashIdx >= 0)
                {
                    type = definition.Substring(0, slashIdx);
                    subtype = definition.Substring(slashIdx + 1);
                }
                else if (!string.IsNullOrEmpty(definition))
                {
                    subtype = definition;
                    type = el.Type ?? "?";
                }

                result.Add(new BlockState
                {
                    EntityId = el.EntityId,
                    Type = type,
                    Subtype = subtype,
                    Enabled = el.IsWorking
                });
            }
            return result;
        }

        // ── Block-level API ────────────────────────────────────────

        public bool ExecuteBlockAction(long gridId, int x, int y, int z, string actionId)
        {
            try
            {
                Post<object>(Url(ApiRoutes.BlockAction, gridId, x, y, z),
                    new BlockActionRequest { ActionId = actionId });
                return true;
            }
            catch
            {
                return false;
            }
        }

        public string GetBlockProperty(long gridId, int x, int y, int z, string propertyId)
        {
            try
            {
                var result = Get<PropertyResponse>(
                    Url(ApiRoutes.BlockProperty, gridId, x, y, z, propertyId));
                return result?.Value;
            }
            catch (InvalidOperationException)
            {
                // Property not found (404), terminal doesn't expose it
                return null;
            }
        }

        public bool SetBlockProperty(long gridId, int x, int y, int z, string propertyId, string value)
        {
            try
            {
                Put(Url(ApiRoutes.BlockProperty, gridId, x, y, z, propertyId),
                    new SetPropertyRequest { Value = value });
                return true;
            }
            catch
            {
                return false;
            }
        }

        public BlockDetailDto GetBlockDetail(long gridId, int x, int y, int z)
        {
            return Get<BlockDetailDto>(Url(ApiRoutes.BlockDetail, gridId, x, y, z));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { StopWorld(); } catch { }
            try { _http.Dispose(); } catch { }
        }

        // ── HTTP helpers (generic) ───────────────────────────────

        private T Get<T>(string url)
        {
            using var cts = new CancellationTokenSource(_http.Timeout);
            var resp = _http.GetAsync(url, cts.Token).GetAwaiter().GetResult();
            EnsureSuccess(resp);
            return resp.Content.ReadFromJsonAsync<T>().GetAwaiter().GetResult();
        }

        private T Post<T>(string url, object body)
        {
            using var cts = new CancellationTokenSource(_http.Timeout);
            var content = JsonContent.Create(body, body.GetType());
            var resp = _http.PostAsync(url, content, cts.Token).GetAwaiter().GetResult();
            EnsureSuccess(resp);
            return resp.Content.ReadFromJsonAsync<T>().GetAwaiter().GetResult();
        }

        private void Put(string url, object body)
        {
            using var cts = new CancellationTokenSource(_http.Timeout);
            var content = JsonContent.Create(body, body.GetType());
            var resp = _http.PutAsync(url, content, cts.Token).GetAwaiter().GetResult();
            EnsureSuccess(resp);
        }

        private void Delete(string url)
        {
            using var cts = new CancellationTokenSource(_http.Timeout);
            var resp = _http.DeleteAsync(url, cts.Token).GetAwaiter().GetResult();
            EnsureSuccess(resp);
        }

        private static void EnsureSuccess(HttpResponseMessage resp)
        {
            if (!resp.IsSuccessStatusCode)
            {
                var body = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
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
                    var resp = _http.GetAsync(Url(ApiRoutes.Health)).Result;
                    var health = resp.Content.ReadFromJsonAsync<JsonElement>().Result;
                    if (health.TryGetProperty("ready", out var ready) && ready.GetBoolean())
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

            var json = JsonSerializer.Serialize(new { worldName });
            File.WriteAllText(configPath, json);
            SdkLog.Info($"AutoWorldLoader config written: {configPath} → {worldName}");
        }
    }
}
