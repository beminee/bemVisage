using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ensage;
using SharpDX;

namespace bemVisage.Utilities
{
    sealed class Order
    {
        public Entity Entity;

        public OrderId OrderId;

        public Vector3 TargetPosition;

        public Entity Target;

        public Ability Ability;

        public float Time;
    }
}
