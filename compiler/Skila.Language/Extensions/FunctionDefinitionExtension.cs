using Skila.Language.Entities;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Language.Extensions
{
    public static class FunctionDefinitionExtension
    {
        public static bool IsOutConverter(this FunctionDefinition @this)
        {
            return @this.Name.Name == NameFactory.ConvertFunctionName
                && @this.Name.Arity == 0
                && @this.Parameters.Count == 0;
        }
        public static bool IsInitConstructor(this FunctionDefinition @this)
        {
            return  @this.Name.Name == NameFactory.InitConstructorName;
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
                && @this.Parameters.Single().TypeName.Evaluation==@this.OwnerType().InstanceOf;
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
