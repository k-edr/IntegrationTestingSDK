using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using IntegrationTestingSDK.Plugin.Infrastructure;

namespace IntegrationTestingSDK.Plugin.Application
{
    /// <summary>
    ///     Minimal HTTP server exposing <see cref="PbTestService"/> operations
    ///     for the SDK-side test harness. Listens on port 9980.
    /// </summary>
    internal sealed class HttpApiServer : IDisposable
    {
        private readonly PbTestService _service;
        private HttpListener _listener;
        private Thread _thread;
        private volatile bool _running;

        public HttpApiServer(PbTestService service)
        {
            _service = service;
        }

        public void Start()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://localhost:9980/");

            try
            {
                _listener.Start();
                Logger.Info("HttpApiServer listening on port 9980");
            }
            catch (HttpListenerException ex)
            {
                Logger.Error("HttpApiServer failed to start", ex);
                return;
            }

            _running = true;
            _thread = new Thread(Listen) { IsBackground = true, Name = "HttpApiServer" };
            _thread.Start();
        }

        public void Dispose()
        {
            _running = false;
            try { _listener?.Stop(); } catch { }
            try { _listener?.Close(); } catch { }
        }

        // ── Listen loop ──────────────────────────────────────────

        private void Listen()
        {
            while (_running)
            {
                try
                {
                    var ctx = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => Handle(ctx));
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (Exception ex) when (_running)
                {
                    Logger.Error("HttpApiServer.Listen", ex);
                }
            }
        }

        private void Handle(HttpListenerContext ctx)
        {
            try
            {
                var method = ctx.Request.HttpMethod;
                var path = ctx.Request.Url.AbsolutePath.Trim('/');
                Logger.Info($"HTTP {method} /{path}");

                if (method == "GET" && path == "api/v1/health")
                    HandleHealth(ctx);
                else if (method == "POST" && path == "api/v1/spawn")
                    HandleSpawn(ctx);
                else if (TryMatch(path, "api/v1/grids/{id}", out var m) && method == "DELETE")
                    HandleDelete(ctx, long.Parse(m.Groups[1].Value));
                else if (TryMatch(path, "api/v1/grids/{id}/script", out m) && method == "PUT")
                    HandleUpload(ctx, long.Parse(m.Groups[1].Value));
                else if (TryMatch(path, "api/v1/grids/{id}/run", out m) && method == "POST")
                    HandleRun(ctx, long.Parse(m.Groups[1].Value));
                else if (TryMatch(path, "api/v1/grids/{id}/lcd", out m) && method == "GET")
                    HandleLcd(ctx, long.Parse(m.Groups[1].Value));
                else if (TryMatch(path, "api/v1/grids/{id}/blocks", out m) && method == "GET")
                    HandleBlocks(ctx, long.Parse(m.Groups[1].Value));
                else
                    Write(ctx, 404, "{\"error\":\"not found\"}");
            }
            catch (Exception ex)
            {
                Logger.Error("HttpApiServer.Handle", ex);
                try { Write(ctx, 500, "{\"error\":\"" + Escape(ex.Message) + "\"}"); } catch { }
            }
        }

        // ── Endpoint handlers ────────────────────────────────────

        private void HandleHealth(HttpListenerContext ctx)
        {
            var ready = _service.IsReady;
            Write(ctx, 200, ready ? "{\"ready\":true}" : "{\"ready\":false}");
        }

        private void HandleSpawn(HttpListenerContext ctx)
        {
            var body = ReadBody(ctx);
            var bp = ExtractString(body, "blueprint");
            var x = ExtractDouble(body, "x");
            var y = ExtractDouble(body, "y");
            var z = ExtractDouble(body, "z");

            if (bp == null)
            {
                Write(ctx, 400, "{\"error\":\"blueprint required\"}");
                return;
            }

            var ids = _service.SpawnTestGrid(bp, x, y, z);
            var json = "{\"gridIds\":[" + string.Join(",", ids) + "]}";
            Write(ctx, 200, json);
        }

        private void HandleDelete(HttpListenerContext ctx, long gridId)
        {
            _service.RemoveTestGrid(gridId);
            Write(ctx, 200, "{}");
        }

        private void HandleUpload(HttpListenerContext ctx, long gridId)
        {
            var body = ReadBody(ctx);
            var code = ExtractString(body, "code");
            if (code == null)
            {
                Write(ctx, 400, "{\"error\":\"code required\"}");
                return;
            }

            _service.SetScriptCode(gridId, code);
            Write(ctx, 200, "{}");
        }

        private void HandleRun(HttpListenerContext ctx, long gridId)
        {
            var body = ReadBody(ctx);
            var arg = ExtractString(body, "argument") ?? "";
            var output = _service.RunScript(gridId, arg);
            Write(ctx, 200, "{\"output\":\"" + Escape(output) + "\"}");
        }

        private void HandleLcd(HttpListenerContext ctx, long gridId)
        {
            var content = _service.GetLcdContent(gridId);
            Write(ctx, 200, "{\"content\":\"" + Escape(content ?? "") + "\"}");
        }

        private void HandleBlocks(HttpListenerContext ctx, long gridId)
        {
            var blocks = _service.GetBlockStates(gridId);
            var sb = new StringBuilder("{\"blocks\":[");
            for (int i = 0; i < blocks.Count; i++)
            {
                var b = blocks[i];
                if (i > 0) sb.Append(',');
                sb.Append("{\"entityId\":").Append(b.EntityId);
                sb.Append(",\"type\":\"").Append(Escape(b.Type)).Append("\"");
                sb.Append(",\"subtype\":\"").Append(Escape(b.Subtype)).Append("\"");
                sb.Append(",\"enabled\":").Append(b.Enabled ? "true" : "false");
                sb.Append('}');
            }
            sb.Append("]}");
            Write(ctx, 200, sb.ToString());
        }

        // ── Helpers ──────────────────────────────────────────────

        private static string ReadBody(HttpListenerContext ctx)
        {
            using var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        private static void Write(HttpListenerContext ctx, int code, string json)
        {
            var data = Encoding.UTF8.GetBytes(json);
            ctx.Response.StatusCode = code;
            ctx.Response.ContentType = "application/json";
            ctx.Response.ContentLength64 = data.Length;
            ctx.Response.OutputStream.Write(data, 0, data.Length);
            ctx.Response.OutputStream.Close();
            ctx.Response.Close();
        }

        private static bool TryMatch(string path, string pattern, out Match match)
        {
            var regex = new Regex(
                "^" + Regex.Replace(pattern, @"\{(\w+)\}", @"(?<$1>[^/]+)") + "$",
                RegexOptions.IgnoreCase);
            match = regex.Match(path);
            return match.Success;
        }

        private static string ExtractString(string json, string key)
        {
            var m = Regex.Match(json, "\"" + key + "\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
            return m.Success ? Unescape(m.Groups[1].Value) : null;
        }

        /// <summary>Decode JSON string escape sequences back to real characters.</summary>
        private static string Unescape(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '\\' && i + 1 < s.Length)
                {
                    switch (s[i + 1])
                    {
                        case '"': sb.Append('"'); i++; break;
                        case '\\': sb.Append('\\'); i++; break;
                        case '/':  sb.Append('/');  i++; break;
                        case 'n':  sb.Append('\n'); i++; break;
                        case 'r':  sb.Append('\r'); i++; break;
                        case 't':  sb.Append('\t'); i++; break;
                        case 'b':  sb.Append('\b'); i++; break;
                        case 'f':  sb.Append('\f'); i++; break;
                        default:   sb.Append(s[i]); break;
                    }
                }
                else
                {
                    sb.Append(s[i]);
                }
            }
            return sb.ToString();
        }

        private static double ExtractDouble(string json, string key)
        {
            var m = Regex.Match(json, "\"" + key + "\"\\s*:\\s*(-?\\d+(?:\\.\\d+)?)");
            if (!m.Success) return 0;
            double.TryParse(m.Groups[1].Value,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var v);
            return v;
        }

        private static string Escape(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                    .Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }
    }
}
