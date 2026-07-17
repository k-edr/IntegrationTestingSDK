using System;
using System.IO;
using System.Xml;
using IntegrationTestingSDK.Infrastructure;

namespace IntegrationTestingSDK.Client
{
    /// <summary>
    ///     Manages world save directories: copy templates to Saves, delete working copies.
    /// </summary>
    internal sealed class WorldManager
    {
        private readonly string _templatesDir;
        private readonly string _savesRoot;

        public WorldManager(string templatesDir)
        {
            _templatesDir = templatesDir;

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var saves = Path.Combine(appData, "SpaceEngineers", "Saves");

            if (!Directory.Exists(saves))
                throw new DirectoryNotFoundException($"Saves directory not found: {saves}");

            var dirs = Directory.GetDirectories(saves);
            if (dirs.Length != 1)
                throw new InvalidOperationException(
                    $"Expected exactly 1 Steam ID folder under Saves, found {dirs.Length}");

            _savesRoot = dirs[0];
            SdkLog.Info($"WorldManager: Saves root = {_savesRoot}");
        }

        /// <summary>
        ///     Copy template world → Saves with a new name.
        /// </summary>
        public void CopyWorld(string templateName, string workingCopyName)
        {
            var templatePath = Path.Combine(_templatesDir, templateName);
            var destPath = Path.Combine(_savesRoot, workingCopyName);

            if (!Directory.Exists(templatePath))
                throw new DirectoryNotFoundException($"Template world not found: {templatePath}");

            if (Directory.Exists(destPath))
            {
                SdkLog.Info($"Removing existing save: {destPath}");
                try { Directory.Delete(destPath, true); }
                catch (Exception ex)
                {
                    SdkLog.Warn($"Failed to delete existing save: {ex.Message}");
                }
            }

            SdkLog.Info($"Copying template: {templatePath} → {destPath}");
            CopyDirectory(templatePath, destPath);
            FixSessionName(destPath, workingCopyName);
            SdkLog.Info($"World copy ready: {destPath}");
        }

        /// <summary>
        ///     Delete a working copy from Saves.
        /// </summary>
        public void DeleteWorld(string worldName)
        {
            var path = Path.Combine(_savesRoot, worldName);
            if (Directory.Exists(path))
            {
                SdkLog.Info($"Deleting world: {path}");
                try { Directory.Delete(path, true); }
                catch (Exception ex)
                {
                    SdkLog.Warn($"Failed to delete world: {ex.Message}");
                }
                SdkLog.Info("World deleted");
            }
            else
            {
                SdkLog.Info($"World not found for cleanup: {path}");
            }
        }

        // ── Internals ──────────────────────────────────────────

        private static void CopyDirectory(string sourceDir, string destDir)
        {
            try
            {
                Directory.CreateDirectory(destDir);

                foreach (var file in Directory.GetFiles(sourceDir))
                {
                    var destFile = Path.Combine(destDir, Path.GetFileName(file));
                    File.Copy(file, destFile, overwrite: true);
                }

                foreach (var dir in Directory.GetDirectories(sourceDir))
                {
                    var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                    CopyDirectory(dir, destSubDir);
                }
            }
            catch (Exception ex)
            {
                SdkLog.Warn($"CopyDirectory failed: {sourceDir} → {destDir}: {ex.Message}");
            }
        }

        private static void FixSessionName(string savePath, string newName)
        {
            foreach (var fileName in new[] { "Sandbox.sbc", "Sandbox_config.sbc" })
            {
                var filePath = Path.Combine(savePath, fileName);
                if (!File.Exists(filePath)) continue;

                try
                {
                    var doc = new XmlDocument();
                    doc.Load(filePath);

                    var nodes = doc.GetElementsByTagName("SessionName");
                    if (nodes.Count > 0)
                    {
                        nodes[0].InnerText = newName;
                        doc.Save(filePath);
                        SdkLog.Info($"  Updated SessionName in {fileName} → {newName}");
                        continue;
                    }

                    nodes = doc.GetElementsByTagName("WorldName");
                    if (nodes.Count > 0)
                    {
                        nodes[0].InnerText = newName;
                        doc.Save(filePath);
                        SdkLog.Info($"  Updated WorldName in {fileName} → {newName}");
                    }
                }
                catch (Exception ex)
                {
                    SdkLog.Error($"  Failed to fix {fileName}", ex);
                }
            }
        }
    }
}
