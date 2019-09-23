using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using bemVisage;
using Ensage;
using Ensage.Common.Menu;
using Ensage.Common.Objects.UtilityObjects;
using Ensage.SDK.Extensions;
using Ensage.SDK.Geometry;
using Ensage.SDK.Handlers;
using Ensage.SDK.Helpers;
using Ensage.SDK.Menu;
using SharpDX;

namespace bemVisage.Core
{
    internal class FamiliarsLanePushing : IFeature
    {
        private Config Config { get; set; }
        private BemVisage Main { get; set; }
        private IUpdateHandler Update { get; set; }
        private Sleeper Sleeper { get; set; }
        private Unit Owner { get; set; }
        public MenuFactory Factory { get; set; }
        private MenuItem<bool> UseStoneForm { get; set; }
        private MenuItem<bool> RunAwayBool { get; set; }
        private Unit Fountain { get; set; }

        public void Dispose()
        {
            UpdateManager.Unsubscribe(Update);
            Config.LasthitKey.PropertyChanged -= LasthitKeyPropertyChanged;
        }

        public void Activate(Config main)
        {
            Config = main;
            Factory = main.LanePushing;
            Main = main.bemVisage;
            Sleeper = new Sleeper();
            Owner = Main.Context.Owner;
            Fountain = ObjectManager.GetEntities<Unit>()
                .FirstOrDefault(x => x.NetworkName == "CDOTA_Unit_Fountain" && x.Team == Owner.Team);

            Update = UpdateManager.Subscribe(Execute);

            if (Config.LasthitKey)
            {
                Config.LasthitKey.Item.SetValue(new KeyBind(Config.LasthitKey.Value, KeyBindType.Toggle));
            }


            UseStoneForm = Factory.Item("Use stone form in push", false);
            RunAwayBool = Factory.Item("Run away", true);

            Config.LasthitKey.PropertyChanged += LasthitKeyPropertyChanged;
        }

        private void LasthitKeyPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Config.LasthitKey)
            {
                Update.IsEnabled = true;

                Config.FollowKey.Item.SetValue(new KeyBind(Config.FollowKey.Value, KeyBindType.Toggle));
                Config.FamiliarsLock.Item.SetValue(new KeyBind(Config.FamiliarsLock.Value, KeyBindType.Toggle));
            }
            else
            {
                Update.IsEnabled = false;
            }
        }

        private void Execute()
        {
            if (!Sleeper.Sleeping && Config.LasthitKey)
            {
                foreach (var familiar in Main.Updater.AllFamiliars)
                {
                    var enemyHero =
                        EntityManager<Hero>.Entities.FirstOrDefault(x =>
                            x.IsAlive &&
                            x.IsVisible &&
                            x.IsEnemy(Owner) &&
                            x.Distance2D(familiar.Unit) <= x.AttackRange + 400);

                    if (RunAwayBool && enemyHero != null)
                    {
                        familiar.Unit.Move(this.Owner.Position);
                        return;
                    }

                    var path = Main.LaneHelper.GetPathCache(familiar.Unit);
                    var lastPoint = path[path.Count - 1];
                    var closestPosition = path.Where(
                            x =>
                                x.Distance2D(lastPoint) < familiar.Unit.Position.Distance2D(lastPoint) - 300)
                        .OrderBy(pos => pos.Distance2D(familiar.Unit.Position))
                        .FirstOrDefault();
                    Sleeper.Sleep(250);
                    var closestTower = EntityManager<Tower>.Entities
                        .Where(x => x.IsAlive && x.IsEnemy(familiar.Unit)).OrderBy(z => z.Distance2D(familiar.Unit))
                        .FirstOrDefault();

                    var rnd = new Random();

                    if (closestTower != null && closestTower.IsInRange(familiar.Unit, 1000))
                    {
                        var myDist = familiar.Unit.Distance2D(closestTower);
                        var allyCreeps = EntityManager<Creep>.Entities.Where(x =>
                            x.IsAlly(familiar.Unit) && x.IsSpawned && x.IsAlive && !x.NetworkName.Contains("CDOTA_Unit_VisageFamiliar") &&
                            x.IsInRange(closestTower, 700) && x.Distance2D(closestTower) <= myDist);

                        if (allyCreeps.Any())
                        {
                            if (closestTower.AttackTarget != null && closestTower.AttackTarget.Equals(familiar.Unit))
                            {
                                var creepForAggro = allyCreeps.FirstOrDefault();
                                if (creepForAggro != null && !Sleeper.Sleeping)
                                {
                                    familiar.Unit.Attack(creepForAggro);
                                    return;
                                }
                                else
                                {
                                    familiar.Unit.Move(Fountain.Position);
                                    return;
                                }
                            }
                        }
                        else
                        {
                            var friendlyTower = EntityManager<Tower>.Entities
                                .Where(x => x.IsAlive && x.IsAlly(familiar.Unit))
                                .OrderBy(z => z.Distance2D(familiar.Unit))
                                .FirstOrDefault();
                            if (friendlyTower != null && !Sleeper.Sleeping)
                            {
                                familiar.Unit.Move(friendlyTower.Position);
                                Sleeper.Sleep(100);
                                return;
                            }
                            else
                            {
                                familiar.Unit.Move(this.Fountain.Position);
                                Sleeper.Sleep(100);
                                return;
                            }
                        }
                    }

                    var creep = GetTarget();
                    if (creep != null)
                    {
                        if (familiar.FamiliarMovementManager.Orbwalker.CanAttack(creep))
                        {
                            familiar.Unit.Attack(creep);

                            if (UseStoneForm && familiar.StoneForm.CanBeCasted)
                            {
                                var lowHpCreeps = EntityManager<Creep>.Entities.Count(x =>
                                    x.IsSpawned && x.IsAlive && UnitExtensions.IsEnemy(x, Owner) &&
                                    x.IsInRange(familiar.Unit, familiar.StoneForm.Radius) &&
                                    familiar.StoneForm.GetDamage(x) >= x.Health);

                                if (lowHpCreeps >= 2)
                                {
                                    familiar.StoneForm.UseAbility();
                                }
                            }
                        }
                    }
                    else
                    {
                        familiar.Unit.Attack(closestPosition);
                    }
                }
            }
        }

        private Unit GetTarget()
        {
            foreach (var familiar in Main.Updater.AllFamiliars)
            {
                var barracks =
                    ObjectManager.GetEntitiesFast<Building>()
                        .FirstOrDefault(
                            unit =>
                                unit.IsValid && unit.IsAlive && unit.Team != Owner.Team && !(unit is Tower) &&
                                familiar.Unit.IsValidOrbwalkingTarget(unit)
                                && unit.Name != "portrait_world_unit");

                if (barracks != null)
                {
                    return barracks;
                }

                var jungleCreep =
                    EntityManager<Creep>.Entities.FirstOrDefault(
                        unit =>
                            unit.IsValid && unit.IsSpawned && unit.IsAlive && unit.IsNeutral &&
                            unit.Team != this.Owner.Team &&
                            familiar.Unit.IsValidOrbwalkingTarget(unit));

                if (jungleCreep != null)
                {
                    return jungleCreep;
                }

                var creep =
                    EntityManager<Creep>.Entities.Where(
                            unit =>
                                unit.IsValid && unit.IsSpawned && unit.IsAlive && unit.Team != Owner.Team &&
                                (familiar.Unit.IsValidOrbwalkingTarget(unit) || familiar.Unit.Distance2D(unit) <= 500))
                        .OrderBy(x => x.Health)
                        .FirstOrDefault();

                if (creep != null)
                {
                    return creep;
                }

                var tower =
                    ObjectManager.GetEntitiesFast<Tower>()
                        .FirstOrDefault(
                            unit =>
                                unit.IsValid && unit.IsAlive && unit.Team != Owner.Team &&
                                familiar.Unit.IsValidOrbwalkingTarget(unit));

                if (tower != null)
                {
                    return tower;
                }

                var others =
                    ObjectManager.GetEntitiesFast<Unit>()
                        .FirstOrDefault(
                            unit =>
                                unit.IsValid && !(unit is Hero) && !(unit is Creep) && unit.IsAlive &&
                                !unit.IsInvulnerable() && unit.Team != Owner.Team &&
                                familiar.Unit.IsValidOrbwalkingTarget(unit) && unit.ClassId != ClassId.CDOTA_BaseNPC);

                if (others != null)
                {
                    return others;
                }
            }

            return null;
        }
    }
}