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

        public static IEnumerable<FunctionDerivation> PairDerivations(ComputationContext ctx,
            EntityInstance baseInstance,
            IEnumerable<FunctionDefinition> derivedFunctions)
        {
            foreach (FunctionDefinition base_func in baseInstance.Target.CastType().NestedFunctions
                .Where(it => !it.IsInitConstructor() && !it.IsZeroConstructor()))
            {
                FunctionDefinition derived_func = derivedFunctions
                    .FirstOrDefault(f => FunctionDefinitionExtension.IsDerivedOf(ctx, f, base_func, baseInstance));

                yield return new FunctionDerivation(base_func, derived_func);
            }
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
