using NaiveLanguageTools.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Language.Extensions
{
    public static class TemplateParameterExtension
    {
        internal static bool IsDerivedOf(TemplateParameter derivedParam, TemplateParameter baseParam)
        {
            if (baseParam.ConstraintModifier.HasConst != derivedParam.ConstraintModifier.HasConst)
                return false;

            HashSet<IEntityInstance> derived_bases = derivedParam.BaseOfNames.Select(it => it.Evaluation).ToHashSet();
            if (!derived_bases.SetEquals(baseParam.BaseOfNames.Select(it => it.Evaluation)))
                return false;

            HashSet<IEntityInstance> derived_inherits = derivedParam.InheritsNames.Select(it => it.Evaluation).ToHashSet();
            if (!derived_inherits.SetEquals(baseParam.InheritsNames.Select(it => it.Evaluation)))
                return false;

            return true;
        }
    }
}