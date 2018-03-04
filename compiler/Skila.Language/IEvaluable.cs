using Skila.Language.Semantics;

namespace Skila.Language
{
    public interface IEvaluable : IValidable,IComputable
    {
        EvaluationInfo Evaluation { get; }
        ValidationData Validation { get; set; }
    }

}
