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
    internal class FamiliarsCombo : IFeature
    {
        private Config Config { get; set; }
        private BemVisage Main { get; set; }
        private MultiSleeper MultiSleeper { get; set; }
        private Unit Owner { get; set; }
        private TaskHandler Handler { get; set; }
        public MenuFactory Factory { get; set; }
        public MenuItem<bool> FamiliarsFollow { get; set; }


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
            MultiSleeper = Config.multiSleeper;
            Main = main.bemVisage;
            Owner = main.bemVisage.Context.Owner;

            Handler = UpdateManager.Run(ExecuteAsync, true, false);

            if (Config.FollowKey)
            {
                Config.FollowKey.Item.SetValue(new KeyBind(Config.FollowKey.Value, KeyBindType.Toggle));
            }


            FamiliarsFollow = Factory.Item("Follow Mouse", true);

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

                foreach (var familiar in Main.Updater.AllFamiliars)
                {
                    if (target != null)
                    {
                        var graveChillDebuff = target.HasModifier(Main.GraveChill.TargetModifierName);
                        var stunDebuff =
                            target.Modifiers.Any(
                                x => x != null && x.IsValid && x.IsStunDebuff && x.RemainingTime > 0.5f);
                        var hexDebuff = target.Modifiers.Any(x => x != null &&
                                                                  x.IsValid && x.Name == "modifier_sheepstick_debuff" &&
                                                                  x.RemainingTime > 0.5f);
                        var atosDebuff = target.Modifiers.Any(x => x != null &&
                                                                   x.IsValid &&
                                                                   x.Name == "modifier_rod_of_atos_debuff" &&
                                                                   x.RemainingTime > 0.5f);
                        var familiarsStoneForm = familiar.StoneForm;

                        if (!target.IsInvulnerable() && !target.IsAttackImmune())
                        {
                            if (Main.IsAbilityEnabled(familiarsStoneForm.Ability.Id)
                                && familiarsStoneForm.CanBeCasted
                                && familiar.Unit.Distance2D(target) <= 100
                                && !graveChillDebuff && !stunDebuff && !hexDebuff && !atosDebuff
                                && !MultiSleeper.Sleeping("FamiliarsStoneForm"))
                            {
                                familiarsStoneForm.UseAbility();
                                MultiSleeper.Sleep(
                                    familiarsStoneForm.StunDuration,
                                    "FamiliarsStoneForm");
                                await Task.Delay(
                                    (int) (familiarsStoneForm.Ability.GetAbilitySpecialData("stun_delay") * 1000 +
                                           Game.Ping),
                                    token);
                            }
                            else if (Main.IsAbilityEnabled(familiarsStoneForm.Ability.Id)
                                     && familiarsStoneForm.CanBeCasted
                                     && familiar.Unit.Distance2D(target) > 120
                                     && !graveChillDebuff && !stunDebuff && !hexDebuff && !atosDebuff
                                     && !MultiSleeper.Sleeping("FamiliarsStoneForm"))
                            {
                                familiar.FamiliarMovementManager.Move(target.InFront(50));
                            }
                            //else
                            //{
                            //    familiar.FamiliarMovementManager.Orbwalk(target);
                            //}
                        }

                        if (target.IsInvulnerable() || target.IsAttackImmune())
                        {
                            familiar.FamiliarMovementManager.Orbwalk(null);
                        }
                        else if (!Main.IsAbilityEnabled(familiarsStoneForm.Ability.Id)
                                 || target.IsMagicImmune()
                                 || !familiarsStoneForm.CanBeCasted
                                 || graveChillDebuff || stunDebuff || hexDebuff || atosDebuff)
                        {
                            familiar.FamiliarMovementManager.Orbwalk(target);
                        }

                        //Main.Log.Debug($"{target != null}");
                        //Main.Log.Debug($"{!target.IsInvulnerable() && !target.IsAttackImmune()}");
                        //familiar.FamiliarMovementManager.Orbwalk(target);
                        //familiarsStoneForm.UseAbility();
                    }
                    else
                    {
                        if (FamiliarsFollow)
                        {
                            familiar.FamiliarMovementManager.Orbwalk(null);
                        }
                        else
                        {
                            familiar.Unit.Follow(this.Owner);
                            await Task.Delay(150, token);
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