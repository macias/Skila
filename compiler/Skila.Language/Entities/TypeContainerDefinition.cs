using System.Collections.Generic;
using System.Linq;
using Skila.Language.Comparers;
using NaiveLanguageTools.Common;
using System;
using System.Diagnostics;
using Skila.Language.Extensions;
using Skila.Language.Semantics;

namespace Skila.Language.Entities
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public abstract class TypeContainerDefinition : TemplateDefinition
    {
        protected TypeContainerDefinition(EntityModifier modifier, NameDefinition name,
            IEnumerable<TemplateConstraint> constraints) : base(modifier, name, constraints)
        {
        }

        public override void Validate(ComputationContext ctx)
        {
            base.Validate(ctx);

            {   // detecting function duplicates
                var functions = NestedFunctions.StoreReadOnlyList();
                for (int a = 0; a < functions.Count; ++a)
                {
                    for (int b = a + 1; b < functions.Count; ++b)
                    {
                        FunctionDefinition func_a = functions[a];
                        FunctionDefinition func_b = functions[b];

                        if (FunctionDefinitionExtension.IsOverloadedDuplicate(func_a, func_b))
                            ctx.ErrorManager.AddError(ErrorCode.OverloadingDuplicateFunctionDefinition, func_b, func_a);
                    }
                }
            }

            // detecting entity duplicates
            foreach (IEntity entity in this.NestedFields.Select(it => it.Cast<IEntity>())
                .Concat(this.NestedTypes.Where(it => !it.IsTrait))
                .Concat(this.NestedProperties.Where(it => !it.IsIndexer))
                .GroupBy(it => it.Name, EntityNameArityComparer.Instance)
                .Select(group => group.Skip(1).FirstOrDefault())
                .Where(it => it != null))
            {
                ctx.ErrorManager.AddError(ErrorCode.NameAlreadyExists, entity);
            }

            IEntityScopeExtension.Validate(this, ctx);
        }

    }
}
