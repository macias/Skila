using System.Diagnostics;

namespace Skila.Language
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public struct TypeAncestor
    {
        public EntityInstance AncestorInstance { get; }
        public int Distance { get; } // 0 for itself, 1 for parents, 2 for parents of the parents...

        public TypeAncestor(EntityInstance instance, int distance)
        {
            this.AncestorInstance = instance;
            this.Distance = distance;
        }

        public override string ToString()
        {
            return $"{AncestorInstance}({Distance})";
        }
    }
}
