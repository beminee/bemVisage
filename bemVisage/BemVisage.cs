using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Ensage;
using Ensage.Common;
using Ensage.Common.Extensions;
using Ensage.Common.Menu;
using Ensage.SDK.Abilities;
using Ensage.SDK.Abilities.Aggregation;
using Ensage.SDK.Abilities.Items;
using Ensage.SDK.Abilities.npc_dota_hero_visage;
using Ensage.SDK.Inventory.Metadata;
using Ensage.SDK.Renderer;
using Ensage.SDK.Service;
using Ensage.SDK.Service.Metadata;
using log4net;
using PlaySharp.Toolkit.Logging;
using bemVisage;
using bemVisage.Core;
using SharpDX;
using System.Linq;
using bemVisage.Utilities;
using Ensage.Common.Objects.UtilityObjects;
using Ensage.SDK.Helpers;
using UnitExtensions = Ensage.SDK.Extensions.UnitExtensions;

namespace bemVisage
{
    [ExportPlugin(name: "bemVisage", mode: StartupMode.Auto, units: HeroId.npc_dota_hero_visage)]
    public class BemVisage : Plugin
    {
        [ImportMany] private IEnumerable<IFeature> features;

        public IServiceContext Context { get; }
        public IRenderManager RendererManager { get; set; }
        protected RenderMode RenderMode => Drawing.RenderMode;
        protected bool IsDx9 => RenderMode == RenderMode.Dx9;
        protected bool IsDx11 => RenderMode == RenderMode.Dx11;

        private AbilityFactory AbilityFactory { get; }

        public ILog Log { get; } = AssemblyLogs.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Config Config { get; set; }

        public Updater Updater { get; set; }

        public LaneHelper LaneHelper { get; set; }

        #region abilities

        public visage_grave_chill GraveChill { get; set; }

        public visage_soul_assumption SoulAssumption { get; set; }

        #endregion

        #region items

        [ItemBinding] public item_necronomicon Necronomicon1 { get; set; }

        [ItemBinding] public item_necronomicon_2 Necronomicon2 { get; set; }

        [ItemBinding] public item_necronomicon_3 Necronomicon3 { get; set; }

        [ItemBinding] public item_dagon Dagon1 { get; set; }

        [ItemBinding] public item_dagon_2 Dagon2 { get; set; }

        [ItemBinding] public item_dagon_3 Dagon3 { get; set; }

        [ItemBinding] public item_dagon_4 Dagon4 { get; set; }

        [ItemBinding] public item_dagon_5 Dagon5 { get; set; }

        [ItemBinding] public item_nullifier Nullifier { get; set; }

        [ItemBinding] public item_medallion_of_courage Medallion { get; set; }

        [ItemBinding] public item_solar_crest SolarCrest { get; set; }

        [ItemBinding] public item_sheepstick Hex { get; set; }

        [ItemBinding] public item_orchid Orchid { get; set; }

        [ItemBinding] public item_bloodthorn Bloodthorn { get; set; }

        [ItemBinding] public item_cyclone Euls { get; set; }

        [ItemBinding] public item_force_staff ForceStaff { get; set; }

        [ItemBinding] public item_rod_of_atos RodOfAtos { get; set; }

        [ItemBinding] public item_heavens_halberd Halberd { get; set; }

        [ItemBinding] public item_ethereal_blade EtherealBlade { get; set; }

        [ItemBinding] public item_blink Blink { get; set; }

        [ItemBinding] public item_hurricane_pike HurricanePike { get; set; }

        [ItemBinding] public item_veil_of_discord VeilOfDiscord { get; set; }

        [ItemBinding] public item_shivas_guard ShivasGuard { get; set; }

        [ItemBinding] public item_urn_of_shadows UrnOfShadows { get; set; }

        [ItemBinding] public item_spirit_vessel SpiritVessel { get; set; }


        public Necronomicon Necronomicon
        {
            get { return Necronomicon1 ?? Necronomicon2 ?? (Necronomicon) Necronomicon3; }
        }

        public Dagon Dagon
        {
            get { return Dagon1 ?? Dagon2 ?? Dagon3 ?? Dagon4 ?? (Dagon) Dagon5; }
        }

        #endregion


        [ImportingConstructor]
        public BemVisage([Import] IServiceContext context)
        {
            Context = context;
            RendererManager = context.RenderManager;
            AbilityFactory = context.AbilityFactory;
        }

        private readonly Dictionary<uint, Order> EntityOrders = new Dictionary<uint, Order>();

        private readonly Dictionary<uint, Order> Cache = new Dictionary<uint, Order>();

        private bool IsRunning;

        private bool IsNewOrder;

        private readonly Sleeper Sleeper = new Sleeper();

        private void EntityRemoved(object sender, Entity e)
        {
            EntityOrders.Remove(e.Handle);
        }

        private readonly HashSet<OrderId> OrderIds = new HashSet<OrderId>
        {
            OrderId.MoveLocation,
            OrderId.MoveTarget,
            OrderId.AttackLocation,
            OrderId.AttackTarget,
            OrderId.AbilityLocation,
            OrderId.AbilityTarget,
            OrderId.AbilityTargetTree,
            OrderId.Ability,
            OrderId.ToggleAbility,
            OrderId.ToggleAutoCast,
            OrderId.AbilityTargetRune,
            OrderId.MoveToDirection
        };

        private void AddOrders(IEnumerable<Entity> entities, OrderId orderId, Vector3 targetPosition, Entity target, Ability ability)
        {
            foreach (var entity in entities)
            {
                var handle = ability != null ? ability.Handle : entity.Handle;
                if (!EntityOrders.TryGetValue(handle, out var entityOrder))
                {
                    entityOrder = new Order();
                    EntityOrders[handle] = entityOrder;
                }

                entityOrder.Entity = entity;
                entityOrder.OrderId = orderId;
                entityOrder.TargetPosition = targetPosition;
                entityOrder.Target = target;
                entityOrder.Ability = ability;

                Cache[handle] = entityOrder;
            }
        }

        private void OnExecuteOrder(Player sender, ExecuteOrderEventArgs args)
        {
            if (args.IsPlayerInput)
            {
                return;
            }

            if (IsNewOrder)
            {
                return;
            }

            if (!OrderIds.Contains(args.OrderId))
            {
                return;
            }

            try
            {
                AddOrders(args.Entities, args.OrderId, args.TargetPosition, args.Target, args.Ability);

                args.Process = false;

                if (IsRunning || Sleeper.Sleeping)
                {
                    return;
                }

                IsRunning = true;

                UpdateManager.BeginInvoke(() =>
                {
                    var rnd = new Random();
                    var delay = rnd.Next(0, 10) + this.Config.OrderLimiterDelay.Value;

                    Sleeper.Sleep(delay);

                    var orders = Cache.Select(x => x.Value);
                    var order = orders.OrderBy(x => x.Time).First();
                    var newOrders = orders.Where(x => x.OrderId == order.OrderId && x.TargetPosition == order.TargetPosition && x.Target == order.Target && x.Ability == order.Ability);

                    IsNewOrder = true;
                    Player.EntitiesOrder(order.OrderId, newOrders.Select(x => x.Entity), order.Target?.Index ?? 0, order.Ability?.Index ?? 0, order.TargetPosition, false);
                    IsNewOrder = false;

                    foreach (var newOrder in newOrders)
                    {
                        newOrder.Time = Game.RawGameTime;
                    }

                    Cache.Clear();
                    IsRunning = false;
                });
            }
            catch (Exception e)
            {
                Cache.Clear();
                IsRunning = false;

                Log.Error(e);
            }
        }

        private void EnabledPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (this.Config.OrderLimiterEnabled)
            {
                EntityManager<Entity>.EntityRemoved += EntityRemoved;
                Player.OnExecuteOrder += OnExecuteOrder;
            }
            else
            {
                EntityManager<Entity>.EntityRemoved -= EntityRemoved;
                Player.OnExecuteOrder -= OnExecuteOrder;
            }
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            GraveChill = AbilityFactory.GetAbility<visage_grave_chill>();
            SoulAssumption = AbilityFactory.GetAbility<visage_soul_assumption>();

            Context.Inventory.Attach(this);

            Config = new Config(this);

            LaneHelper = new LaneHelper(this);

            Updater = new Updater(this);

            foreach (var feature in features)
            {
                feature.Activate(Config);
                Log.Debug($"{feature.ToString()} activated.");
            }

            if (this.Config.OrderLimiterEnabled)
            {
                EntityManager<Entity>.EntityRemoved += EntityRemoved;
                Player.OnExecuteOrder += OnExecuteOrder;
            }

            RendererManager.Draw += OnDraw;
            this.Config.OrderLimiterEnabled.PropertyChanged += EnabledPropertyChanged;
        }

        protected override void OnDeactivate()
        {
            base.OnDeactivate();
            Updater?.Dispose();
            Context.Inventory.Detach(this);
            foreach (var feature in features)
            {
                feature.Dispose();
            }

            if (this.Config.OrderLimiterEnabled)
            {
                EntityManager<Entity>.EntityRemoved -= EntityRemoved;
                Player.OnExecuteOrder -= OnExecuteOrder;
            }

            this.Config.OrderLimiterEnabled.PropertyChanged -= EnabledPropertyChanged;
            Config?.Dispose();
            RendererManager.Draw -= OnDraw;
        }

        private void OnDraw(IRenderer renderer1)
        {
            if (Config.DrawInformationTab)
            {
                try
                {
                    var startPos = new Vector2(this.Config.PosX.Value, this.Config.PosY.Value);

                    var combo = Config.ComboKey;
                    renderer1.DrawText(startPos,
                        "Combo" + " [" + Utils.KeyToText(Config.ComboKey.Item.GetValue<KeyBind>().Key) + "] " +
                        (combo ? "ON" : "OFF"), combo ? System.Drawing.Color.LawnGreen : System.Drawing.Color.Red, Config.TextSize);

                    var lastHit = Config.LasthitKey;
                    renderer1.DrawText(startPos + new Vector2(0, 30),
                        "Lane Push" + " [" + Utils.KeyToText(Config.LasthitKey.Item.GetValue<KeyBind>().Key) + "] " +
                        (lastHit ? "ON" : "OFF"), lastHit ? System.Drawing.Color.LawnGreen : System.Drawing.Color.Red, Config.TextSize);

                    var follow = Config.FollowKey;
                    renderer1.DrawText(startPos + new Vector2(0, 60),
                        "Follow" + " [" + Utils.KeyToText(Config.FollowKey.Item.GetValue<KeyBind>().Key) + "] " +
                        (follow ? "ON" : "OFF"), follow ? System.Drawing.Color.LawnGreen : System.Drawing.Color.Red, Config.TextSize);

                    if (Config.FamiliarTarget != null)
                    {
                        var hero = LoadHeroTexture(Config.FamiliarTarget.HeroId);
                        renderer1.DrawTexture(Config.FamiliarTarget.HeroId.ToString(), new RectangleF(startPos.X + 25, startPos.Y - 75, 100, 66));
                        renderer1.DrawRectangle(new RectangleF(startPos.X + 22, startPos.Y - 77, 103, 69),
                            this.Config.FamiliarsLock.Item.IsActive() ? System.Drawing.Color.LawnGreen : System.Drawing.Color.Red, 3f);
                    }
                    else
                    {
                        var hero = LoadEmptyTexture();
                        renderer1.DrawTexture("default", new RectangleF(startPos.X + 25, startPos.Y - 75, 100, 66));
                        renderer1.DrawRectangle(new RectangleF(startPos.X + 22, startPos.Y - 77, 103, 69),
                            this.Config.FamiliarsLock.Item.IsActive() ? System.Drawing.Color.LawnGreen : System.Drawing.Color.Red, 3f);
                    }
                }
                catch
                {
                    // Works correctly but takes some time to load the texture and throws exception in the meantime. Since there's no "IsTextureLoaded" kind of thing,
                    // surpressing all errors make sense, I guess. 
                    // todo: load textures onactivate and on hero visible, add them to an array and don't load it twice.
                }
            }
        }

        public async Task LoadHeroTexture(HeroId id)
        {
            RendererManager.TextureManager.LoadFromDota($"{id}", $@"panorama/images/heroes/{id}_png.vtex_c");
            await Task.Delay(150);
        }

        public async Task LoadEmptyTexture()
        {
            RendererManager.TextureManager.LoadFromDota($"default", $@"panorama/images/heroes/npc_dota_hero_default_png.vtex_c");
            await Task.Delay(150);
        }


        public async Task<bool> ShouldExecute(CancellationToken token)
        {
            if (!Context.Owner.IsAlive || Context.Owner.IsChanneling())
            {
                Config.Target = null;
                await Task.Delay(125, token);
                return false;
            }

            return true;
        }

        public bool IsItemEnabled(AbilityId id)
        {
            return Config.AbilitiesInCombo.GetValue<AbilityToggler>("Items: ").IsEnabled(id.ToString());
        }

        public bool IsLinkenBreakerEnabled(AbilityId id)
        {
            return Config.AbilitiesInCombo.GetValue<AbilityToggler>("Linken breaker: ").IsEnabled(id.ToString());
        }

        public bool IsLinkenBreakerEnabled(string name)
        {
            return Config.AbilitiesInCombo.GetValue<AbilityToggler>("Linken breaker: ").IsEnabled(name);
        }

        public bool IsAbilityEnabled(AbilityId id)
        {
            return Config.AbilitiesInCombo.GetValue<AbilityToggler>("Abilities: ").IsEnabled(id.ToString());
        }

        public bool IsItemEnabled(ActiveAbility ability)
        {
            return IsItemEnabled(ability.Item.Id);
        }

        public bool IsLinkenBreakerEnabled(ActiveAbility ability)
        {
            return IsLinkenBreakerEnabled(ability.Item.Id);
        }
    }
}