using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using bemVisage;
using Ensage;
using Ensage.SDK.Helpers;
using SharpDX;

namespace bemVisage
{
    public class LaneHelper
    {
        private readonly BemVisage bemVisage;
        public Map Map;

        public List<Vector3> BotPath { get; set; }
        public List<Vector3> MidPath { get; set; }
        public List<Vector3> TopPath { get; set; }
        public Dictionary<Unit, List<Vector3>> LaneCache;

        public LaneHelper(BemVisage Main)
        {
            bemVisage = Main;
            Map = new Map();
            var isRadiant = ObjectManager.LocalHero.Team == Team.Radiant;
            TopPath = isRadiant ? Map.RadiantTopRoute : Map.DireTopRoute;
            MidPath = isRadiant ? Map.RadiantMiddleRoute : Map.DireMiddleRoute;
            BotPath = isRadiant ? Map.RadiantBottomRoute : Map.DireBottomRoute;
            LaneCache = new Dictionary<Unit, List<Vector3>>();
        }

        public List<Vector3> GetPathCache(Unit unit)
        {
            if (!LaneCache.ContainsKey(unit))
            {
                LaneCache.Add(unit, GetPath(unit));
                UpdateManager.BeginInvoke(() =>
                {
                    LaneCache.Remove(unit);
                }, 150);
            }
            return LaneCache[unit];
        }

        public List<Vector3> GetPath(Unit unit)
        {
            var currentLane = GetLane(unit);
            switch (currentLane)
            {
                case MapArea.Top:
                    return TopPath;
                case MapArea.Middle:
                    return MidPath;
                case MapArea.Bottom:
                    return BotPath;
                case MapArea.DireTopJungle:
                    return TopPath;
                case MapArea.RadiantBottomJungle:
                    return BotPath;
                case MapArea.RadiantTopJungle:
                    return TopPath;
                case MapArea.DireBottomJungle:
                    return BotPath;
                default:
                    return MidPath;
            }
        }

        private MapArea GetLane(Unit unit)
        {
            var lane = GetLane(unit.Position);
            switch (lane)
            {
                case MapArea.Top:
                    return MapArea.Top;
                case MapArea.Middle:
                    return MapArea.Middle;
                case MapArea.Bottom:
                    return MapArea.Bottom;
                case MapArea.DireTopJungle:
                    return MapArea.Top;
                case MapArea.RadiantBottomJungle:
                    return MapArea.Bottom;
                case MapArea.RadiantTopJungle:
                    return MapArea.Top;
                case MapArea.DireBottomJungle:
                    return MapArea.Bottom;
                default:
                    return MapArea.Middle;
            }
        }

        private MapArea GetLane(Vector3 pos)
        {
            if (Map.Top.IsInside(pos))
            {
                return MapArea.Top;
            }
            if (Map.Middle.IsInside(pos))
            {
                return MapArea.Middle;
            }
            if (Map.Bottom.IsInside(pos))
            {
                return MapArea.Bottom;
            }
            if (Map.River.IsInside(pos))
            {
                return MapArea.River;
            }
            if (Map.RadiantBase.IsInside(pos))
            {
                return MapArea.RadiantBase;
            }
            if (Map.DireBase.IsInside(pos))
            {
                return MapArea.DireBase;
            }
            if (Map.Roshan.IsInside(pos))
            {
                return MapArea.RoshanPit;
            }
            if (Map.DireBottomJungle.IsInside(pos))
            {
                return MapArea.DireBottomJungle;
            }
            if (Map.DireTopJungle.IsInside(pos))
            {
                return MapArea.DireTopJungle;
            }
            if (Map.RadiantBottomJungle.IsInside(pos))
            {
                return MapArea.RadiantBottomJungle;
            }
            if (Map.RadiantTopJungle.IsInside(pos))
            {
                return MapArea.RadiantTopJungle;
            }

            return MapArea.Unknown;
        }
    }
}
