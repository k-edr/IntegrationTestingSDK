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
            get
            {
                // SE doesn't have a MaxRange terminal property; use maximum of all extents
                return System.Math.Max(System.Math.Max(LeftExtend, RightExtend), System.Math.Max(BackExtend, FrontExtend));
            }
            set
            {
                // Set all extents to the same value
                LeftExtend = value;
                RightExtend = value;
                BackExtend = value;
                FrontExtend = value;
            }
        }

        public float LeftExtend
        {
            get => float.TryParse(GetProperty("Left"), out var v) ? v : 0f;
            set => SetProperty("Left", value.ToString());
        }

        public float RightExtend
        {
            get => float.TryParse(GetProperty("Right"), out var v) ? v : 0f;
            set => SetProperty("Right", value.ToString());
        }

        public float BottomExtend
        {
            get => float.TryParse(GetProperty("Bottom"), out var v) ? v : 0f;
            set => SetProperty("Bottom", value.ToString());
        }

        public float TopExtend
        {
            get => float.TryParse(GetProperty("Top"), out var v) ? v : 0f;
            set => SetProperty("Top", value.ToString());
        }

        public float FrontExtend
        {
            get => float.TryParse(GetProperty("Front"), out var v) ? v : 0f;
            set => SetProperty("Front", value.ToString());
        }

        public float BackExtend
        {
            get => float.TryParse(GetProperty("Back"), out var v) ? v : 0f;
            set => SetProperty("Back", value.ToString());
        }

        public bool DetectPlayers
        {
            get => bool.TryParse(GetProperty("Detect Players"), out var v) && v;
            set => SetProperty("Detect Players", value.ToString());
        }

        public bool DetectFloatingObjects
        {
            get => bool.TryParse(GetProperty("Detect Floating Objects"), out var v) && v;
            set => SetProperty("Detect Floating Objects", value.ToString());
        }

        public bool DetectSmallShips
        {
            get => bool.TryParse(GetProperty("Detect Small Ships"), out var v) && v;
            set => SetProperty("Detect Small Ships", value.ToString());
        }

        public bool DetectLargeShips
        {
            get => bool.TryParse(GetProperty("Detect Large Ships"), out var v) && v;
            set => SetProperty("Detect Large Ships", value.ToString());
        }

        public bool DetectStations
        {
            get => bool.TryParse(GetProperty("Detect Stations"), out var v) && v;
            set => SetProperty("Detect Stations", value.ToString());
        }

        public bool DetectSubgrids
        {
            get => bool.TryParse(GetProperty("Detect Subgrids"), out var v) && v;
            set => SetProperty("Detect Subgrids", value.ToString());
        }

        public bool DetectAsteroids
        {
            get => bool.TryParse(GetProperty("Detect Asteroids"), out var v) && v;
            set => SetProperty("Detect Asteroids", value.ToString());
        }

        public bool DetectOwner
        {
            get => bool.TryParse(GetProperty("Detect Owner"), out var v) && v;
            set => SetProperty("Detect Owner", value.ToString());
        }

        public bool DetectFriendly
        {
            get => bool.TryParse(GetProperty("Detect Friendly"), out var v) && v;
            set => SetProperty("Detect Friendly", value.ToString());
        }

        public bool DetectNeutral
        {
            get => bool.TryParse(GetProperty("Detect Neutral"), out var v) && v;
            set => SetProperty("Detect Neutral", value.ToString());
        }

        public bool DetectEnemy
        {
            get => bool.TryParse(GetProperty("Detect Enemy"), out var v) && v;
            set => SetProperty("Detect Enemy", value.ToString());
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
