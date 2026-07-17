using System;
using System.IO;
using System.Xml;
using IntegrationTestingSDK.Infrastructure;

namespace IntegrationTestingSDK.Client
{
    /// <summary>
    ///     Deploys the test harness plugin and its dependencies to the
    ///     game's Plugins folder, registers in PluginLoader's config.xml,
    ///     and ensures blueprints are available.
    ///     <para>
    ///         Called automatically from <see cref="PbTestHarnessFactory.Create"/>
    ///         — no separate build script needed.
    ///     </para>
    /// </summary>
    internal static class PluginDeployer
    {
        /// <summary>
        ///     Deploy everything needed for integration tests to work.
        ///     Idempotent — skips steps that are already done.
        /// </summary>
        /// <param name="seBin64">Path to the game's Bin64 directory.</param>
        public static void Deploy(string seBin64)
        {
            SdkLog.Info("=== PluginDeployer.Deploy ===");

            var pluginsDir = Path.Combine(seBin64, "Plugins");
            if (!Directory.Exists(pluginsDir))
                throw new DirectoryNotFoundException($"Plugins dir not found: {pluginsDir}");

            var reposRoot = ResolveReposRoot();

            CopyPluginDlls(reposRoot, pluginsDir);
            RegisterPlugin(pluginsDir);
            CopyBlueprints(reposRoot);

            SdkLog.Info("=== Deploy complete ===");
        }

        // ── Path resolution ───────────────────────────────────

        /// <summary>
        ///     Resolve the repository root (D:\repos\SE.Plugins\).
        ///     Navigates up from <c>AppDomain.CurrentDomain.BaseDirectory</c>.
        /// </summary>
        private static string ResolveReposRoot()
        {
            var dir = AppDomain.CurrentDomain.BaseDirectory;

            // Walk up until we find a directory named "SE.Plugins"
            for (int i = 0; i < 8; i++)
            {
                if (Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                    .Equals("SE.Plugins", StringComparison.OrdinalIgnoreCase))
                {
                    SdkLog.Info($"Resolved repos root: {dir}");
                    return dir;
                }
                dir = Path.GetFullPath(Path.Combine(dir, ".."));
            }

            throw new DirectoryNotFoundException(
                "Cannot resolve repository root (SE.Plugins). " +
                $"Started from: {AppDomain.CurrentDomain.BaseDirectory}");
        }

        // ── DLL copy ──────────────────────────────────────────

        private static void CopyPluginDlls(string reposRoot, string pluginsDir)
        {
            // GridSpawner.Plugin (MySpawnerController)
            var spawnerPluginBin = Path.Combine(reposRoot,
                "MySpawnerController", "GridSpawner.Plugin", "bin", "Release");

            if (!Directory.Exists(spawnerPluginBin))
            {
                spawnerPluginBin = Path.Combine(reposRoot,
                    "MySpawnerController", "GridSpawner.Plugin", "bin", "Debug");
            }

            if (!Directory.Exists(spawnerPluginBin))
                throw new DirectoryNotFoundException(
                    $"GridSpawner.Plugin output not found: {spawnerPluginBin}. Build MySpawnerController first (build.ps1).");

            var spawnerDll = Path.Combine(spawnerPluginBin, "GridSpawner.Plugin.dll");
            if (!File.Exists(spawnerDll))
                throw new FileNotFoundException($"GridSpawner.Plugin.dll not found: {spawnerDll}");

            // Resolve config folder from the plugin path (Debug or Release)
            var configFolder = Path.GetFileName(spawnerPluginBin.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

            CopyIfNewer(spawnerDll, Path.Combine(pluginsDir, "GridSpawner.Plugin.dll"));

            // GridSpawner.Shared
            var sharedDll = Path.Combine(reposRoot,
                "MySpawnerController", "GridSpawner.Shared", "bin", configFolder, "netstandard2.0", "GridSpawner.Shared.dll");
            if (!File.Exists(sharedDll))
                throw new FileNotFoundException(
                    $"GridSpawner.Shared.dll not found: {sharedDll}. Build MySpawnerController first (build.ps1).");
            CopyIfNewer(sharedDll, Path.Combine(pluginsDir, "GridSpawner.Shared.dll"));

            // GridSpawner.Api
            var apiDll = Path.Combine(reposRoot,
                "MySpawnerController", "GridSpawner.Api", "bin", configFolder, "netstandard2.0", "GridSpawner.Api.dll");
            if (!File.Exists(apiDll))
                throw new FileNotFoundException(
                    $"GridSpawner.Api.dll not found: {apiDll}. Build MySpawnerController first (build.ps1).");
            CopyIfNewer(apiDll, Path.Combine(pluginsDir, "GridSpawner.Api.dll"));
        }

        // ── PluginLoader registration ─────────────────────────

        private static void RegisterPlugin(string pluginsDir)
        {
            var configXml = Path.Combine(pluginsDir, "config.xml");
            if (!File.Exists(configXml))
            {
                SdkLog.Warn("PluginLoader config.xml not found — skipping registration");
                return;
            }

            var dllPath = Path.Combine(pluginsDir, "GridSpawner.Plugin.dll");

            var doc = new XmlDocument { PreserveWhitespace = true };
            try { doc.Load(configXml); }
            catch (Exception ex)
            {
                SdkLog.Warn($"Failed to load config.xml: {ex.Message}");
                return;
            }

            var pluginsNode = doc.SelectSingleNode("//Plugins");
            if (pluginsNode == null)
            {
                SdkLog.Warn("No <Plugins> node in config.xml — skipping registration");
                return;
            }

            foreach (XmlNode node in pluginsNode.SelectNodes("Id"))
            {
                if (string.Equals(node.InnerText, dllPath, StringComparison.OrdinalIgnoreCase))
                {
                    SdkLog.Info("Plugin already registered in config.xml");
                    return;
                }
            }

            var idElem = doc.CreateElement("Id");
            idElem.InnerText = dllPath;
            pluginsNode.AppendChild(idElem);

            try { doc.Save(configXml); }
            catch (Exception ex)
            {
                SdkLog.Warn($"Failed to save config.xml: {ex.Message}");
                return;
            }

            SdkLog.Info($"Registered plugin in config.xml: {dllPath}");
        }

        // ── Blueprints ────────────────────────────────────────

        private static void CopyBlueprints(string reposRoot)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var destRoot = Path.Combine(appData, "SpaceEngineers", "Blueprints", "local");

            var blueprintSource = Path.Combine(reposRoot,
                "MySpawnerController", "GridSpawner.Shared", "Gamedata", "TestBlueprints");

            if (!Directory.Exists(blueprintSource))
            {
                SdkLog.Warn($"Blueprint source not found: {blueprintSource}");
                return;
            }

            foreach (var bpDir in Directory.GetDirectories(blueprintSource))
            {
                try
                {
                    var bpName = Path.GetFileName(bpDir);
                    var destDir = Path.Combine(destRoot, bpName);

                    if (Directory.Exists(destDir))
                    {
                        SdkLog.Info($"Blueprint already exists: {bpName}");
                        continue;
                    }

                    CopyDirectory(bpDir, destDir);
                    SdkLog.Info($"Copied blueprint: {bpName}");
                }
                catch (Exception ex)
                {
                    SdkLog.Warn($"Failed to copy blueprint from {bpDir}: {ex.Message}");
                }
            }
        }

        // ── Helpers ───────────────────────────────────────────

        private static void CopyIfNewer(string source, string dest)
        {
            try
            {
                if (File.Exists(dest))
                {
                    var srcTime = File.GetLastWriteTimeUtc(source);
                    var dstTime = File.GetLastWriteTimeUtc(dest);
                    if (dstTime >= srcTime)
                    {
                        SdkLog.Info($"DLL up to date: {Path.GetFileName(dest)}");
                        return;
                    }
                }

                File.Copy(source, dest, overwrite: true);
                SdkLog.Info($"Copied: {Path.GetFileName(dest)}");
            }
            catch (Exception ex)
            {
                SdkLog.Warn($"Copy failed: {source} → {dest}: {ex.Message}");
            }
        }

        private static void CopyDirectory(string sourceDir, string destDir)
        {
            try
            {
                Directory.CreateDirectory(destDir);

                foreach (var file in Directory.GetFiles(sourceDir))
                    File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), overwrite: true);

                foreach (var subDir in Directory.GetDirectories(sourceDir))
                    CopyDirectory(subDir, Path.Combine(destDir, Path.GetFileName(subDir)));
            }
            catch (Exception ex)
            {
                SdkLog.Warn($"CopyDirectory failed: {sourceDir} → {destDir}: {ex.Message}");
            }
        }
    }
}
