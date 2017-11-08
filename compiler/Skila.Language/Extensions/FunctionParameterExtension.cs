namespace Skila.Language.Extensions
{
    public static class FunctionParameterExtension
    {
        public static bool IsDerivedOf(ComputationContext ctx, FunctionParameter derivedParam, FunctionParameter baseParam)
        {
            // unlike the result type, for parameters we check strict matching, this is because
            // we could have overloaded function in base type, and then figuring out
            // which function in derived type inherits from which base function would take longer
            // when we allowed more relaxed matching (i.e. allowing to have supertypes as arguments when deriving)
            if (!baseParam.Evaluation.IsSame(derivedParam.Evaluation, jokerMatchesAll: true))
                return false;

            if (baseParam.IsVariadic != derivedParam.IsVariadic)
                return false;

            if (baseParam.IsVariadic)
            {
                if (!baseParam.Variadic.IsWithinLimits(derivedParam.Variadic.MinLimit)
                    || !baseParam.Variadic.IsWithinLimits(derivedParam.Variadic.MaxLimit))
                    return false;
            }

            return true;
        }
    }

}
