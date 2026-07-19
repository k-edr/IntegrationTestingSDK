using IntegrationTestingSDK.Client;
using NUnit.Framework;

namespace IntegrationTestingSDK.Tests
{
    // ─────────────────────────────────────────────────────────────────────────
    // P2 — ApiRoutes constant values
    // ─────────────────────────────────────────────────────────────────────────

    [TestFixture]
    public class ApiRoutesTests
    {
        [Test]
        public void Health_HasNoLeadingSlash()
            => Assert.That(ApiRoutes.Health, Is.EqualTo("health"));

        [Test]
        public void GridById_FormattedWithId_ProducesExpectedPath()
        {
            var result = string.Format(ApiRoutes.GridById, 12345);
            Assert.That(result, Is.EqualTo("grids/12345"));
        }

        [Test]
        public void BlockAction_FormattedWithGridAndXyz_ProducesExpectedPath()
        {
            var result = string.Format(ApiRoutes.BlockAction, 1, 2, 3, 4);
            Assert.That(result, Is.EqualTo("grids/1/blocks/2/3/4/action"));
        }

        [Test]
        public void BlockProperty_FormattedWithAllArgs_ProducesExpectedPath()
        {
            var result = string.Format(ApiRoutes.BlockProperty, 1, 2, 3, 4, "Color");
            Assert.That(result, Is.EqualTo("grids/1/blocks/2/3/4/properties/Color"));
        }

        [Test]
        public void Grids_ConstantIsGrids()
            => Assert.That(ApiRoutes.Grids, Is.EqualTo("grids"));
    }
}
