using Skila.Language.Semantics;

namespace Skila.Language
{
    public interface IComputable : INode
    {
        bool IsComputed { get; }

        void Evaluate(ComputationContext ctx);
    }
}
