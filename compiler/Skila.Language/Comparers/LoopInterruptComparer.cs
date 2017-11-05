using Skila.Language.Flow;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Skila.Language.Comparers
{
    public sealed class LoopInterruptComparer : IEqualityComparer<ILoopInterrupt>
    {
        public static IEqualityComparer<ILoopInterrupt> Instance = new LoopInterruptComparer();

        private LoopInterruptComparer()
        {

        }
        public bool Equals(ILoopInterrupt x, ILoopInterrupt y)
        {
            return x.IsBreak == y.IsBreak && x.AssociatedLoop == y.AssociatedLoop;
        }

        public int GetHashCode(ILoopInterrupt obj)
        {
            return RuntimeHelpers.GetHashCode(obj.AssociatedLoop) ^ obj.IsBreak.GetHashCode();
        }
    }
}
