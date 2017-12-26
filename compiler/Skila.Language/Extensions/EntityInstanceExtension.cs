using Skila.Language.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Language.Extensions
{
    public static class EntityInstanceExtension
    {
        public static IEntity Target(this IEntityInstance instance)
        {
            if (instance is EntityInstance)
                return (instance as EntityInstance).Target;
            else if (instance is EntityInstanceUnion)
                return (instance as EntityInstanceUnion).Instances.Single().Target();
            else
                throw new NotImplementedException();
        }
        public static IEnumerable<EntityInstance> PrimaryAncestors(this EntityInstance instance, ComputationContext ctx)
        {
            EntityInstance primary_parent = instance.Inheritance(ctx).MinimalParentsWithObject.FirstOrDefault();
            if (primary_parent == null)
                return Enumerable.Empty<EntityInstance>();
            else
                return new[] { primary_parent }.Concat(primary_parent.PrimaryAncestors(ctx));
        }

        public static bool IsOfType(this EntityInstance instance, TypeDefinition target)
        {
            return /*instance.IsJoker ||*/ (instance.Target.IsType() && target == instance.Target);
        }

        public static VirtualTable BuildDuckVirtualTable(ComputationContext ctx, EntityInstance input, EntityInstance target,
            // this is used when building for types intersection, no single type can cover entire aggregated type
            // so we build only partial virtual table for each element of type intersection
            // for example consider "*Double & *String" -- such super type would have "sqrt" function and "substr" as well
            // but bulting regular vtable Double->DoubleString would be impossible, because (for example) String does not have "sqrt"
            bool allowPartial)
        {
            VirtualTable vtable;
            if (!input.TryGetDuckVirtualTable(target, out vtable))
            {
                Dictionary<FunctionDefinition, FunctionDefinition> mapping
                    = TypeDefinitionExtension.PairDerivations(ctx, target, input.TargetType.NestedFunctions)
                    .Where(it => it.Derived != null)
                    .ToDictionary(it => it.Base, it => it.Derived);

                bool is_partial = false;

                foreach (FunctionDefinition base_func in target.TargetType.NestedFunctions)
                    if (base_func.Modifier.HasAbstract && !mapping.ContainsKey(base_func))
                    {
                        if (allowPartial)
                            is_partial = true;
                        else
                        {
                            mapping = null;
                            break;
                        }
                    }

                if (mapping != null)
                {
                    vtable = new VirtualTable(mapping, is_partial);
                    input.AddDuckVirtualTable(target, vtable);
                }
            }

            return vtable;
        }


    }
    /*
        public static IEnumerable<FunctionDefinition> FilterFunctionsLike(this IEnumerable<FunctionDefinition> __this__,
         TypeDefinition thisSelfSubsitution,
         TypeDefinition funcOwner,
         bool funcStatic,
         string funcName, ITemplateParameters funcSpec, IFunctionParameters funcParams,
         IFunctionOutcome funcOutcome,
         FunctionMatchMode checkMode)
        {
            return __this__.Where(it => it.HasSameOverrideSignature(thisSelfSubsitution, funcOwner, funcStatic, funcName, funcSpec,
                funcParams, funcOutcome, checkMode) == DerivationModeInfo.DerivationMatchEnum.RegularMatch);
        }
        public static DerivationModeInfo.DerivationMatchEnum HasSameOverrideSignature(this IQueryMember __this__, 
            TypeDefinition thisSelfSubsitution,
          TypeDefinition baseOwner,
          bool baseStatic,
          string baseName,
          ITemplateParameters baseSpec,
          IFunctionParameters baseParams,
          IFunctionOutcome baseOutcome,
          FunctionMatchMode checkMode) // use only when detecting if function overrides each other
        {
            // [COORDINATES PROBLEM]
            // this is actuall a desparate escape from problem such as
            // consider Tuple<T> and some method bar<X> which in its body uses Tuple<X>
            // so Tuple will get an instance Tuple<X> and there will be problem with computing methods of Tuple<X>
            // because of type coordinates -- X originates from function bar, so it is outside Tuple
            // so within Tuple it is impossible to compute coordinates of X
            // "of course" everything is technically possible but it is more complex than such if as below
            // which for now also solves the problem

            // small exception for init-->new, it is not overriden version technically, but it can be seen this way
            bool new_overrides_init = checkMode.HasFlag(FunctionMatchMode.NewOverridesInit)
                && baseName.IsEqual(TreeConstants.InitConstructorString) && __this__.IsEitherNewConstructor;

            if (!new_overrides_init)
            {
                if (__this__.ModifierComputed.HasStaticFlag != baseStatic
                    // in namespace don't pay attention to static, because it does not matter HERE
                    && __this__.TypeOwner != null)
                    return DerivationModeInfo.DerivationMatchEnum.None;

                if (!__this__.HasSameName(baseName))
                    return DerivationModeInfo.DerivationMatchEnum.None;

                if (checkMode.HasFlag(FunctionMatchMode.CheckOutcome)
                    &&
                    (isAncestorOrSameReferenceType(baseOutcome, baseOutcome.ComputedReturnTypeName, __this__.Outcome, __this__.Outcome.ComputedReturnTypeName, thisSelfSubsitution,
                    substituteSelf: checkMode.HasFlag(FunctionMatchMode.SubstituteSelfOutcome), allowTypeDerivation: true) == DerivationModeInfo.DerivationMatchEnum.None
                    || baseOutcome.Usage != __this__.Outcome.Usage))
                {
                    return DerivationModeInfo.DerivationMatchEnum.None;
                }
            }

            if (__this__.TemplateParamElements.Count() != baseSpec.GetElements().Count())
                return DerivationModeInfo.DerivationMatchEnum.None;

            // in conversion result types have to be identical for override
            if (checkMode.HasFlag(FunctionMatchMode.CheckOutcome)
                && __this__.IsConverter
                && !isSameReferenceType(__this__.Outcome.ComputedReturnTypeName,
                    thisSelfSubsitution,
                    baseOutcome.ComputedReturnTypeName,
                    strictMutability: false,
                    substituteSelf: checkMode.HasFlag(FunctionMatchMode.SubstituteSelfOutcome)))
                return DerivationModeInfo.DerivationMatchEnum.None;



            DerivationModeInfo.DerivationMatchEnum result = parametersMatchesDerivation(__this__, thisSelfSubsitution, baseParams, checkMode);
            return result;
        }
    }*/
}
