using System;

namespace IntegrationTestingSDK.ModAPI.Types
{
    /// <summary>
    ///     Mirrors <c>VRage.Game.MyDefinitionId</c> for block/filter identification.
    /// </summary>
    public struct MyDefinitionId
    {
        public string TypeId { get; }
        public string SubtypeName { get; }

        public MyDefinitionId(string typeId, string subtypeName)
        {
            TypeId = typeId ?? "";
            SubtypeName = subtypeName ?? "";
        }

        public static bool TryParse(string type, string subtype, out MyDefinitionId id)
        {
            id = new MyDefinitionId(type, subtype);
            return !string.IsNullOrEmpty(subtype);
        }

        public override string ToString() => $"{TypeId}/{SubtypeName}";

        public override bool Equals(object obj)
        {
            if (obj is MyDefinitionId other)
                return string.Equals(TypeId, other.TypeId, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(SubtypeName, other.SubtypeName, StringComparison.OrdinalIgnoreCase);
            return false;
        }

        public override int GetHashCode()
        {
            return (TypeId?.ToLowerInvariant()?.GetHashCode() ?? 0)
                ^ (SubtypeName?.ToLowerInvariant()?.GetHashCode() ?? 0);
        }

        public static bool operator ==(MyDefinitionId a, MyDefinitionId b) => a.Equals(b);
        public static bool operator !=(MyDefinitionId a, MyDefinitionId b) => !a.Equals(b);
    }
}
