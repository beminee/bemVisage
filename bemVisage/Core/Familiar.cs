using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using bemVisage;
using Ensage;
using Ensage.SDK.Extensions;
using Ensage.SDK.Service;
using log4net;
using PlaySharp.Toolkit.Logging;
using bemVisage.Abilities;

namespace bemVisage.Core
{
    public class Familiar : IEquatable<Familiar>
    {
        public readonly BemVisage Main;
        public Unit Unit;
        public int Handle;
        private static readonly ILog Log = AssemblyLogs.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public FamiliarMovementManager FamiliarMovementManager { get; set; }

        public visage_summon_familiars_stone_form StoneForm { get; set; }

        public Familiar(BemVisage main, Unit familiar)
        {
            Main = main;
            Unit = familiar;
            Handle = familiar.Handle.Handle;
            StoneForm = new visage_summon_familiars_stone_form(familiar.Spellbook.SpellQ);
            FamiliarMovementManager = new FamiliarMovementManager(new EnsageServiceContext(familiar));
        }

        public static bool operator ==(Familiar left, Familiar right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Familiar left, Familiar right)
        {
            return !Equals(left, right);
        }

        public bool Equals(Familiar entity)
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
            return Equals(obj as Familiar);
        }

        public override int GetHashCode()
        {
            return Handle;
        }
    }
}