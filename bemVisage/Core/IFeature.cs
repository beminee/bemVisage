using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bemVisage.Core
{
    [InheritedExport]
    internal interface IFeature : IDisposable
    {
        void Activate(Config main);
    }
}