using Skila.Language.Entities;
using Skila.Language.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Language
{
    public interface IEntityInstance
    {
#if DEBUG
        DebugId DebugId { get; }
#endif

        INameReference NameOf { get; }

        IEnumerable<EntityInstance> EnumerateAll();
        bool IsJoker { get; }

        bool IsValueType(ComputationContext ctx);

        IEntityInstance TranslateThrough(ref bool translated, TemplateTranslation closedTranslation);
        IEntityInstance TranslationOf(IEntityInstance openTemplate, ref bool translated, TemplateTranslation closedTranslation);
        ConstraintMatch ArgumentMatchesParameterConstraints(ComputationContext ctx, EntityInstance closedTemplate, TemplateParameter param);

        TypeMatch TemplateMatchesTarget(ComputationContext ctx, IEntityInstance target, VarianceMode variance, TypeMatching matching);
        TypeMatch TemplateMatchesInput(ComputationContext ctx, EntityInstance input, VarianceMode variance, TypeMatching matching);

        // are types can be assigned or passed
        TypeMatch MatchesTarget(ComputationContext ctx, IEntityInstance target, TypeMatching matching);
        TypeMatch MatchesInput(ComputationContext ctx, EntityInstance input, TypeMatching matching);

        bool IsStrictDescendantOf(ComputationContext ctx, EntityInstance ancestor);
        bool IsStrictAncestorOf(ComputationContext ctx, IEntityInstance descendant);

        bool IsSame(IEntityInstance other, bool jokerMatchesAll);
        // checks if types are distinct from each other for function overloading validation
        bool IsOverloadDistinctFrom(IEntityInstance other);

        bool CoreEquals(IEntityInstance instance);
        IEntityInstance Map(Func<EntityInstance, IEntityInstance> func);
    }
}