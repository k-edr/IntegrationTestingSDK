namespace IntegrationTestingSDK.Client
{
    /// <summary>
    ///     URL route templates for the GridSpawner API.
    ///     All paths are relative to <c>api/v1</c>.
    ///     Parameterized routes use <c>{0}</c>, <c>{1}</c> etc. for <see cref="string.Format"/>.
    /// </summary>
    internal static class ApiRoutes
    {
        // ── Top-level ─────────────────────────────────────

        public const string Health = "health";

        // ── Blueprints ─────────────────────────────────────

        public const string Blueprints = "blueprints";

        // ── Spawn ──────────────────────────────────────────

        public const string Spawn      = "spawn";
        public const string SpawnTests = "spawn-tests";

        // ── Grids ──────────────────────────────────────────

        /// <summary>GET (list), DELETE (all).</summary>
        public const string Grids = "grids";

        /// <summary>GET, DELETE. <c>{0}</c> = grid entity ID.</summary>
        public const string GridById   = "grids/{0}";
        public const string GridScript = "grids/{0}/script";
        public const string GridRun    = "grids/{0}/run";
        public const string GridLcd    = "grids/{0}/lcd";
        public const string GridBlocks = "grids/{0}/blocks";

        // ── Block actions / properties ─────────────────────

        /// <summary>POST block action. <c>{0}</c>=gridId, <c>{1}</c>=x, <c>{2}</c>=y, <c>{3}</c>=z.</summary>
        public const string BlockAction = "grids/{0}/blocks/{1}/{2}/{3}/action";

        /// <summary>GET/PUT block property. <c>{0}</c>=gridId, <c>{1}</c>=x, <c>{2}</c>=y, <c>{3}</c>=z, <c>{4}</c>=propId.</summary>
        public const string BlockProperty = "grids/{0}/blocks/{1}/{2}/{3}/properties/{4}";

        /// <summary>GET block detail (properties + actions).</summary>
        public const string BlockDetail = "grids/{0}/blocks/{1}/{2}/{3}";
    }
}
