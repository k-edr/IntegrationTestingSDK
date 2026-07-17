using System;
using System.IO;
using IntegrationTestingSDK.Client;
using NUnit.Framework;

namespace IntegrationTestingSDK.Tests
{
    [TestFixture]
    public class GameProcessManagerTests
    {
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"GPM_Test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        [Test]
        public void Constructor_NonExistentDirectory_ThrowsFileNotFoundException()
        {
            var nonExistent = Path.Combine(_tempDir, "does_not_exist");
            Assert.Throws<FileNotFoundException>(() =>
            {
                new GameProcessManager(nonExistent);
            });
        }

        [Test]
        public void Constructor_EmptyDirectory_ThrowsFileNotFoundException()
        {
            Assert.Throws<FileNotFoundException>(() =>
            {
                new GameProcessManager(_tempDir);
            });
        }

        [Test]
        public void Constructor_ValidBin64_Succeeds()
        {
            File.WriteAllText(Path.Combine(_tempDir, "SpaceEngineersLauncher.exe"), "fake");
            Assert.DoesNotThrow(() =>
            {
                var mgr = new GameProcessManager(_tempDir);
                Assert.That(mgr, Is.Not.Null);
            });
        }

        [Test]
        public void Kill_WhenNoProcess_DoesNotThrow()
        {
            File.WriteAllText(Path.Combine(_tempDir, "SpaceEngineersLauncher.exe"), "fake");
            var mgr = new GameProcessManager(_tempDir);
            Assert.DoesNotThrow(() => mgr.Kill());
        }
    }
}
