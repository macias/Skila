using Skila.Language.Semantics;

namespace Skila.Language
{
    public interface IEvaluable : IValidable
    {
        EvaluationInfo Evaluation { get; }
        ValidationData Validation { get; set; }

        bool IsComputed { get; }

        void Evaluate(ComputationContext ctx);
    }

}
