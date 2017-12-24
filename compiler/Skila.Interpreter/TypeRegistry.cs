using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Data;
using Skila.Language.Extensions;
using Skila.Language;
using System;
using Skila.Language.Entities;
using System.Threading.Tasks;

namespace Skila.Interpreter
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    internal sealed class TypeRegistry
    {
        private readonly object threadLock = new object();

        private readonly Dictionary<EntityInstance, TaskCompletionSource<ObjectData>> types;

        public TypeRegistry()
        {
            this.types = new Dictionary<EntityInstance, TaskCompletionSource<ObjectData>>();
        }

        // we need this "simplified" register (without waiting for the result) to resolve legal, infinite loops
        // consider type Unit, in order to create it we need to create "unit" instance of type Unit, but this would mean
        // waiting for created type "Unit" but we are building it already, so we would wait forever
        // that is why during creation of static "unit" field, we register type "Unit" but we don't wait for the outcome
        internal async Task RegisterAdd(ExecutionContext ctx, EntityInstance typeInstance)
        {
            await register(ctx, typeInstance).ConfigureAwait(false);
        }

        internal async Task<ObjectData> RegisterGet(ExecutionContext ctx, EntityInstance typeInstance)
        {
            TaskCompletionSource<ObjectData> tcs = await register(ctx, typeInstance).ConfigureAwait(false);
            ObjectData obj_data = await tcs.Task.ConfigureAwait(false);
            return obj_data;
        }

        private async Task<TaskCompletionSource<ObjectData>> register(ExecutionContext ctx, EntityInstance typeInstance)
        {
            if (typeInstance.DebugId.Id == 3400)
            {
                ;
            }

            TaskCompletionSource<ObjectData> type_entry;
            bool existing;

            lock (this.threadLock)
            {
                existing = this.types.TryGetValue(typeInstance, out type_entry);

                if (!existing)
                {
                    // creating entry to avoid infinite loop when building static fields of the type we are just building
                    // in future it might be necessary to use Option instead of raw null to have 3 cases
                    // * empty placeholder (type in processing)
                    // * null (type processed, no static fields)
                    // * value with fields

                    type_entry = new TaskCompletionSource<ObjectData>();
                    this.types.Add(typeInstance, type_entry);
                }
            }

            if (!existing)
            {
                ObjectData type_object = null;

                IEnumerable<VariableDeclaration> static_fields = typeInstance.TargetType.NestedFields.Where(it => it.Modifier.HasStatic);
                if (static_fields.Any())
                    type_object = await ObjectData.CreateType(ctx, typeInstance).ConfigureAwait(false);

                type_entry.SetResult(type_object);

                if (type_object != null)
                {
                    Interpreter.SetupFunctionCallData(ref ctx, typeInstance.TemplateArguments, metaThis: null, functionArguments: null);
                    await ctx.Interpreter.ExecutedAsync(typeInstance.TargetType.NestedFunctions.Single(it => it.IsZeroConstructor()
                        && it.Modifier.HasStatic), ctx).ConfigureAwait(false);

                    if (typeInstance.DebugId.Id == 3400)
                    {
                        ;
                    }
                }
            }


            return type_entry;
        }

        public override string ToString()
        {
            return $"{this.types.Count} types registered";
        }
    }
}