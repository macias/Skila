using NaiveLanguageTools.Common;
using Skila.Language.Comparers;
using Skila.Language.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Language.Extensions
{
    public static class FunctionDefinitionExtension
    {
        public static bool NOT_USED_CounterpartParameters(this FunctionDefinition @this, FunctionDefinition other)
        {
            if (@this.Parameters.Count != other.Parameters.Count)
                return false;

            foreach (var pair in @this.Parameters.SyncZip(other.Parameters))
                if (!pair.Item1.NOT_USED_CounterpartParameter(@this, pair.Item2, other))
                    return false;

            return true;
        }
        public static bool IsOutConverter(this FunctionDefinition @this)
        {
            return @this.Name.Name == NameFactory.ConvertFunctionName
                && @this.Name.Arity == 0
                && @this.Parameters.Count == 0;
        }
        public static bool IsConstructor(this FunctionDefinition @this)
        {
            return @this.IsZeroConstructor() || @this.IsNewConstructor() || @this.IsInitConstructor();
        }
        public static bool IsInitConstructor(this FunctionDefinition @this)
        {
            return @this.Name.Name == NameFactory.InitConstructorName;
        }
        public static bool IsNewConstructor(this FunctionDefinition @this)
        {
            return @this.Name.Name == NameFactory.NewConstructorName;
        }
        public static bool IsZeroConstructor(this FunctionDefinition @this)
        {
            return @this.Name.Name == NameFactory.ZeroConstructorName;
        }
        public static bool IsDefaultInitConstructor(this FunctionDefinition @this)
        {
            return @this.IsInitConstructor() && @this.Parameters.Count == 0;
        }
        public static bool IsCopyInitConstructor(this FunctionDefinition @this)
        {
            return @this.IsInitConstructor() && @this.Parameters.Count == 1
                && @this.Parameters.Single().TypeName.Evaluation.Components == @this.OwnerType().InstanceOf;
        }

        internal static bool IsOverloadedDuplicate(FunctionDefinition f1, FunctionDefinition f2)
        {
            // since in case of functions type parameters can be inferred it is better to exclude arity
            // when checking if two functions are duplicates -- let the function parameter types decide
            if (!EntityBareNameComparer.Instance.Equals(f1.Name, f2.Name))
                return false;

            {   // linear check of anonymous parameters types

                // we move optional parameters at the end, because they have to be at the end (among anonymous ones)
                IEnumerable<FunctionParameter> f1_params = f1.Parameters.Where(it => !it.IsNameRequired)
                    .OrderBy(it => it.IsOptional)
                    .Concat((FunctionParameter)null); // terminal for easier checking
                IEnumerable<FunctionParameter> f2_params = f2.Parameters.Where(it => !it.IsNameRequired)
                    .OrderBy(it => it.IsOptional)
                    .Concat((FunctionParameter)null);

                foreach (Tuple<FunctionParameter, FunctionParameter> param_pair in f1_params.Zip(f2_params, (a, b) => Tuple.Create(a, b)))
                {
                    if (param_pair.Item1 == null)
                    {
                        if (param_pair.Item2 == null || param_pair.Item2.IsOptional)
                            break;
                        // when we hit terminal and in the other function we still have non-optional we have a difference
                        else
                            return false;
                    }
                    if (param_pair.Item2 == null)
                    {
                        if (param_pair.Item1 == null || param_pair.Item1.IsOptional)
                            break;
                        else
                            return false;
                    }

                    if (param_pair.Item1.IsOptional && param_pair.Item2.IsOptional)
                        break;

                    if (param_pair.Item1.TypeName.Evaluation.Components.IsOverloadDistinctFrom(param_pair.Item2.TypeName.Evaluation.Components)
                        || param_pair.Item1.IsVariadic != param_pair.Item2.IsVariadic)
                        return false;
                }
            }

            {
                // checking non-optional parameters with required names
                Dictionary<ITemplateName, FunctionParameter> f1_params = f1.Parameters.Where(it => it.IsNameRequired && !it.IsOptional)
                    .ToDictionary(it => it.Name, it => it, EntityBareNameComparer.Instance);
                IEnumerable<FunctionParameter> f2_params = f2.Parameters.Where(it => it.IsNameRequired && !it.IsOptional).StoreReadOnly();

                if (f1_params.Count() != f2_params.Count())
                    return false;

                foreach (FunctionParameter f2_param in f2_params)
                {
                    FunctionParameter f1_param;
                    if (!f1_params.TryGetValue(f2_param.Name, out f1_param))
                        return false;
                    if (f1_param.TypeName.Evaluation.Components.IsOverloadDistinctFrom(f2_param.TypeName.Evaluation.Components))
                        return false;
                }
            }

            if (f1.IsOutConverter() && f2.IsOutConverter())
                if (f1.ResultTypeName.Evaluation.Components.IsOverloadDistinctFrom(f2.ResultTypeName.Evaluation.Components))
                    return false;

            return true;
        }

        public static bool IsDerivedOf(ComputationContext ctx, FunctionDefinition derivedFunc,
            FunctionDefinition baseFunc, EntityInstance baseTemplate)
        {
            // todo: we have to check constraints as well
            if (!EntityNameArityComparer.Instance.Equals(derivedFunc.Name, baseFunc.Name))
                return false;

            foreach (Tuple<TemplateParameter, TemplateParameter> param_pair in derivedFunc.Name.Parameters
                .SyncZip(baseFunc.Name.Parameters))
            {
                if (!TemplateParameterExtension.IsSame(param_pair.Item1, param_pair.Item2, baseTemplate))
                    return false;
            }

            {
                IEntityInstance base_result_type = baseFunc.ResultTypeName.Evaluation.Components.TranslateThrough(baseTemplate);
                if (derivedFunc.ResultTypeName.Evaluation.Components.MatchesTarget(ctx, base_result_type, allowSlicing: false) != TypeMatch.Pass)
                    return false;

            }

            if (derivedFunc.Parameters.Count != baseFunc.Parameters.Count)
                return false;

            foreach (Tuple<FunctionParameter, FunctionParameter> param_pair in derivedFunc.Parameters.SyncZip(baseFunc.Parameters))
            {
                if (!FunctionParameterExtension.IsDerivedOf(ctx, param_pair.Item1, param_pair.Item2, baseTemplate))
                    return false;
            }

            return true;
        }

        public static bool IsSame(ComputationContext ctx, FunctionDefinition derivedFunc,
            FunctionDefinition baseFunc, EntityInstance baseTemplate)
        {
            // todo: we have to check constraints as well
            if (!EntityNameArityComparer.Instance.Equals(derivedFunc.Name, baseFunc.Name))
                return false;

            foreach (Tuple<TemplateParameter, TemplateParameter> param_pair in derivedFunc.Name.Parameters
                .SyncZip(baseFunc.Name.Parameters))
            {
                if (!TemplateParameterExtension.IsSame(param_pair.Item1, param_pair.Item2, baseTemplate))
                    return false;
            }

            {
                IEntityInstance base_result_type = baseFunc.ResultTypeName.Evaluation.Components.TranslateThrough(baseTemplate);
                if (!derivedFunc.ResultTypeName.Evaluation.Components.IsSame(base_result_type, jokerMatchesAll:true))
                    return false;

            }

            if (derivedFunc.Parameters.Count != baseFunc.Parameters.Count)
                return false;

            foreach (Tuple<FunctionParameter, FunctionParameter> param_pair in derivedFunc.Parameters.SyncZip(baseFunc.Parameters))
            {
                if (!FunctionParameterExtension.IsSame(ctx, param_pair.Item1, param_pair.Item2, baseTemplate))
                    return false;
            }

            return true;
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
