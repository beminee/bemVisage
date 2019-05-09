using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ensage;
using Ensage.SDK.Abilities;
using Ensage.SDK.Extensions;
using SharpDX;

namespace bemVisage.Abilities
{
    public class visage_summon_familiars_stone_form : ActiveAbility, IAreaOfEffectAbility
    {
        public visage_summon_familiars_stone_form(Ability ability) : base(ability)
        {
        }

        public float Radius
        {
            get { return this.Ability.GetAbilitySpecialData("stun_radius"); }
        }

        public override int GetCastDelay()
        {
            return (int) (this.Ability.GetAbilitySpecialData("stun_delay") * 1000f) + (int) Game.Ping;
        }

        public float StunDuration
        {
            get { return this.Ability.GetAbilitySpecialData("stun_duration") * 1000 - 200; }
        }
    }
}