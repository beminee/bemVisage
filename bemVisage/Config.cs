using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using bemVisage;
using Ensage;
using Ensage.Common.Menu;
using Ensage.Common.Objects.UtilityObjects;
using Ensage.SDK.Handlers;
using Ensage.SDK.Menu;

namespace bemVisage
{
    public class Config : IDisposable
    {
        #region uselessstuf

        public bool disposed { get; set; }
        public MultiSleeper multiSleeper { get; }
        public BemVisage bemVisage { get; }
        public TaskHandler LinkenHandler { get; set; }

        #endregion

        #region MenuStuff

        public MenuFactory Factory { get; set; }
        public MenuFactory LanePushing { get; set; }
        public MenuFactory AbilitiesInCombo { get; set; }
        public MenuFactory LinkenBreaker { get; set; }
        public MenuFactory ComboMenu { get; set; }
        public MenuFactory Drawings { get; set; }
        public MenuFactory FamiliarMenu { get; set; }
        public MenuItem<KeyBind> ComboKey { get; set; }

        public MenuItem<StringList> TargetOption { get; set; }

        #endregion

        public Hero FamiliarTarget { get; set; }
        public Hero Target { get; set; }
        private VisageOrbwalking VisageOrbwalking { get; set; }
        public MenuItem<KeyBind> FamiliarsLock { get; set; }
        public MenuItem<KeyBind> FollowKey { get; set; }
        public MenuItem<KeyBind> LasthitKey { get; set; }
        public MenuItem<bool> BmBehavior { get; }
        public MenuItem<Slider> BlinkDistance2Mouse { get; set; }
        public MenuItem<Slider> BlinkDistance2Enemy { get; set; }
        public MenuItem<bool> SoulAssumptionDraw { get; set; }
        public MenuItem<bool> GraveChillsDraw { get; set; }
        public MenuItem<bool> DrawTargetIndicator { get; set; } 
        public MenuItem<bool> DrawInformationTab { get; set; }
        public MenuItem<Slider> PosX { get; set; }
        public MenuItem<Slider> PosY { get; set; }

        public MenuItem<Slider> TextSize { get; set; }

        public MenuItem<Slider> OrbwalkMinimumDistance { get; set; }

        public Config(BemVisage Main)
        {
            bemVisage = Main;
            NoobFailSafe();

            multiSleeper = new MultiSleeper();

            Factory = MenuFactory.Create("bemVisage");
            LanePushing = Factory.Menu("Lane Push");
            AbilitiesInCombo = Factory.Menu("Combo Abilities");
            Drawings = Factory.Menu("Drawings");
            LinkenBreaker = AbilitiesInCombo.Menu("Linken Breaker");
            ComboMenu = AbilitiesInCombo.Menu("Combo Menu");
            FamiliarMenu = Factory.Menu("Unit Menu");
            ComboKey = ComboMenu.Item("Combo", new KeyBind(32));
            TargetOption = ComboMenu.Item("Target Option", new StringList("Lock", "Default"));
            BmBehavior = ComboMenu.Item("Keep combo if Blade Mail", false);
            BlinkDistance2Mouse = ComboMenu.Item("Blink Distance to Mouse", new Slider(600, 0, 1200));
            BlinkDistance2Enemy = ComboMenu.Item("Blink Distance to Enemy", new Slider(250, 0, 550));
            //OrbwalkMinimumDistance = ComboMenu.Item("Orbwalker Minimum Distance", new Slider(500, 0, 600));
            //OrbwalkMinimumDistance.Item.Tooltip = "Set to 0 to disable";


            FamiliarsLock = FamiliarMenu.Item("Units Target Lock", new KeyBind('E', KeyBindType.Toggle, false));
            FollowKey = FamiliarMenu.Item("Follow Key", new KeyBind('F', KeyBindType.Toggle, false));

            LasthitKey = LanePushing.Item("Lane Push Key", new KeyBind('D', KeyBindType.Toggle, false));

            SoulAssumptionDraw = Drawings.Item("Draw Soul Assumption Range", true);
            GraveChillsDraw = Drawings.Item("Draw Grave Chills Range", true);
            DrawTargetIndicator = Drawings.Item("Draw target indicator", true);
            DrawInformationTab = Drawings.Item("Draw information tab", true);
            TextSize = Drawings.Item("Text size", new Slider(13, 1, 100));
            PosX = Drawings.Item("Drawing X", new Slider(1750, 0, 1800));
            PosY = Drawings.Item("Drawing Y", new Slider(850, 0, 1800));

            ComboKey.Item.ValueChanged += ComboKeyChanged;
            ComboKey.PropertyChanged += ComboKeyPropertyChanged;
            var key = KeyInterop.KeyFromVirtualKey((int) ComboKey.Value.Key);
            VisageOrbwalking = new VisageOrbwalking(key, this);
            bemVisage.Context.Orbwalker.RegisterMode(VisageOrbwalking);
        }

        private void NoobFailSafe()
        {
            var orbwalker = bemVisage.Context.Orbwalker;
            if (!orbwalker.IsActive)
            {
                orbwalker.Activate();
            }

            var targetSelector = bemVisage.Context.TargetSelector;
            if (!targetSelector.IsActive)
            {
                targetSelector.Activate();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void ComboKeyChanged(object sender, OnValueChangeEventArgs e)
        {
            var keyCode = e.GetNewValue<KeyBind>().Key;
            if (keyCode == e.GetOldValue<KeyBind>().Key)
            {
                return;
            }

            var key = KeyInterop.KeyFromVirtualKey((int) keyCode);
            VisageOrbwalking.Key = key;
        }

        private void ComboKeyPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (this.ComboKey.Item.IsActive())
            {
                FollowKey.Item.SetValue(new KeyBind(FollowKey.Value, KeyBindType.Toggle));
                LasthitKey.Item.SetValue(new KeyBind(LasthitKey.Value, KeyBindType.Toggle));
            }
        }


        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                bemVisage.Context.Orbwalker.UnregisterMode(VisageOrbwalking);
                ComboKey.Item.ValueChanged -= ComboKeyChanged;
            }

            disposed = true;
        }
    }
}