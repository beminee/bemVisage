using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using bemVisage;
using Ensage;
using Ensage.Common.Extensions;
using Ensage.Common.Menu;
using Ensage.SDK.Handlers;
using Ensage.SDK.Helpers;
using Ensage.SDK.Menu;

namespace bemVisage.Core
{
    internal class UseAbilities : IFeature
    {
        private Config Config { get; set; }
        private BemVisage Main { get; set; }
        private Unit Owner { get; set; }
        private TaskHandler Handler { get; set; }
        public MenuFactory Factory { get; set; }
        public MenuItem<AbilityToggler> Items { get; set; }
        public MenuItem<AbilityToggler> Abilities { get; set; }
        public MenuItem<AbilityToggler> LinkenBreaker { get; set; }
        public MenuItem<bool> AutoSoulAssumption { get; set; }

        public void Dispose()
        {
            AutoSoulAssumption.PropertyChanged -= AutoSoulAssumptionPropertyChanged;
            Handler?.Cancel();
        }

        public void Activate(Config main)
        {
            Config = main;
            Factory = main.AbilitiesInCombo;
            Main = main.bemVisage;
            Owner = main.bemVisage.Context.Owner;


            var dict = new Dictionary<string, bool>
            {
                {AbilityId.visage_soul_assumption.ToString(), true},
                {AbilityId.visage_grave_chill.ToString(), true},
                {AbilityId.visage_summon_familiars_stone_form.ToString(), true}
            };
            var items = new Dictionary<string, bool>
            {
                {AbilityId.item_sheepstick.ToString(), true},
                {AbilityId.item_blink.ToString(), true},
                {AbilityId.item_orchid.ToString(), true},
                {AbilityId.item_bloodthorn.ToString(), true},
                {AbilityId.item_ethereal_blade.ToString(), true},
                {AbilityId.item_dagon_5.ToString(), true},
                {AbilityId.item_necronomicon_3.ToString(), true},
                {AbilityId.item_rod_of_atos.ToString(), true},
                {AbilityId.item_hurricane_pike.ToString(), true},
                {AbilityId.item_heavens_halberd.ToString(), true},
                {AbilityId.item_veil_of_discord.ToString(), true},
                {AbilityId.item_medallion_of_courage.ToString(), true},
                {AbilityId.item_solar_crest.ToString(), true},
                {AbilityId.item_nullifier.ToString(), true},
                {AbilityId.item_spirit_vessel.ToString(), true},
            };
            var linkens = new Dictionary<string, bool>
            {
                {AbilityId.item_sheepstick.ToString(), true},
                {AbilityId.item_orchid.ToString(), true},
                {AbilityId.item_bloodthorn.ToString(), true},
                {AbilityId.item_cyclone.ToString(), true},
                {AbilityId.item_force_staff.ToString(), true},
                {AbilityId.item_rod_of_atos.ToString(), true},
                {AbilityId.item_heavens_halberd.ToString(), true},
                {AbilityId.item_ethereal_blade.ToString(), true},
                {AbilityId.item_nullifier.ToString(), true},
                {AbilityId.visage_soul_assumption.ToString(), true},
                {AbilityId.visage_grave_chill.ToString(), true},
            };
            Abilities = Factory.Item("Abilities: ", new AbilityToggler(dict));
            Items = Factory.Item("Items: ", new AbilityToggler(items));
            LinkenBreaker = Factory.Item("Linken breaker: ", new AbilityToggler(linkens));
            AutoSoulAssumption = Factory.Item("Auto Soul Assumption", true);
            AutoSoulAssumption.Item.Tooltip =
                "Will use Soul Assumption automatically on closest target if the charge is max unless combo active";
            AutoSoulAssumption.PropertyChanged += AutoSoulAssumptionPropertyChanged;
            Handler = UpdateManager.Run(ExecuteAsync, true, false);
            if (AutoSoulAssumption)
            {
                Handler.RunAsync();
            }
        }

        private void AutoSoulAssumptionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (AutoSoulAssumption)
            {
                Handler.RunAsync();
            }
            else
            {
                Handler?.Cancel();
            }
        }

        private async Task ExecuteAsync(CancellationToken token)
        {
            try
            {
                if (Game.IsPaused || !Owner.IsValid || !Owner.IsAlive || Owner.IsStunned() || Config.ComboKey ||
                    Owner.IsInvisible())
                {
                    return;
                }

                var soulAssumption = Main.SoulAssumption;
                if (soulAssumption.CanBeCasted && soulAssumption.MaxCharges)
                {
                    var target =
                        EntityManager<Hero>.Entities.Where(x =>
                            x.IsValid &&
                            !x.IsIllusion &&
                            x.IsAlive &&
                            x.IsVisible &&
                            x.Team != Owner.Team &&
                            soulAssumption.CanHit(x)).OrderBy(x => x.Health).FirstOrDefault();

                    if (target != null)
                    {
                        Main.SoulAssumption.UseAbility(target);
                        await Task.Delay(Main.SoulAssumption.GetCastDelay(target), token);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // ignore
            }
            catch (Exception e)
            {
                Main.Log.Error(e);
            }
        }
    }
}