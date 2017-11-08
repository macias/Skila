using NaiveLanguageTools.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Language.Extensions
{
    public static class TemplateParameterExtension
    {
        internal static bool IsDerivedOf(TemplateParameter derivedParam, TemplateParameter baseParam, EntityInstance baseTemplate)
        {
            if (baseParam.ConstraintModifier.HasConst != derivedParam.ConstraintModifier.HasConst)
                return false;

            {
                HashSet<IEntityInstance> derived_bases = derivedParam.BaseOfNames
                    .Select(it => it.Evaluation).ToHashSet();
                IEnumerable<IEntityInstance> base_bases = baseParam.BaseOfNames
                    .Select(it => it.Evaluation.TranslateThrough(baseTemplate)).ToArray();
                if (!derived_bases.SetEquals(base_bases))
                    return false;
            }

            {
                HashSet<IEntityInstance> derived_inherits = derivedParam.InheritsNames
                    .Select(it => it.Evaluation).ToHashSet();
                IEnumerable<IEntityInstance> base_inherits = baseParam.InheritsNames
                    .Select(it => it.Evaluation.TranslateThrough(baseTemplate)).ToArray();
                if (!derived_inherits.SetEquals(base_inherits))
                    return false;
            }

            return true;
        }
    }
}