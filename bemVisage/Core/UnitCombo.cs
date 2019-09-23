using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using bemVisage;
using Ensage;
using Ensage.Common.Menu;
using Ensage.Common.Objects.UtilityObjects;
using Ensage.Common.Threading;
using Ensage.SDK.Extensions;
using Ensage.SDK.Handlers;
using Ensage.SDK.Helpers;
using Ensage.SDK.Menu;
using Ensage.SDK.TargetSelector;
using AbilityExtensions = Ensage.Common.Extensions.AbilityExtensions;

namespace bemVisage.Core
{
    internal class UnitCombo : IFeature
    {
        private Config Config { get; set; }
        private BemVisage Main { get; set; }
        private Unit Owner { get; set; }
        private TaskHandler Handler { get; set; }
        public MenuFactory Factory { get; set; }


        public void Dispose()
        {
            if (Config.ComboKey)
            {
                Handler?.Cancel();
            }

            Config.ComboKey.PropertyChanged -= ComboPropertyChanged;
            Config.FamiliarsLock.PropertyChanged -= FamiliarsLockPropertyChanged;
        }

        public void Activate(Config main)
        {
            Config = main;
            Factory = main.FamiliarMenu;
            Main = main.bemVisage;
            Owner = main.bemVisage.Context.Owner;

            Handler = UpdateManager.Run(ExecuteAsync, true, false);

            if (Config.FollowKey)
            {
                Config.FollowKey.Item.SetValue(new KeyBind(Config.FollowKey.Value, KeyBindType.Toggle));
            }


            Config.ComboKey.PropertyChanged += ComboPropertyChanged;
            Config.FamiliarsLock.PropertyChanged += FamiliarsLockPropertyChanged;
        }

        private void FamiliarsLockPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Config.FamiliarsLock)
            {
                Config.FollowKey.Item.SetValue(new KeyBind(Config.FollowKey.Value, KeyBindType.Toggle));
                Config.LasthitKey.Item.SetValue(new KeyBind(Config.LasthitKey.Value, KeyBindType.Toggle));
            }

            if (Handler.IsRunning)
            {
                Handler?.Cancel();
            }

            if (Config.FamiliarsLock)
            {
                Handler.RunAsync();
            }
            else
            {
                Handler?.Cancel();
            }
        }

        private void ComboPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Config.FamiliarsLock)
            {
                return;
            }

            if (Config.ComboKey)
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
                if (Game.IsPaused)
                {
                    return;
                }

                Hero target = null;

                if (Config.FamiliarsLock)
                {
                    target = Config.FamiliarTarget;
                }
                else
                {
                    target = Config.Target;
                }

                foreach (var unit in Main.Updater.AllOtherUnits)
                {
                    var ability1 = unit.Ability;
                    var ability2 = unit.Ability2;
                    if (target != null)
                    {
                        if (!target.IsInvulnerable() && !target.IsAttackImmune() && !target.IsMagicImmune() &&
                            ((ability1 != null && AbilityExtensions.CanBeCasted(ability1)) || (ability2 != null && AbilityExtensions.CanBeCasted(ability2))))
                        {
                            //Main.Log.Debug($"null? {ability1 != null}");
                            //Main.Log.Debug($"CanBeCasted? {AbilityExtensions.CanBeCasted(ability1)}");
                            //Main.Log.Debug($"TargetTeamType? {ability1?.TargetTeamType.ToString()}");
                            //Main.Log.Debug($"Distance2D? {unit.Unit.Distance2D(target) <= ability1?.CastRange}");
                            //Main.Log.Debug($"cast range {AbilityExtensions.GetRadius(ability1)}");

                            //Main.Log.Debug($"AbilityBehavior? {ability1?.AbilityBehavior.ToString()}");
                            //Main.Log.Debug($"AbilityBehavior? {ability1?.AbilityBehavior == AbilityBehavior.UnitTarget}");

                            if (ability1 != null
                                && AbilityExtensions.CanBeCasted(ability1)
                                && AbilityExtensions.CanHit(ability1, target)
                                && (ability1.TargetTeamType == TargetTeamType.Enemy ||
                                    ability1.TargetTeamType == TargetTeamType.None)
                                && (unit.Unit.Distance2D(target) <= ability1.CastRange - 70 ||
                                    unit.Unit.Distance2D(target) <= AbilityExtensions.GetRadius(ability1) - 70))
                            {
                                if (ability1.AbilityBehavior.HasFlag(AbilityBehavior.NoTarget))
                                {
                                    ability1.UseAbility();
                                    await Task.Delay(250, token);
                                }
                                else if (ability1.AbilityBehavior.HasFlag(AbilityBehavior.UnitTarget))
                                {
                                    ability1.UseAbility(target);
                                    await Task.Delay(250, token);
                                }
                                else if (ability1.AbilityBehavior.HasFlag(AbilityBehavior.Point))
                                {
                                    ability1.UseAbility(target.Position);
                                    await Task.Delay(250, token);
                                }
                            }
                            else if (ability1 != null
                                     && AbilityExtensions.CanBeCasted(ability1)
                                     && ability1.TargetTeamType == TargetTeamType.Allied
                                     && unit.Unit.Distance2D(this.Owner) <= ability1.CastRange)
                            {
                                ability1.UseAbility(this.Owner);
                                await Task.Delay(250, token);
                            }

                            if (ability2 != null
                                && AbilityExtensions.CanBeCasted(ability2)
                                && AbilityExtensions.CanHit(ability2, target)
                                && (ability2.TargetTeamType == TargetTeamType.Enemy ||
                                    ability2.TargetTeamType == TargetTeamType.None)
                                && (unit.Unit.Distance2D(target) <= ability2.CastRange -70 ||
                                    unit.Unit.Distance2D(target) <= AbilityExtensions.GetRadius(ability2) - 70))
                            {
                                if (ability2.AbilityBehavior.HasFlag(AbilityBehavior.NoTarget))
                                {
                                    ability2.UseAbility();
                                    await Task.Delay(250, token);
                                }
                                else if (ability2.AbilityBehavior.HasFlag(AbilityBehavior.UnitTarget))
                                {
                                    ability2.UseAbility(target);
                                    await Task.Delay(250, token);
                                }
                                else if (ability2.AbilityBehavior.HasFlag(AbilityBehavior.Point))
                                {
                                    ability2.UseAbility(target.Position);
                                    await Task.Delay(250, token);
                                }
                            }
                            else if (ability2 != null
                                     && AbilityExtensions.CanBeCasted(ability2)
                                     && ability2.TargetTeamType == TargetTeamType.Allied
                                     && unit.Unit.Distance2D(this.Owner) <= ability2.CastRange)
                            {
                                ability2.UseAbility(this.Owner);
                                await Task.Delay(250, token);
                            }
                            unit.FamiliarMovementManager.Move(target.InFront(50));
                        }

                        if (target.IsInvulnerable() || target.IsAttackImmune())
                        {
                            unit.FamiliarMovementManager.Orbwalk(null);
                        }

                        else if (target.IsMagicImmune()
                                 || (!AbilityExtensions.CanBeCasted(ability1) || !AbilityExtensions.CanBeCasted(ability2)))
                        {
                            unit.FamiliarMovementManager.Orbwalk(target);
                        }
                    }
                    else
                    {
                        if (this.Config.FamiliarMenu.GetValue<bool>("Follow Mouse"))
                        {
                            unit.FamiliarMovementManager.Orbwalk(null);
                        }
                        else
                        {
                            unit.Unit.Follow(this.Owner);
                            await Task.Delay(350, token);
                        }
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