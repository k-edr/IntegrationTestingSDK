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
            var pluginBin = Path.Combine(reposRoot,
                "IntegrationTestingSDK", "IntegrationTestingSDK.Plugin", "bin", "Debug");

            if (!Directory.Exists(pluginBin))
                throw new DirectoryNotFoundException(
                    $"Plugin output not found: {pluginBin}. Build IntegrationTestingSDK.Plugin first.");

            var pluginDll = Path.Combine(pluginBin, "IntegrationTestingSDK.Plugin.dll");
            if (!File.Exists(pluginDll))
                throw new FileNotFoundException($"Plugin DLL not found: {pluginDll}");

            CopyIfNewer(pluginDll, Path.Combine(pluginsDir, "IntegrationTestingSDK.Plugin.dll"));

            var sdkDll = Path.Combine(reposRoot,
                "IntegrationTestingSDK", "IntegrationTestingSDK",
                "bin", "Debug", "netstandard2.0", "IntegrationTestingSDK.dll");
            if (File.Exists(sdkDll))
                CopyIfNewer(sdkDll, Path.Combine(pluginsDir, "IntegrationTestingSDK.dll"));
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

            var dllPath = Path.Combine(pluginsDir, "IntegrationTestingSDK.Plugin.dll");

            var doc = new XmlDocument { PreserveWhitespace = true };
            doc.Load(configXml);

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
            doc.Save(configXml);

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
        }

        // ── Helpers ───────────────────────────────────────────

        private static void CopyIfNewer(string source, string dest)
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

        private static void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (var file in Directory.GetFiles(sourceDir))
                File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), overwrite: true);
            foreach (var subDir in Directory.GetDirectories(sourceDir))
                CopyDirectory(subDir, Path.Combine(destDir, Path.GetFileName(subDir)));
        }
    }
}
