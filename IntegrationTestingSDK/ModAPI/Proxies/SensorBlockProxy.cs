using System.Collections.Generic;
using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;
using IntegrationTestingSDK.ModAPI.Types;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class SensorBlockProxy : FunctionalBlockProxy, IMySensorBlock
    {
        public float MaxRange
        {
            get => float.TryParse(GetProperty("MaxRange"), out var v) ? v : 50f;
            set => SetProperty("MaxRange", value.ToString());
        }

        public float LeftExtend
        {
            get => float.TryParse(GetProperty("LeftExtend"), out var v) ? v : 0f;
            set => SetProperty("LeftExtend", value.ToString());
        }

        public float RightExtend
        {
            get => float.TryParse(GetProperty("RightExtend"), out var v) ? v : 0f;
            set => SetProperty("RightExtend", value.ToString());
        }

        public float BottomExtend
        {
            get => float.TryParse(GetProperty("BottomExtend"), out var v) ? v : 0f;
            set => SetProperty("BottomExtend", value.ToString());
        }

        public float TopExtend
        {
            get => float.TryParse(GetProperty("TopExtend"), out var v) ? v : 0f;
            set => SetProperty("TopExtend", value.ToString());
        }

        public float FrontExtend
        {
            get => float.TryParse(GetProperty("FrontExtend"), out var v) ? v : 0f;
            set => SetProperty("FrontExtend", value.ToString());
        }

        public float BackExtend
        {
            get => float.TryParse(GetProperty("BackExtend"), out var v) ? v : 0f;
            set => SetProperty("BackExtend", value.ToString());
        }

        public bool DetectPlayers
        {
            get => bool.TryParse(GetProperty("DetectPlayers"), out var v) && v;
            set => SetProperty("DetectPlayers", value.ToString());
        }

        public bool DetectFloatingObjects
        {
            get => bool.TryParse(GetProperty("DetectFloatingObjects"), out var v) && v;
            set => SetProperty("DetectFloatingObjects", value.ToString());
        }

        public bool DetectSmallShips
        {
            get => bool.TryParse(GetProperty("DetectSmallShips"), out var v) && v;
            set => SetProperty("DetectSmallShips", value.ToString());
        }

        public bool DetectLargeShips
        {
            get => bool.TryParse(GetProperty("DetectLargeShips"), out var v) && v;
            set => SetProperty("DetectLargeShips", value.ToString());
        }

        public bool DetectStations
        {
            get => bool.TryParse(GetProperty("DetectStations"), out var v) && v;
            set => SetProperty("DetectStations", value.ToString());
        }

        public bool DetectSubgrids
        {
            get => bool.TryParse(GetProperty("DetectSubgrids"), out var v) && v;
            set => SetProperty("DetectSubgrids", value.ToString());
        }

        public bool DetectAsteroids
        {
            get => bool.TryParse(GetProperty("DetectAsteroids"), out var v) && v;
            set => SetProperty("DetectAsteroids", value.ToString());
        }

        public bool DetectOwner
        {
            get => bool.TryParse(GetProperty("DetectOwner"), out var v) && v;
            set => SetProperty("DetectOwner", value.ToString());
        }

        public bool DetectFriendly
        {
            get => bool.TryParse(GetProperty("DetectFriendly"), out var v) && v;
            set => SetProperty("DetectFriendly", value.ToString());
        }

        public bool DetectNeutral
        {
            get => bool.TryParse(GetProperty("DetectNeutral"), out var v) && v;
            set => SetProperty("DetectNeutral", value.ToString());
        }

        public bool DetectEnemy
        {
            get => bool.TryParse(GetProperty("DetectEnemy"), out var v) && v;
            set => SetProperty("DetectEnemy", value.ToString());
        }

        public bool IsActive
        {
            get => bool.TryParse(GetProperty("IsActive"), out var v) && v;
        }

        public MyDetectedEntityInfo LastDetectedEntity
        {
            get
            {
                var raw = GetProperty("LastDetectedEntity");
                if (string.IsNullOrEmpty(raw)) return new MyDetectedEntityInfo();
                return new MyDetectedEntityInfo
                {
                    EntityId = long.TryParse(raw, out var id) ? id : 0,
                    Name = raw
                };
            }
        }

        public void DetectedEntities(List<MyDetectedEntityInfo> entities)
        {
            if (entities == null) return;
            var last = LastDetectedEntity;
            if (!last.IsEmpty)
                entities.Add(last);
        }

        internal SensorBlockProxy(IPbTestHarness harness, long gridId, BlockDto block) : base(harness, gridId, block) { }
    }
}
