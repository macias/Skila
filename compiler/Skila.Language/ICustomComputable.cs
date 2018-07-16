using Skila.Language.Semantics;

namespace Skila.Language
{
    public interface ICustomComputable : IComputable
    {
        void CustomEvaluate(ComputationContext ctx);
    }

}
