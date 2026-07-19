using IntegrationTestingSDK.Contracts;
using NUnit.Framework;

namespace IntegrationTestingSDK.Tests
{
    // ─────────────────────────────────────────────────────────────────────────
    // P2 — LightColor struct
    // ─────────────────────────────────────────────────────────────────────────

    [TestFixture]
    public class LightColorTests
    {
        [Test]
        public void Constructor_ClampsAbove255()
        {
            var c = new LightColor(300, 300, 300);
            Assert.That(c.R, Is.EqualTo(255));
            Assert.That(c.G, Is.EqualTo(255));
            Assert.That(c.B, Is.EqualTo(255));
        }

        [Test]
        public void Constructor_ClampsBelow0()
        {
            var c = new LightColor(-1, -1, -1);
            Assert.That(c.R, Is.EqualTo(0));
            Assert.That(c.G, Is.EqualTo(0));
            Assert.That(c.B, Is.EqualTo(0));
        }

        [Test]
        public void ToPackedString_ThenTryParse_RoundTrips()
        {
            var original = new LightColor(100, 150, 200);
            var packed = original.ToPackedString();
            var ok = LightColor.TryParse(packed, out var parsed);

            Assert.That(ok, Is.True);
            Assert.That(parsed.R, Is.EqualTo(100));
            Assert.That(parsed.G, Is.EqualTo(150));
            Assert.That(parsed.B, Is.EqualTo(200));
        }

        [Test]
        public void TryParse_EmptyString_ReturnsFalse()
            => Assert.That(LightColor.TryParse("", out _), Is.False);

        [Test]
        public void TryParse_NullString_ReturnsFalse()
            => Assert.That(LightColor.TryParse(null, out _), Is.False);

        [Test]
        public void TryParse_NonNumeric_ReturnsFalse()
            => Assert.That(LightColor.TryParse("not-a-number", out _), Is.False);

        [Test]
        public void White_HasAllChannels255()
        {
            var w = LightColor.White;
            Assert.That(w.R, Is.EqualTo(255));
            Assert.That(w.G, Is.EqualTo(255));
            Assert.That(w.B, Is.EqualTo(255));
        }
    }
}
