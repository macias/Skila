using Skila.Language.Semantics;

namespace Skila.Language
{
    // "lighter" version of IEvaluable
    public interface IVerificable : INode
    {
        void Verify(ComputationContext ctx);
    }

}
