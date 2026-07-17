using IntegrationTestingSDK.ModAPI.Types;

namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Gravity generator interface. Mirrors Sandbox.ModAPI.Ingame.IMyGravityGenerator.
    ///     The float GravityAcceleration from the game is represented as a Vector3
    ///     whose length encodes the magnitude and orientation encodes the direction.
    /// </summary>
    public interface IMyGravityGenerator : IMyFunctionalBlock
    {
        Vector3 GravityAcceleration { get; set; }
        Vector3 FieldSize { get; }
    }
}
