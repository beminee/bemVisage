using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using bemVisage;
using Ensage;
using Ensage.SDK.Helpers;
using Ensage.SDK.Service;
using log4net;
using PlaySharp.Toolkit.Logging;
using bemVisage.Core;
using Ensage.SDK.Extensions;
using Ensage.SDK.Handlers;

namespace bemVisage
{
    public class Updater : IDisposable
    {
        private static readonly ILog Log = AssemblyLogs.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly BemVisage _main;

        private readonly Dictionary<int, Familiar> allFamiliars = new Dictionary<int, Familiar>();

        private readonly Dictionary<int, OtherUnits> allOtherUnits = new Dictionary<int, OtherUnits>();

        private IUpdateHandler Handler;

        public IEnumerable<Familiar> AllFamiliars
        {
            get
            {
                return allFamiliars.Values.Where(x =>
                    x.Unit.IsValid && x.Unit.IsAlive && x.Unit.Team == _main.Context.Owner.Team &&
                    x.Unit.IsControllable);
            }
        }

        public IEnumerable<OtherUnits> AllOtherUnits
        {
            get
            {
                return allOtherUnits.Values.Where(x =>
                    x.Unit.IsValid && x.Unit.IsAlive && x.Unit.Team == _main.Context.Owner.Team &&
                    x.Unit.IsControllable);
            }
        }

        public Updater(BemVisage main)
        {
            _main = main;

            try
            {
                foreach (var unit in EntityManager<Unit>.Entities)
                {
                    if (unit.NetworkName.Contains("CDOTA_Unit_VisageFamiliar") && unit.Team == main.Context.Owner.Team)
                    {
                        allFamiliars[unit.Handle.Handle] = new Familiar(_main, unit);
                        Log.Debug($"new familiar found");
                    }
                    if (unit.NetworkName.Contains("CDOTA_BaseNPC_Creep_Neutral") && unit.Team == main.Context.Owner.Team
                        && unit.IsControllable && unit.IsControllableByPlayer(ObjectManager.LocalPlayer))
                    {
                        allOtherUnits[unit.Handle.Handle] = new OtherUnits(_main, unit);
                        Log.Debug($"new unit found");
                    }
                }

                Handler = UpdateManager.Subscribe(OnUpdate);
            }
            catch
            {
                //Ignore
            }

            EntityManager<Unit>.EntityAdded += EntityManagerOnEntityAdded;
            EntityManager<Unit>.EntityRemoved += EntityManagerOnEntityRemoved;
        }

        private void OnUpdate()
        {
            foreach (var unit in EntityManager<Unit>.Entities.Where(x => x != null && x.IsValid && x.IsAlly(_main.Context.Owner) && x.IsControllable &&
                                                                         x.IsControllableByPlayer(ObjectManager.LocalPlayer) && x.NetworkName.Contains("CDOTA_BaseNPC_Creep_Neutral")))
            {
                if (!allOtherUnits.ContainsKey(unit.Handle.Handle))
                {
                    allOtherUnits[unit.Handle.Handle] = new OtherUnits(_main, unit);
                    Log.Debug($"new unit added");
                }
            }
        }

        private void EntityManagerOnEntityAdded(object sender, Unit unit)
        {
            if (!unit.IsValid || unit.Team != _main.Context.Owner.Team || !unit.NetworkName.Contains("CDOTA_Unit_VisageFamiliar"))
            {
                return;
            }
            allFamiliars[unit.Handle.Handle] = new Familiar(_main, unit);
            Log.Debug($"new familiar added");
        }

        private void EntityManagerOnEntityRemoved(object sender, Unit unit)
        {
            var handle = unit.Handle.Handle;
            if (!allFamiliars.ContainsKey(handle) || allOtherUnits.ContainsKey(handle))
            {
                return;
            }

            if (allFamiliars.ContainsKey(handle))
            {
                allFamiliars.Remove(handle);
            }
            else if (allOtherUnits.ContainsKey(handle))
            {
                allOtherUnits.Remove(handle);
            }
        }

        public void Dispose()
        {
            EntityManager<Hero>.EntityAdded -= EntityManagerOnEntityAdded;
            EntityManager<Unit>.EntityRemoved -= EntityManagerOnEntityRemoved;
            UpdateManager.Unsubscribe(OnUpdate);
            allFamiliars.Clear();
            allOtherUnits.Clear();
        }
    }
}