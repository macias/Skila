using NaiveLanguageTools.Common;
using Skila.Language.Comparers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Language.Extensions
{
    public static class TemplateParameterExtension
    {
        internal static bool IsSame(TemplateParameter derivedParam, TemplateParameter baseParam, EntityInstance baseTemplate)
        {
            if (baseParam.Constraint.Modifier.HasConst != derivedParam.Constraint.Modifier.HasConst)
                return false;

            {
                HashSet<IEntityInstance> derived_bases = derivedParam.Constraint.BaseOfNames
                    .Select(it => it.Evaluation.Components).ToHashSet(EntityInstance.ComparerI);
                IEnumerable<IEntityInstance> base_bases = baseParam.Constraint.BaseOfNames
                    .Select(it => it.Evaluation.Components.TranslateThrough(baseTemplate)).ToArray();
                if (!derived_bases.SetEquals(base_bases))
                    return false;
            }

            {
                HashSet<IEntityInstance> derived_inherits = derivedParam.Constraint.InheritsNames
                    .Select(it => it.Evaluation.Components).ToHashSet(EntityInstance.ComparerI);
                IEnumerable<IEntityInstance> base_inherits = baseParam.Constraint.InheritsNames
                    .Select(it => it.Evaluation.Components.TranslateThrough(baseTemplate)).ToArray();

                if (!derived_inherits.SetEquals(base_inherits))
                    return false;
            }

            return true;
        }
    }
}