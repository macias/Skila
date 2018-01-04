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
        bool DependsOnTypeParameter { get; }

        IEnumerable<EntityInstance> Enumerate();
        bool IsJoker { get; }

        bool IsValueType(ComputationContext ctx);

        IEntityInstance TranslateThrough(EntityInstance closedTemplate, ref bool translated);
        IEntityInstance TranslationOf(IEntityInstance openTemplate, ref bool translated);
        ConstraintMatch ArgumentMatchesParameterConstraints(ComputationContext ctx, EntityInstance verifiedInstance, TemplateParameter param);

        TypeMatch TemplateMatchesTarget(ComputationContext ctx, bool inversedVariance, IEntityInstance target, VarianceMode variance, bool allowSlicing);
        TypeMatch TemplateMatchesInput(ComputationContext ctx, bool inversedVariance, EntityInstance input, VarianceMode variance, bool allowSlicing);

        // are types can be assigned or passed
        TypeMatch MatchesTarget(ComputationContext ctx, IEntityInstance target, bool allowSlicing);
        TypeMatch MatchesInput(ComputationContext ctx, EntityInstance input, bool allowSlicing);

        bool IsStrictDescendantOf(ComputationContext ctx, EntityInstance ancestor);
        bool IsStrictAncestorOf(ComputationContext ctx, IEntityInstance descendant);

        bool IsSame(IEntityInstance other, bool jokerMatchesAll);
        // checks if types are distinct from each other for function overloading validation
        bool IsOverloadDistinctFrom(IEntityInstance other);

        bool CoreEquals(IEntityInstance instance);
    }  
}