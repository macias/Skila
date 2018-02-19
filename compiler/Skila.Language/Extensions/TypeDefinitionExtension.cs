using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Language.Extensions
{
    public static class TypeDefinitionExtension
    {
        public static bool HasDefaultPublicConstructor(this TypeDefinition @this)
        {
            return @this.NestedFunctions.Where(it => it.IsDefaultInitConstructor())
                 .Any(it => it.Modifier.HasPublic);
        }
        public static bool HasDefaultConstructor(this TypeDefinition @this)
        {
            return DefaultConstructor(@this) != null;
        }

        public static FunctionDefinition DefaultConstructor(this TypeDefinition @this)
        {
            return @this.NestedFunctions.Where(it => it.IsDefaultInitConstructor()).FirstOrDefault();
        }

        public static IEnumerable<FunctionDefinition> InvokeFunctions(this TypeDefinition @this)
        {
            return @this.NestedFunctions.Where(it => it.Name.Name == NameFactory.LambdaInvoke);
        }

        public static IEnumerable<FunctionDerivation> PairDerivations(ComputationContext ctx,
            EntityInstance baseInstance,
            IEnumerable<FunctionDefinition> derivedFunctions)
        {
            var result = new List<FunctionDerivation>();
            foreach (FunctionDefinition base_func in baseInstance.TargetType.AllNestedFunctions
                .Where(it => !it.IsInitConstructor() && !it.IsZeroConstructor()))
            {
                FunctionDefinition derived_func = derivedFunctions
                    .FirstOrDefault(dfunc => FunctionDefinitionExtension.IsDerivedOf(ctx, dfunc, base_func, baseInstance));

                result.Add(new FunctionDerivation(base_func, derived_func));
            }

            return result;
        }

    }/*
        public static FunctionDefinition GetNewConstructor(this TypeDefinition @this, FunctionDefinition initConstructor)
        {
            return getConstructor(@this, initConstructor, staticConstructor: true);
        }

        private static FunctionDefinition getConstructor(this TypeDefinition @this, FunctionDefinition cons,
            bool staticConstructor)
        {
            return @this.GetOwnedFunctions(staticConstructor,
                 (staticConstructor ? NameFactory.NewConstructorName() : NameFactory.InitConstructorName()).Name,
                 cons.Name,
                cons,
                null,
                FunctionMatchMode.AllRegular ^ FunctionMatchMode.CheckOutcome).FirstOrDefault();
        }

        public static IEnumerable<FunctionDefinition> GetOwnedFunctions(this TypeContainerDefinition @this,
            bool isStaticFunction,
            string funcName,
            ITemplateParameters funcSpec,
            IFunctionParameters funcParams,
            IFunctionOutcome funcOutcome,
            FunctionMatchMode matchMode)
        {
            return @this.NestedFunctions.FilterFunctionsLike(@this as TypeDefinition, 
                @this as TypeDefinition, 
                isStaticFunction, 
                funcName, funcSpec, funcParams, funcOutcome, matchMode);
        }
    }*/
}
