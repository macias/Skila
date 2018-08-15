using Skila.Language.Semantics;

namespace Skila.Language
{
    public interface IComputable : IOwnedNode
    {
        bool IsComputed { get; }

        void Evaluate(ComputationContext ctx);
    }
}
