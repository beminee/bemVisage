using System;
using System.Collections.Generic;
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
using Ensage.SDK.Helpers;
using UnitExtensions = Ensage.SDK.Extensions.UnitExtensions;

namespace bemVisage
{
    [ExportPlugin(name: "bemVisage", mode: StartupMode.Auto, units: HeroId.npc_dota_hero_visage)]
    public class BemVisage : Plugin
    {
        [ImportMany] private IEnumerable<IFeature> features;

        public IServiceContext Context { get; }
        public IRendererManager RendererManager { get; set; }
        //private ITextureManager TextureManager { get; set; }
        //private List<string> LoadedHeroes { get; set; }

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
            RendererManager = context.Renderer;
            AbilityFactory = context.AbilityFactory;
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

            //if (EntityManager<Hero>.Entities.Any(x => x != null && x.IsValid && UnitExtensions.IsEnemy(x, this.Context.Owner)))
            //{
            //    foreach (var hero in EntityManager<Hero>.Entities)
            //    {
            //        //AddTexture(hero);
            //        Log.Debug($"{hero.Name}");
            //    }
            //}

            foreach (var feature in features)
            {
                feature.Activate(Config);
                Log.Debug($"{feature.ToString()} activated.");
            }
            //EntityManager<Hero>.EntityAdded += EntityManagerOnEntityAdded;
            RendererManager.Draw += OnDraw;
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

            Config?.Dispose();
            //EntityManager<Hero>.EntityAdded -= EntityManagerOnEntityAdded;
            RendererManager.Draw -= OnDraw;
        }

        private void OnDraw(object sender, EventArgs e)
        {
            if (Config.DrawInformationTab)
            {
                var startPos = new Vector2(Convert.ToSingle(Drawing.Width) - 175,
                    Convert.ToSingle(Drawing.Height * 0.8));

                var combo = Config.ComboKey;
                RendererManager.DrawText(startPos,
                    "Combo" + " [" + Utils.KeyToText(Config.ComboKey.Item.GetValue<KeyBind>().Key) + "] " +
                    (combo ? "ON" : "OFF"), combo ? System.Drawing.Color.LawnGreen : System.Drawing.Color.Red, Config.TextSize);

                var lastHit = Config.LasthitKey;
                RendererManager.DrawText(startPos + new Vector2(0, 30),
                    "Lane Push" + " [" + Utils.KeyToText(Config.LasthitKey.Item.GetValue<KeyBind>().Key) + "] " +
                    (lastHit ? "ON" : "OFF"), lastHit ? System.Drawing.Color.LawnGreen : System.Drawing.Color.Red, Config.TextSize);

                var follow = Config.FollowKey;
                RendererManager.DrawText(startPos + new Vector2(0, 60),
                    "Follow" + " [" + Utils.KeyToText(Config.FollowKey.Item.GetValue<KeyBind>().Key) + "] " +
                    (follow ? "ON" : "OFF"), follow ? System.Drawing.Color.LawnGreen : System.Drawing.Color.Red, Config.TextSize);

                //if (Config.ComboKey && Config.Target != null)
                //{
                //    RendererManager.DrawTexture($"{Config.Target.Name}", new SharpDX.RectangleF(startPos.X, startPos.Y - 90, 150, 85));
                //    RendererManager.DrawRectangle(new SharpDX.RectangleF(startPos.X, startPos.Y - 30, 155, 155), System.Drawing.Color.Red);
                //}
            }
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

        //private void EntityManagerOnEntityAdded(object sender, Hero unit)
        //{
        //    AddTexture(unit);
        //}

        //private void AddTexture(Hero hero)
        //{
        //    if (LoadedHeroes.Contains(hero.Name) || hero.Team == this.Context.Owner.Team)
        //    {
        //        return;
        //    }

        //    TextureManager.LoadFromDota($"{hero.Name}", $"panorama/images/heroes/{hero.Name}_png.vtex_c");
        //    Log.Debug($"Loaded {hero.Name} texture");
        //    LoadedHeroes.Add(hero.Name);
        //}

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