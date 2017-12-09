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
            lock (this.threadLock)
            {
                ObjectData type_object;
                if (this.types.TryGetValue(typeInstance, out type_object))
                    return type_object;

                this.types.Add(typeInstance, null);

                IEnumerable<VariableDeclaration> static_fields = typeInstance.TargetType.NestedFields.Where(it => it.Modifier.HasStatic);
                if (static_fields.Any())
                    type_object = ObjectData.CreateType(ctx, typeInstance);

                this.types[typeInstance] = type_object;

                if (type_object != null)
                {
                    Interpreter.SetupFunctionCallData(ref ctx, typeInstance.TemplateArguments, null, null);
                    ctx.Interpreter.Executed(typeInstance.TargetType.NestedFunctions.Single(it => it.IsZeroConstructor()), ctx);
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