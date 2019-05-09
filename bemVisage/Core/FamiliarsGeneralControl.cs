using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    internal class FamiliarsGeneralControl : IFeature
    {
        private Config Config { get; set; }
        private ITargetSelector TargetSelector { get; set; }
        private BemVisage Main { get; set; }
        private MultiSleeper MultiSleeper { get; set; }
        private Unit Owner { get; set; }
        private TaskHandler Handler { get; set; }
        public MenuFactory Factory { get; set; }
        public MenuItem<Slider> FamiliarHPThreshold { get; set; }
        public MenuItem<bool> TargetCourrier { get; set; }


        public void Dispose()
        {
            Config.FollowKey.PropertyChanged -= FollowKeyPropertyChanged;
            Handler?.Cancel();
        }

        public void Activate(Config main)
        {
            Config = main;
            Factory = main.FamiliarMenu;
            MultiSleeper = Config.multiSleeper;
            Main = main.bemVisage;
            Owner = main.bemVisage.Context.Owner;
            TargetSelector = main.bemVisage.Context.TargetSelector;

            FamiliarHPThreshold = Factory.Item("Familiar HP Threshold", new Slider(50, 0, 90));
            TargetCourrier = Factory.Item("Target Courier if in sight", true);

            Handler = UpdateManager.Run(ExecuteAsync, true, true);

            if (Config.FollowKey)
            {
                Config.FollowKey.Item.SetValue(new KeyBind(Config.FollowKey.Value, KeyBindType.Toggle));
            }

            Config.FollowKey.PropertyChanged += FollowKeyPropertyChanged;
        }

        private void FollowKeyPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!Config.FollowKey)
            {
                return;
            }

            Config.LasthitKey.Item.SetValue(new KeyBind(Config.LasthitKey.Value, KeyBindType.Toggle));
            Config.FamiliarsLock.Item.SetValue(new KeyBind(Config.FamiliarsLock.Value, KeyBindType.Toggle));
        }



        private async Task ExecuteAsync(CancellationToken token)
        {
            try
            {
                if (Game.IsPaused)
                {
                    return;
                }

                var familiars = Main.Updater.AllFamiliars;
                var courier = EntityManager<Unit>.Entities.FirstOrDefault(x => x.IsValid && x.IsAlive && !x.IsInvulnerable() && x.Team != Main.Context.Owner.Team &&
                                                                               x.NetworkName == "CDOTA_Unit_Courier");

                foreach (var familiar in familiars)
                {
                    if (Config.FollowKey)
                    {
                        familiar.Unit.Follow(this.Owner);
                        await Task.Delay(250, token);
                    }

                    var familiarsStoneForm = familiar.StoneForm;
                    if (familiar.Unit.Health * 100 / familiar.Unit.MaximumHealth <= FamiliarHPThreshold && familiarsStoneForm.CanBeCasted)
                    {
                        familiarsStoneForm.UseAbility();
                        await Task.Delay(familiarsStoneForm.GetCastDelay(), token);
                    }


                    if (TargetCourrier)
                    {
                        if (courier != null && familiar.Unit.Distance2D(courier) <= 600 && !Config.FollowKey)
                        {
                            familiar.FamiliarMovementManager.Orbwalk(courier);
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
