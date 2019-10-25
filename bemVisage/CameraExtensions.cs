using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ensage;
using Ensage.Common.Extensions;
using SharpDX;

namespace bemVisage
{
    public static class CameraExtensions
    {
        public static void PositionCamera(float x, float y)
        {
            var pos = new Vector3(x, y, 256);
            Vector2 screenposVector2;
            if (!Drawing.WorldToScreen(pos, out screenposVector2) && Config.idek)
            {
                Game.ExecuteCommand($"dota_camera_set_lookatpos {x} {y}");
            }
        }
        public static void PositionCamera(Unit unit)
        {
            var x = unit.Position.X;
            var y = unit.Position.Y;
            Vector2 screenposVector2;
            if (!Drawing.WorldToScreen(unit.Position, out screenposVector2) && Config.idek)
            {
                Game.ExecuteCommand($"dota_camera_set_lookatpos {x} {y}");
            }
        }
    }
}
