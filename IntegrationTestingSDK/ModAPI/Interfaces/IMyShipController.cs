using IntegrationTestingSDK.ModAPI.Enums;
using IntegrationTestingSDK.ModAPI.Types;

namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Interface for ship controller blocks (cockpits, remote controls, etc.).
    /// </summary>
    public interface IMyShipController : IMyFunctionalBlock
    {
        bool CanControlShip { get; }
        bool IsUnderControl { get; }
        bool ControlThrusters { get; set; }
        bool ControlWheels { get; set; }
        bool HandBrake { get; set; }
        bool DampenersOverride { get; set; }
        bool ShowHorizonIndicator { get; set; }
        bool IsMainCockpit { get; }
        Vector3 MoveIndicator { get; }
        Vector2 RotationIndicator { get; }
        float RollIndicator { get; }
        Vector3D CenterOfMass { get; }

        Vector3 GetNaturalGravity();
        Vector3 GetArtificialGravity();
        Vector3 GetTotalGravity();
        double GetShipSpeed();
        MyShipVelocities GetShipVelocities();
        float CalculateShipMass();
        bool TryGetPlanetPosition(out Vector3D position);
        bool TryGetPlanetElevation(MyPlanetElevation detail, out double elevation);
    }
}
