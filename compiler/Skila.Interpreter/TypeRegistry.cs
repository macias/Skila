using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Data;
using Skila.Language.Extensions;
using Skila.Language;
using System;
using Skila.Language.Entities;

namespace Skila.Interpreter
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    internal sealed class TypeRegistry
    {
        private readonly object threadLock = new object();

        private readonly Dictionary<EntityInstance, ObjectData> types;

        public TypeRegistry()
        {
            this.types = new Dictionary<EntityInstance, ObjectData>();
        }

        internal ObjectData Add(ExecutionContext ctx, EntityInstance typeInstance)
        {
            if (typeInstance.DebugId.Id == 3400)
            {
                ;
            }

            lock (this.threadLock)
            {
                ObjectData type_object;
                if (this.types.TryGetValue(typeInstance, out type_object))
                    return type_object;

                // creating entry to avoid infinite loop when building static fields of the type we are just building
                // in future it might be necessary to use Option instead of raw null to have 3 cases
                // * empty placeholder (type in processing)
                // * null (type processed, no static fields)
                // * value with fields
                this.types.Add(typeInstance, null);

                IEnumerable<VariableDeclaration> static_fields = typeInstance.TargetType.NestedFields.Where(it => it.Modifier.HasStatic);
                if (static_fields.Any())
                    type_object = ObjectData.CreateType(ctx, typeInstance);

                this.types[typeInstance] = type_object;

                if (type_object != null)
                {
                    Interpreter.SetupFunctionCallData(ref ctx, typeInstance.TemplateArguments, null, null);
                    ctx.Interpreter.Executed(typeInstance.TargetType.NestedFunctions.Single(it => it.IsZeroConstructor()
                        && it.Modifier.HasStatic), ctx);

                    if (typeInstance.DebugId.Id == 3400)
                    {
                        ;
                    }
                }

                return type_object;
            }
        }

        public override string ToString()
        {
            return $"{this.types.Count} types registered";
        }
    }
}