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
using UnitExtensions = Ensage.SDK.Extensions.UnitExtensions;

namespace bemVisage.Core
{
    internal class AutoKillStealer : IFeature
    {
        private Config Config { get; set; }
        private BemVisage Main { get; set; }
        private Unit Owner { get; set; }
        private TaskHandler Handler { get; set; }
        public MenuFactory Factory { get; set; }
        public MenuItem<bool> AutoKillStealerItem { get; set; }

        public void Dispose()
        {
            AutoKillStealerItem.PropertyChanged -= AutoKillStealerItemPropertyChanged;
            Handler?.Cancel();
        }

        public void Activate(Config main)
        {
            Config = main;
            Factory = main.Factory;
            Main = main.bemVisage;
            Owner = main.bemVisage.Context.Owner;

            AutoKillStealerItem = Factory.Item("Auto Killstealer", true);
            AutoKillStealerItem.Item.Tooltip = "Killstealer";
            AutoKillStealerItem.PropertyChanged += AutoKillStealerItemPropertyChanged;
            Handler = UpdateManager.Run(ExecuteAsync, true, false);
            if (AutoKillStealerItem)
            {
                Handler.RunAsync();
            }
        }

        private void AutoKillStealerItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (AutoKillStealerItem)
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
                if (Game.IsPaused || !Owner.IsValid || !Owner.IsAlive || Owner.IsStunned())
                {
                    await Task.Delay(125, token);
                    return;
                }

                var target = EntityManager<Hero>.Entities.FirstOrDefault(
                    x => x.IsAlive
                         && (x.Team != this.Owner.Team)
                         && !x.IsIllusion
                         && Main.SoulAssumption.CanHit(x)
                         && Main.SoulAssumption.GetDamage(x) > x.Health);

                if (target != null)
                {
                    if (!UnitExtensions.IsBlockingAbilities(target))
                    {
                        if (Main.SoulAssumption.UseAbility(target))
                        {
                            await Task.Delay(Main.SoulAssumption.GetCastDelay(target) + 20, token);
                        }
                    }
                    else
                    {
                        Config.LinkenHandler.RunAsync();
                        return;
                    }
                }

                await Task.Delay(125, token);
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