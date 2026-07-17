namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Spherical gravity generator interface. Mirrors Sandbox.ModAPI.Ingame.IMyGravityGeneratorSphere.
    /// </summary>
    public interface IMyGravityGeneratorSphere : IMyFunctionalBlock
    {
        float Radius { get; set; }
        float GravityAcceleration { get; set; }
    }
}
