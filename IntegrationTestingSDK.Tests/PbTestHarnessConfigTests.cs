using IntegrationTestingSDK.Contracts;
using NUnit.Framework;

namespace IntegrationTestingSDK.Tests
{
    // ─────────────────────────────────────────────────────────────────────────
    // P2 — PbTestHarnessConfig defaults
    // ─────────────────────────────────────────────────────────────────────────

    [TestFixture]
    public class PbTestHarnessConfigTests
    {
        private PbTestHarnessConfig _config;

        [SetUp]
        public void SetUp() => _config = new PbTestHarnessConfig();

        [Test]
        public void DefaultApiPort_Is9997()
            => Assert.That(_config.ApiPort, Is.EqualTo(9997));

        [Test]
        public void DefaultApiScheme_IsHttp()
            => Assert.That(_config.ApiScheme, Is.EqualTo("http"));

        [Test]
        public void DefaultApiHost_IsLocalhost()
            => Assert.That(_config.ApiHost, Is.EqualTo("localhost"));

        [Test]
        public void DefaultSessionTimeoutMinutes_Is3()
            => Assert.That(_config.SessionTimeoutMinutes, Is.EqualTo(3));

        [Test]
        public void DefaultHttpTimeoutSeconds_Is30()
            => Assert.That(_config.HttpTimeoutSeconds, Is.EqualTo(30));

        [Test]
        public void DefaultLcdPollTimeoutSeconds_IsPointFive()
            => Assert.That(_config.LcdPollTimeoutSeconds, Is.EqualTo(0.5));

        [Test]
        public void DefaultLcdPollIntervalMs_Is50()
            => Assert.That(_config.LcdPollIntervalMs, Is.EqualTo(50));
    }
}
