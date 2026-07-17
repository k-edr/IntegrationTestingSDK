namespace IntegrationTestingSDK.ModAPI.Enums
{
    /// <summary>Door status matching Sandbox.ModAPI.Ingame.DoorStatus.</summary>
    public enum DoorStatus
    {
        Opening,
        Open,
        Closing,
        Closed
    }

    /// <summary>Battery charge mode matching Sandbox.ModAPI.Ingame.ChargeMode.</summary>
    public enum ChargeMode
    {
        Auto,
        Recharge,
        Discharge
    }

    /// <summary>Remote control flight mode matching Sandbox.ModAPI.Ingame.FlightMode.</summary>
    public enum FlightMode
    {
        Patrol,
        Circle,
        OneWay,
        Track,
        WorkArea
    }

    /// <summary>Piston status matching Sandbox.ModAPI.Ingame.PistonStatus.</summary>
    public enum PistonStatus
    {
        Stopped,
        Extending,
        Extended,
        Retracting,
        Retracted
    }

    /// <summary>Connector status matching Sandbox.ModAPI.Ingame.MyShipConnectorStatus.</summary>
    public enum MyShipConnectorStatus
    {
        Unconnected,
        Connectable,
        Connected
    }

    /// <summary>Jump drive status matching Sandbox.ModAPI.Ingame.MyJumpDriveStatus.</summary>
    public enum MyJumpDriveStatus
    {
        Charging,
        Ready,
        Jumping
    }

    /// <summary>Laser antenna status matching Sandbox.ModAPI.Ingame.MyLaserAntennaStatus.</summary>
    public enum MyLaserAntennaStatus
    {
        Idle,
        RotatingToTarget,
        SearchingTargetForAntenna,
        Connecting,
        Connected,
        OutOfRange
    }

    /// <summary>Conveyor sorter mode matching Sandbox.ModAPI.Ingame.MyConveyorSorterMode.</summary>
    public enum MyConveyorSorterMode
    {
        Whitelist,
        Blacklist
    }

    /// <summary>Rotation direction matching Sandbox.ModAPI.Ingame.MyRotationDirection.</summary>
    public enum MyRotationDirection
    {
        AUTO,
        CW,
        CCW
    }

    /// <summary>Vent status matching Space Engineers in-game air vent.</summary>
    public enum VentStatus
    {
        Depressurized,
        Depressurizing,
        Pressurized,
        Pressurizing
    }

    /// <summary>Merge block state matching Space Engineers in-game merge block.</summary>
    public enum MergeState
    {
        Unset,
        None,
        Working,
        Constrained,
        Locked
    }

    /// <summary>Detected entity type matching Sandbox.ModAPI.Ingame.MyDetectedEntityType.</summary>
    public enum MyDetectedEntityType
    {
        None,
        Unknown,
        SmallGrid,
        LargeGrid,
        CharacterHuman,
        CharacterOther,
        FloatingObject,
        Asteroid,
        Planet,
        Meteor,
        Missile,
        Tree,
        Forageable
    }

    /// <summary>Landing gear mode matching Sandbox.ModAPI.Ingame.LandingGearMode.</summary>
    public enum LandingGearMode
    {
        ReadyToLock,
        Locked,
        Unlocking
    }

    /// <summary>Broadcast target matching Space Engineers in-game antenna/beacon.</summary>
    public enum BroadcastTarget
    {
        Owner,
        Faction,
        Everyone
    }

    /// <summary>Planet elevation reference matching Sandbox.ModAPI.Ingame.MyPlanetElevation.</summary>
    public enum MyPlanetElevation
    {
        Sealevel,
        Surface
    }

    /// <summary>Targeting group options matching Space Engineers in-game targeting systems.</summary>
    public enum TargetingGroupOptions
    {
        Default,
        Weapons,
        Propulsion,
        Power
    }
}
