using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Ensage;
using Ensage.Common.Extensions;
using Ensage.SDK.Service;
using log4net;
using PlaySharp.Toolkit.Logging;

namespace bemVisage.Core
{
    public class OtherUnits : IEquatable<OtherUnits>
    {
        public readonly BemVisage Main;
        public Unit Unit;
        public int Handle;
        private static readonly ILog Log = AssemblyLogs.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public FamiliarMovementManager FamiliarMovementManager { get; set; }

        public Ability Ability { get; set; }
        public Ability Ability2 { get; set; }

        public OtherUnits(BemVisage main, Unit unit)
        {
            Main = main;
            Unit = unit;
            Handle = unit.Handle.Handle;
            Ability = unit.Spellbook.Spells.Count(x => !x.Name.StartsWith("special_") && !x.AbilityBehavior.HasFlag(AbilityBehavior.Passive)) > 0 ? unit.Spellbook.SpellQ : null;
            Ability2 = unit.Spellbook.Spells.Count(x => !x.Name.StartsWith("special_") && !x.AbilityBehavior.HasFlag(AbilityBehavior.Passive) && x.Id != AbilityId.dark_troll_warlord_raise_dead) > 1 ? unit.Spellbook.SpellW : null;
            FamiliarMovementManager = new FamiliarMovementManager(new EnsageServiceContext(unit));
        }

        public static bool operator ==(OtherUnits left, OtherUnits right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(OtherUnits left, OtherUnits right)
        {
            return !Equals(left, right);
        }

        public bool Equals(OtherUnits entity)
        {
            if (entity is null)
            {
                return false;
            }

            if (ReferenceEquals(this, entity))
            {
                return true;
            }

            return Handle == entity.Handle;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as OtherUnits);
        }

        public override int GetHashCode()
        {
            return Handle;
        }

    }
}
