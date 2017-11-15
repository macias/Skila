using Skila.Language.Semantics;

namespace Skila.Language
{
    public interface IEvaluable : INode
    {
        IEntityInstance Evaluation { get; }
        ValidationData Validation { get; set; }

        bool IsComputed { get; }

        void Evaluate(ComputationContext ctx);
        void Validate(ComputationContext ctx);
    }

}
