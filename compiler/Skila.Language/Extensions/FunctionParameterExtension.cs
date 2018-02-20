namespace Skila.Language.Extensions
{
    public static class FunctionParameterExtension
    {
        public static bool IsDerivedOf(ComputationContext ctx, FunctionParameter derivedParam, 
            FunctionParameter baseParam,EntityInstance baseTemplate)
        {
            // unlike the result type, for parameters we check strict matching, this is because
            // we could have overloaded function in base type, and then figuring out
            // which function in derived type inherits from which base function would take longer
            // when we allowed more relaxed matching (i.e. allowing to have supertypes as arguments when deriving)
            IEntityInstance base_param_type = baseParam.Evaluation.Components.TranslateThrough(baseTemplate);
            if (!base_param_type.IsExactlySame(derivedParam.Evaluation.Components, jokerMatchesAll: true))
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

        public static bool IsSame(ComputationContext ctx, FunctionParameter derivedParam,
            FunctionParameter baseParam, EntityInstance baseTemplate)
        {
            // unlike the result type, for parameters we check strict matching, this is because
            // we could have overloaded function in base type, and then figuring out
            // which function in derived type inherits from which base function would take longer
            // when we allowed more relaxed matching (i.e. allowing to have supertypes as arguments when deriving)
            IEntityInstance base_param_type = baseParam.Evaluation.Components.TranslateThrough(baseTemplate);
            if (!base_param_type.IsExactlySame(derivedParam.Evaluation.Components, jokerMatchesAll: true))
                return false;

            if (baseParam.IsVariadic != derivedParam.IsVariadic)
                return false;

            if (baseParam.IsVariadic)
            {
                if (baseParam.Variadic.MinLimit!=derivedParam.Variadic.MinLimit
                    || baseParam.Variadic.MaxLimit!=derivedParam.Variadic.MaxLimit)
                    return false;
            }

            return true;
        }
    }

}
