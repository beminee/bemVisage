using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ensage;
using Ensage.SDK.Extensions;
using Ensage.SDK.Orbwalker;
using Ensage.SDK.Service;
using SharpDX;

namespace bemVisage
{
    public class FamiliarMovementManager
    {
        public readonly IOrbwalkerManager Orbwalker;

        public FamiliarMovementManager(IOrbwalkerManager orbwalker)
        {
            Orbwalker = orbwalker;
            if (!orbwalker.IsActive)
            {
                orbwalker.Activate();
            }

            orbwalker.Settings.DrawHoldRange.Value = false;
            orbwalker.Settings.DrawRange.Value = false;
        }

        public FamiliarMovementManager(IServiceContext context)
        {
            context.TargetSelector.Activate();

            var orbwalker = context.GetExport<IOrbwalkerManager>().Value;

            orbwalker.Activate();

            orbwalker.Settings.DrawHoldRange.Value = false;
            orbwalker.Settings.DrawRange.Value = false;

            Orbwalker = orbwalker;

            context.TargetSelector.Deactivate();
        }

        public void Orbwalk(Unit target)
        {
            if (target == null)
            {
                Orbwalker.Move(Game.MousePosition);
            }
            else
            {
                if (target.IsAttackImmune())
                {
                    Orbwalker.Move(target.Position);
                }
                else
                    Orbwalker.OrbwalkTo(target);
            }
        }

        public void Attack(Unit target)
        {
            if (target == null)
            {
            }
            else
            {
                if (Orbwalker.CanAttack(target))
                {
                    Orbwalker.Attack(target);
                }
            }
        }

        public void Move(Vector3 pos)
        {
            if (Orbwalker.CanMove())
            {
                Orbwalker.Move(pos);
            }
        }
    }
}