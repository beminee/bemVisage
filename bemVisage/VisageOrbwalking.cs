using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using bemVisage;
using Ensage;
using Ensage.Common;
using Ensage.Common.Menu;
using Ensage.Common.Objects.UtilityObjects;
using Ensage.SDK.Abilities.Aggregation;
using Ensage.SDK.Abilities.Items;
using Ensage.SDK.Abilities.npc_dota_hero_broodmother;
using Ensage.SDK.Extensions;
using Ensage.SDK.Geometry;
using Ensage.SDK.Handlers;
using Ensage.SDK.Helpers;
using Ensage.SDK.Inventory.Metadata;
using Ensage.SDK.Orbwalker.Modes;
using Ensage.SDK.Renderer;
using Ensage.SDK.Renderer.Particle;
using Ensage.SDK.Service;
using Ensage.SDK.TargetSelector;
using log4net;
using PlaySharp.Toolkit.Helper.Annotations;
using SharpDX;

namespace bemVisage
{
    [PublicAPI]
    public class VisageOrbwalking : KeyPressOrbwalkingModeAsync
    {
        private BemVisage Main { get; set; }
        private Config Config { get; set; }
        private MultiSleeper MultiSleeper { get; set; }
        private ITargetSelector TargetSelector { get; set; }
        private IParticleManager ParticleManager { get; set; }

        public VisageOrbwalking(Key key, Config config)
            : base(config.bemVisage.Context, key)
        {
            Config = config;
            Main = config.bemVisage;
            MultiSleeper = config.multiSleeper;
            TargetSelector = Main.Context.TargetSelector;
            ParticleManager = Main.Context.Particle;

            UpdateManager.Subscribe(OnUpdate, 25);
        }

        public override async Task ExecuteAsync(CancellationToken token)
        {
            var target = Config.Target;

            if (!await Main.ShouldExecute(token))
            {
                return;
            }

            if ((target == null) || !target.IsVisible || !target.IsAlive)
            {
                Context.Orbwalker.Active.OrbwalkTo(null);
                await Task.Delay(50, token);
                return;
            }

            if (target.IsIllusion)
            {
                Context.Orbwalker.Active.OrbwalkTo(target);
                return;
            }

            if ((!Config.BmBehavior || !target.HasModifier("modifier_item_blade_mail_reflect")))
            {
                var stunDebuff = target.Modifiers.FirstOrDefault(x => x != null && x.IsValid && x.IsStunDebuff);
                var hexDebuff = target.Modifiers.FirstOrDefault(x =>
                    x != null && x.IsValid && x.Name == "modifier_sheepstick_debuff");
                var atosDebuff = target.Modifiers.FirstOrDefault(x =>
                    x != null && x.IsValid && x.Name == "modifier_rod_of_atos_debuff");
                var modifierHurricanePike = Owner.HasModifier("modifier_item_hurricane_pike_range");

                var blink = Main.Blink;
                if (blink != null
                    && Main.IsItemEnabled(blink)
                    && Owner.Distance2D(Game.MousePosition) > Config.BlinkDistance2Mouse
                    && Owner.Distance2D(target) > 600
                    && blink.CanBeCasted)
                {
                    var blinkPos = target.Position.Extend(Game.MousePosition, Config.BlinkDistance2Mouse);
                    if (Owner.Distance2D(blinkPos) < blink.CastRange)
                    {
                        blink.UseAbility(blinkPos);
                        await Task.Delay(blink.GetCastDelay(blinkPos), token);
                    }
                }

                if (!target.IsInvulnerable() && !target.IsAttackImmune())
                {
                    if (!target.IsBlockingAbilities())
                    {
                        var hex = Main.Hex;
                        if (hex != null
                            && Main.IsItemEnabled(hex)
                            && hex.CanBeCasted
                            && hex.CanHit(target)
                            && (stunDebuff == null || !stunDebuff.IsValid || stunDebuff.RemainingTime <= 0.2f)
                            && (hexDebuff == null || !hexDebuff.IsValid || hexDebuff.RemainingTime <= 0.2f))
                        {
                            hex.UseAbility(target);
                            await Task.Delay(hex.GetCastDelay(target), token);
                        }

                        var orchid = Main.Orchid;
                        if (orchid != null
                            && Main.IsItemEnabled(orchid)
                            && orchid.CanBeCasted
                            && orchid.CanHit(target))
                        {
                            orchid.UseAbility(target);
                            await Task.Delay(orchid.GetCastDelay(target), token);
                        }

                        var bloodthorn = Main.Bloodthorn;
                        if (bloodthorn != null
                            && Main.IsItemEnabled(bloodthorn)
                            && bloodthorn.CanBeCasted
                            && bloodthorn.CanHit(target))
                        {
                            bloodthorn.UseAbility(target);
                            await Task.Delay(bloodthorn.GetCastDelay(target), token);
                        }

                        var nullifier = Main.Nullifier;
                        if (nullifier != null
                            && Main.IsItemEnabled(nullifier)
                            && nullifier.CanBeCasted
                            && nullifier.CanHit(target)
                            && (stunDebuff == null || !stunDebuff.IsValid ||
                                stunDebuff.RemainingTime <= nullifier.GetHitTime(target) + 0.2f)
                            && (hexDebuff == null || !hexDebuff.IsValid ||
                                hexDebuff.RemainingTime <= nullifier.GetHitTime(target) + 0.2f))
                        {
                            nullifier.UseAbility(target);
                            await Task.Delay(nullifier.GetCastDelay(target), token);
                        }

                        var rodofAtos = Main.RodOfAtos;
                        if (rodofAtos != null
                            && Main.IsItemEnabled(rodofAtos)
                            && rodofAtos.CanBeCasted
                            && rodofAtos.CanHit(target)
                            && (stunDebuff == null || !stunDebuff.IsValid ||
                                stunDebuff.RemainingTime <= rodofAtos.GetHitTime(target) + 0.2f)
                            && (atosDebuff == null || !atosDebuff.IsValid ||
                                atosDebuff.RemainingTime <= rodofAtos.GetHitTime(target) + 0.2f))
                        {
                            rodofAtos.UseAbility(target);
                            await Task.Delay(rodofAtos.GetCastDelay(target), token);
                        }

                        var graveChill = Main.GraveChill;
                        if (Main.IsAbilityEnabled(graveChill.Ability.Id)
                            && graveChill.CanBeCasted
                            && graveChill.CanHit(target))
                        {
                            graveChill.UseAbility(target);
                            await Task.Delay(graveChill.GetCastDelay(target), token);
                        }

                        var hurricanePike = Main.HurricanePike;
                        if (hurricanePike != null
                            && Main.IsItemEnabled(hurricanePike)
                            && hurricanePike.CanBeCasted
                            && hurricanePike.CanHit(target)
                            && !MultiSleeper.Sleeping("etherealblade")
                            && !target.IsEthereal())
                        {
                            hurricanePike.UseAbility(target);
                            await Task.Delay(hurricanePike.GetCastDelay(target), token);
                            return;
                        }

                        var veil = Main.VeilOfDiscord;
                        if (veil != null
                            && Main.IsItemEnabled(veil)
                            && veil.CanBeCasted
                            && veil.CanHit(target))
                        {
                            veil.UseAbility(target.Position);
                            await Task.Delay(veil.GetCastDelay(target.Position), token);
                        }

                        var ethereal = Main.EtherealBlade;
                        if (ethereal != null
                            && Main.IsItemEnabled(ethereal)
                            && ethereal.CanBeCasted
                            && ethereal.CanHit(target)
                            && !modifierHurricanePike)
                        {
                            ethereal.UseAbility(target);
                            MultiSleeper.Sleep(ethereal.GetHitTime(target), "etherealblade");
                            await Task.Delay(ethereal.GetCastDelay(target), token);
                        }

                        var shivas = Main.ShivasGuard;
                        if (shivas != null
                            && Main.IsItemEnabled(shivas)
                            && shivas.CanBeCasted
                            && shivas.CanHit(target))
                        {
                            shivas.UseAbility();
                            await Task.Delay(shivas.GetCastDelay(), token);
                        }

                        if (!MultiSleeper.Sleeping("ethereal") || target.IsEthereal())
                        {
                            var SoulAssumption = Main.SoulAssumption;
                            if (Main.IsAbilityEnabled(SoulAssumption.Ability.Id)
                                && SoulAssumption.CanBeCasted
                                && SoulAssumption.CanHit(target)
                                && SoulAssumption.MaxCharges)
                            {
                                SoulAssumption.UseAbility(target);
                                await Task.Delay(SoulAssumption.GetCastDelay(target), token);
                                return;
                            }

                            var Dagon = Main.Dagon;
                            if (Dagon != null
                                && Main.IsItemEnabled(AbilityId.item_dagon_5)
                                && Dagon.CanBeCasted
                                && Dagon.CanHit(target))
                            {
                                Dagon.UseAbility(target);
                                await Task.Delay(Dagon.GetCastDelay(target), token);
                                return;
                            }
                        }

                        var medallion = Main.Medallion;
                        if (medallion != null
                            && Main.IsItemEnabled(medallion)
                            && medallion.CanBeCasted
                            && medallion.CanHit(target))
                        {
                            medallion.UseAbility(target);
                            await Task.Delay(medallion.GetCastDelay(target), token);
                        }

                        var solarCrest = Main.SolarCrest;
                        if (solarCrest != null
                            && Main.IsItemEnabled(solarCrest)
                            && solarCrest.CanBeCasted
                            && solarCrest.CanHit(target))
                        {
                            solarCrest.UseAbility(target);
                            await Task.Delay(solarCrest.GetCastDelay(target), token);
                        }

                        var urnOfShadows = Main.UrnOfShadows;
                        if (urnOfShadows != null
                            && Main.IsItemEnabled(AbilityId.item_spirit_vessel)
                            && urnOfShadows.CanBeCasted
                            && urnOfShadows.CanHit(target))
                        {
                            urnOfShadows.UseAbility(target);
                            await Task.Delay(urnOfShadows.GetCastDelay(target), token);
                        }

                        var spiritVessel = Main.SpiritVessel;
                        if (spiritVessel != null
                            && Main.IsItemEnabled(AbilityId.item_spirit_vessel)
                            && spiritVessel.CanBeCasted
                            && spiritVessel.CanHit(target))
                        {
                            spiritVessel.UseAbility(target);
                            await Task.Delay(spiritVessel.GetCastDelay(target), token);
                        }
                    }
                    else
                    {
                        Config.LinkenHandler.RunAsync();
                    }

                    var necronomicon = Main.Necronomicon;
                    if (necronomicon != null
                        && Main.IsItemEnabled(AbilityId.item_necronomicon_3)
                        && necronomicon.CanBeCasted
                        && Owner.Distance2D(target) <= Owner.AttackRange)
                    {
                        necronomicon.UseAbility();
                        await Task.Delay(necronomicon.GetCastDelay(), token);
                    }

                    Main.Context.Orbwalker.Active.OrbwalkTo(target);
                }
            }
        }



        private void OnUpdate()
        {
            if (Config.TargetOption.Value.SelectedValue.Contains("Lock") && TargetSelector.IsActive
                                                                         && (!Config.ComboKey ||
                                                                             Config.Target == null ||
                                                                             !Config.Target.IsValid ||
                                                                             !Config.Target.IsAlive))
            {
                Config.Target = TargetSelector.GetTargets().FirstOrDefault() as Hero;
            }
            else if (Config.TargetOption.Value.SelectedValue.Contains("Default") && TargetSelector.IsActive)
            {
                Config.Target = TargetSelector.GetTargets().FirstOrDefault() as Hero;
            }

            if (TargetSelector.IsActive
                && (!this.Config.FamiliarMenu.GetValue<KeyBind>("Units Target Lock").Active ||
                    Config.FamiliarTarget == null || !Config.FamiliarTarget.IsValid || !Config.FamiliarTarget.IsAlive))
            {
                Config.FamiliarTarget = TargetSelector.GetTargets().FirstOrDefault() as Hero;
            }

            var graveChill = Main.GraveChill;
            if (Config.GraveChillsDraw && graveChill.Ability.Level > 0)
            {
                ParticleManager.DrawRange(
                    Owner,
                    "GraveChill",
                    graveChill.CastRange,
                    Color.Red);
            }
            else
            {
                ParticleManager.Remove("GraveChill");
            }

            var soulAssumption = Main.SoulAssumption;
            if (Config.SoulAssumptionDraw && soulAssumption.Ability.Level > 0)
            {
                ParticleManager.DrawRange(
                    Owner,
                    "SoulAssumption",
                    soulAssumption.CastRange,
                    Color.Blue);
            }
            else
            {
                ParticleManager.Remove("SoulAssumption");
            }

            if (Config.ComboKey && (Config.Target != null) && this.Config.DrawTargetIndicator)
            {
                this.Context.Particle.DrawTargetLine(this.Owner, "TargetIndicator", Config.Target.NetworkPosition,
                    Color.Red);
            }
            else
            {
                this.Context.Particle.Remove("TargetIndicator");
            }

            if (Config.FamiliarsLock && (Config.FamiliarTarget != null) && Main.Updater.AllFamiliars.Count() > 0 && this.Config.DrawTargetIndicator)
            {
                this.Context.Particle.DrawTargetLine(Main.Updater.AllFamiliars.First().Unit, "FamiliarTargetIndicator",
                    Config.FamiliarTarget.NetworkPosition, Color.Yellow);
            }
            else
            {
                this.Context.Particle.Remove("FamiliarTargetIndicator");
            }
        }
    }
}