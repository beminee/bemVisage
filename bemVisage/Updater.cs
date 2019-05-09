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

namespace bemVisage
{
    public class Updater : IDisposable
    {
        private static readonly ILog Log = AssemblyLogs.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly BemVisage _main;

        private readonly Dictionary<int, Familiar> allFamiliars = new Dictionary<int, Familiar>();

        public IEnumerable<Familiar> AllFamiliars
        {
            get
            {
                return allFamiliars.Values.Where(x =>
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
                    }
                }
            }
            catch
            {
                //Ignore
            }

            EntityManager<Unit>.EntityAdded += EntityManagerOnEntityAdded;
            EntityManager<Unit>.EntityRemoved += EntityManagerOnEntityRemoved;
        }

        private void EntityManagerOnEntityAdded(object sender, Unit unit)
        {
            if (!unit.IsValid || !unit.NetworkName.Contains("CDOTA_Unit_VisageFamiliar") ||
                unit.Team != _main.Context.Owner.Team)
            {
                return;
            }

            allFamiliars[unit.Handle.Handle] = new Familiar(_main, unit);
            Log.Debug($"new familiar added");
        }

        private void EntityManagerOnEntityRemoved(object sender, Unit unit)
        {
            var handle = unit.Handle.Handle;
            if (!allFamiliars.ContainsKey(handle))
            {
                return;
            }

            allFamiliars.Remove(handle);
        }

        public void Dispose()
        {
            EntityManager<Hero>.EntityAdded -= EntityManagerOnEntityAdded;
            EntityManager<Unit>.EntityRemoved -= EntityManagerOnEntityRemoved;
            allFamiliars.Clear();
        }
    }
}