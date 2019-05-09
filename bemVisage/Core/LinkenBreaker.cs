using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using bemVisage;
using Ensage;
using Ensage.Common.Extensions;
using Ensage.Common.Menu;
using Ensage.SDK.Abilities;
using Ensage.SDK.Handlers;
using Ensage.SDK.Helpers;
using Ensage.SDK.Menu;

namespace bemVisage.Core
{
    internal class LinkenBreaker : IFeature
    {
        private Config Config { get; set; }
        private BemVisage bemVisage { get; set; }
        private Unit Owner { get; set; }
        private MenuFactory Factory { get; set; }

        public MenuItem<PriorityChanger> LinkenBreakerPriorityMenu { get; set; }

        public void Dispose()
        {
            Config.LinkenHandler?.Cancel();
        }

        public void Activate(Config main)
        {
            Config = main;
            bemVisage = main.bemVisage;
            Factory = Config.LinkenBreaker;
            Owner = bemVisage.Context.Owner;

            LinkenBreakerPriorityMenu = Factory.Item("Priority: ", new PriorityChanger(new List<string>
            {
                {"item_ethereal_blade"},
                {"visage_soul_assumption"},
                {"visage_grave_chill"},
                {"item_sheepstick"},
                {"item_rod_of_atos"},
                {"item_nullifier"},
                {"item_bloodthorn"},
                {"item_orchid"},
                {"item_heavens_halberd"},
                {"item_cyclone"},
                {"item_force_staff"}
            }));

            main.LinkenHandler = UpdateManager.Run(ExecuteAsync, false, false);
        }

        private async Task ExecuteAsync(CancellationToken token)
        {
            if (Config.Target != null && Config.Target.IsValid)
            {
                try
                {
                    List<KeyValuePair<string, uint>> breakerChanger = new List<KeyValuePair<string, uint>>();

                    if (Config.Target.IsLinkensProtected())
                    {
                        breakerChanger = LinkenBreakerPriorityMenu.Value.Dictionary.Where(
                                x => bemVisage.IsLinkenBreakerEnabled(x.Key))
                            .OrderByDescending(x => x.Value)
                            .ToList();
                    }

                    foreach (var order in breakerChanger)
                    {
                        var euls = bemVisage.Euls;
                        if (euls != null
                            && euls.ToString() == order.Key
                            && euls.CanBeCasted && euls.CanHit(Config.Target))
                        {
                            euls.UseAbility(Config.Target);
                            await Task.Delay(euls.GetCastDelay(Config.Target), token);
                            return;
                        }

                        var force = bemVisage.ForceStaff;
                        if (force != null
                            && force.ToString() == order.Key
                            && force.CanBeCasted && force.CanHit(Config.Target))
                        {
                            force.UseAbility(Config.Target);
                            await Task.Delay(force.GetCastDelay(Config.Target), token);
                            return;
                        }

                        var orchid = bemVisage.Orchid;
                        if (orchid != null
                            && orchid.ToString() == order.Key
                            && orchid.CanBeCasted && orchid.CanHit(Config.Target))
                        {
                            orchid.UseAbility(Config.Target);
                            await Task.Delay(orchid.GetCastDelay(Config.Target), token);
                            return;
                        }

                        var bloodthorn = bemVisage.Bloodthorn;
                        if (bloodthorn != null
                            && bloodthorn.ToString() == order.Key
                            && bloodthorn.CanBeCasted && bloodthorn.CanHit(Config.Target))
                        {
                            bloodthorn.UseAbility(Config.Target);
                            await Task.Delay(bloodthorn.GetCastDelay(Config.Target), token);
                            return;
                        }

                        var nullifier = bemVisage.Nullifier;
                        if (nullifier != null
                            && nullifier.ToString() == order.Key
                            && nullifier.CanBeCasted && nullifier.CanHit(Config.Target))
                        {
                            nullifier.UseAbility(Config.Target);
                            await Task.Delay(
                                nullifier.GetCastDelay(Config.Target) + nullifier.GetHitTime(Config.Target), token);
                            return;
                        }

                        var atos = bemVisage.RodOfAtos;
                        if (atos != null
                            && atos.ToString() == order.Key
                            && atos.CanBeCasted && atos.CanHit(Config.Target))
                        {
                            atos.UseAbility(Config.Target);
                            await Task.Delay(atos.GetCastDelay(Config.Target) + atos.GetHitTime(Config.Target), token);
                            return;
                        }

                        var hex = bemVisage.Hex;
                        if (hex != null
                            && hex.ToString() == order.Key
                            && hex.CanBeCasted && hex.CanHit(Config.Target))
                        {
                            hex.UseAbility(Config.Target);
                            await Task.Delay(hex.GetCastDelay(Config.Target), token);
                            return;
                        }

                        var halberd = bemVisage.Halberd;
                        if (halberd != null
                            && halberd.ToString() == order.Key
                            && halberd.CanBeCasted && halberd.CanHit(Config.Target))
                        {
                            halberd.UseAbility(Config.Target);
                            await Task.Delay(halberd.GetCastDelay(Config.Target), token);
                            return;
                        }

                        var ethereal = bemVisage.EtherealBlade;
                        if (ethereal != null
                            && ethereal.ToString() == order.Key
                            && ethereal.CanBeCasted && ethereal.CanHit(Config.Target))
                        {
                            ethereal.UseAbility(Config.Target);
                            await Task.Delay(halberd.GetHitTime(Config.Target), token);
                            return;
                        }

                        var soul_assumption = bemVisage.SoulAssumption;
                        if (soul_assumption != null
                            && soul_assumption.ToString() == order.Key
                            && soul_assumption.CanBeCasted && soul_assumption.CanHit(Config.Target))
                        {
                            soul_assumption.UseAbility(Config.Target);
                            await Task.Delay(soul_assumption.GetCastDelay(Config.Target), token);
                            return;
                        }

                        var grave_chill = bemVisage.GraveChill;
                        if (grave_chill != null
                            && grave_chill.ToString() == order.Key
                            && grave_chill.CanBeCasted && grave_chill.CanHit(Config.Target))
                        {
                            grave_chill.UseAbility(Config.Target);
                            await Task.Delay(grave_chill.GetCastDelay(Config.Target), token);
                            return;
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    // ignore
                }
                catch (Exception e)
                {
                    bemVisage.Log.Error(e);
                }
            }
        }
    }
}