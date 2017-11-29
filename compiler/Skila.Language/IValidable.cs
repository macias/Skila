using Skila.Language.Semantics;

namespace Skila.Language
{
    // "lighter" version of IEvaluable
    public interface IValidable : INode
    {
        void Validate(ComputationContext ctx);
    }

}
