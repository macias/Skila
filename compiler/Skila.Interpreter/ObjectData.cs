using Skila.Language.Entities;
using System.Collections.Generic;
using NaiveLanguageTools.Common;
using Skila.Language;
using Skila.Language.Extensions;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Skila.Interpreter
{
    // we use internal class to store all the data for easy assignment
    // consider some code "p.x = y"
    // because during the interpreted execution we return instances, assigning would mean
    // assigning to local (in host language) variable
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class ObjectData
    {
#if DEBUG
        public DebugId DebugId { get; } = new DebugId();
#endif

        internal static Task<ObjectData> CreateInstanceAsync(ExecutionContext ctx, IEntityInstance typeInstance, object value)
        {
            TypeDefinition type_def = typeInstance.Cast<EntityInstance>().TargetType;
            return constructorAsync(ctx, type_def.Modifier.HasNative, value, typeInstance, isStatic: false);
        }
        internal static Task<ObjectData> CreateTypeAsync(ExecutionContext ctx, IEntityInstance typeInstance)
        {
            if (typeInstance.DebugId.Id == 652)
            {
                ;
            }
            TypeDefinition type_def = typeInstance.Cast<EntityInstance>().TargetType;
            return constructorAsync(ctx, type_def.Modifier.HasNative, null, typeInstance, isStatic: true);
        }

        private static async Task<ObjectData> constructorAsync(ExecutionContext ctx, bool isNative, object value, IEntityInstance typeInstance, bool isStatic)
        {
            EntityInstance runtime_instance = typeInstance.Cast<EntityInstance>();

            ObjectData primary_parent = null;

            if (!isStatic)
                await ctx.TypeRegistry.RegisterAddAsync(ctx, runtime_instance).ConfigureAwait(false);
            else
            {
                EntityInstance primary = runtime_instance.TargetType.Inheritance.GetTypeImplementationParent();
                if (primary != null)
                    primary_parent = await ctx.TypeRegistry.RegisterGetAsync(ctx, primary).ConfigureAwait(false);
            }

            var fields = new Dictionary<VariableDeclaration, ObjectData>(ReferenceEqualityComparer<VariableDeclaration>.Instance);
            var translators = new List<EntityInstance>();

            IEnumerable<EntityInstance> source_types = new[] { runtime_instance };
            if (!isStatic)
                source_types = source_types.Concat(runtime_instance.PrimaryAncestors(ComputationContext.CreateBare(ctx.Env)));

            foreach (EntityInstance type_instance in source_types)
            {
                translators.Add(type_instance);

                foreach (VariableDeclaration field in type_instance.TargetType.AllNestedFields
                    .Where(it => it.Modifier.HasStatic == isStatic))
                {
                    EntityInstance field_type = field.Evaluation.Components.Cast<EntityInstance>();
                    field_type = field_type.TranslateThrough((translators as IEnumerable<EntityInstance>).Reverse());
                    fields.Add(field, await ObjectData.CreateEmptyAsync(ctx, field_type).ConfigureAwait(false));
                }
            }

            return new ObjectData(ctx, isNative, value, runtime_instance, isStatic, primary_parent, fields);
        }

        internal static Task<ObjectData> CreateEmptyAsync(ExecutionContext ctx, IEntityInstance typeInstance)
        {
            return CreateInstanceAsync(ctx, typeInstance, null);
        }

        private int isDisposedFlag;
        private bool isDisposed
        {
            get
            {
                return Interlocked.CompareExchange(ref this.isDisposedFlag, 0, 0) != 0;
            }
            set
            {
                Interlocked.Exchange(ref this.isDisposedFlag, value ? 1 : 0);
            }
        }
        private Data data;

        internal IEnumerable<ObjectData> Fields
        {
            get
            {
                if (this.isDisposed)
                    throw new ObjectDisposedException($"{this}");
                return this.data.Fields;
            }
        }
        public object PlainValue
        {
            get
            {
                if (this.isDisposed)
                    throw new ObjectDisposedException($"{this}");
                return this.data.PlainValue;
            }
        }
        internal VirtualTable InheritanceVirtualTable
        {
            get
            {
                if (this.isDisposed)
                    throw new ObjectDisposedException($"{this}");
                return this.RunTimeTypeInstance.TargetType.InheritanceVirtualTable;
            }
        }
        public EntityInstance RunTimeTypeInstance => this.data.RunTimeTypeInstance;

        private sealed class Data
        {
#if DEBUG
            public DebugId DebugId { get; } = new DebugId();
#endif
            internal IEnumerable<ObjectData> Fields => this.fields?.Values ?? Enumerable.Empty<ObjectData>();

            private readonly ObjectData primaryParentType; // set only for types, not instances
            private readonly Dictionary<VariableDeclaration, ObjectData> fields;

            public object PlainValue { get; }
            internal bool IsNative { get; }
            public EntityInstance RunTimeTypeInstance { get; }

            public Data(ExecutionContext ctx, bool isNative, object value, EntityInstance typeInstance, bool isStatic,
                ObjectData primary_parent, Dictionary<VariableDeclaration, ObjectData> fields)
            {
                this.PlainValue = value;
                this.IsNative = isNative;
                this.RunTimeTypeInstance = typeInstance;
                this.primaryParentType = primary_parent;
                this.fields = fields;
            }

            public Data(Data src)
            {
                // pointer/references sits here, so on copy simply assign the pointer/reference value
                if (src.PlainValue is IInstanceValue val)
                    this.PlainValue = val.Copy();
                else
                    this.PlainValue = src.PlainValue;

                this.IsNative = src.IsNative;
                this.RunTimeTypeInstance = src.RunTimeTypeInstance;
                // however make copies of the fields
                this.fields = src.fields?.ToDictionary(it => it.Key, it => new ObjectData(it.Value));
            }

            internal ObjectData GetField(IEntity entity)
            {

                VariableDeclaration field = entity.Cast<VariableDeclaration>();
                if (this.fields.TryGetValue(field, out ObjectData data))
                    return data;
                // types do not inherit static fields the way instances do, so we refer to parent's fields to get its field
                else if (this.primaryParentType != null)
                    return this.primaryParentType.GetField(entity);
                else
                    throw new Exception("Internal error -- no such field");
            }

            public override string ToString()
            {
                return $"plain value {this.PlainValue} C# {this.PlainValue.GetType()}";
            }
        }

        private ObjectData(ExecutionContext ctx, bool isNative, object value, EntityInstance typeInstance, bool isStatic,
            ObjectData primary_parent, Dictionary<VariableDeclaration, ObjectData> fields)
        {
            if (this.DebugId.Id == 9167)
            {
                ;
            }
            this.data = new Data(ctx, isNative, value, typeInstance, isStatic, primary_parent, fields);
        }

        private ObjectData(ObjectData src)
        {
            if (src.isDisposed)
                throw new ObjectDisposedException($"{src}");
            this.data = new Data(src.data);
        }


        internal ObjectData GetField(IEntity entity)
        {
            if (this.isDisposed)
                throw new ObjectDisposedException($"{this}");
            return this.data.GetField(entity);
        }

        public override string ToString()
        {
            return $"{this.RunTimeTypeInstance}";
        }

        public ObjectData Dereference()
        {
            if (this.isDisposed)
                throw new ObjectDisposedException($"{this}");
            ObjectData result = this.PlainValue.Cast<ObjectData>();
            if (result != null && result.isDisposed)
                throw new ObjectDisposedException($"{result}");
            return result;
        }

        internal bool Dispose()
        {
            if (this.isDisposed)
                throw new ObjectDisposedException($"{this}");

            bool value_disposed = false;
            if (this.data.IsNative)
            {
                if (this.PlainValue is IDisposable d)
                {
                    d.Dispose();
                    value_disposed = true;
                }
            }
            this.isDisposed = true;

            return value_disposed;
        }

        internal ObjectData Copy()
        {
            return new ObjectData(this);
        }

        internal Task<ObjectData> ReferenceAsync(ExecutionContext ctx)
        {
            return ObjectData.CreateInstanceAsync(ctx, ctx.Env.ReferenceType.GetInstance(new[] { this.RunTimeTypeInstance },
                overrideMutability: false, translation: null), this);
        }

        internal ObjectData TryDereference(Language.Environment env)
        {
            if (env.IsPointerLikeOfType(this.RunTimeTypeInstance))
                return this.Dereference();
            else
                return this;
        }
        internal ObjectData TryDereference(IExpression parentExpr, IExpression childExpr)
        {
            bool dereferencing = childExpr != null && childExpr.IsDereferenced;
            if (dereferencing != parentExpr.IsDereferencing)
                throw new Exception($"Internal error {ExceptionCode.SourceInfo()}");

            if (parentExpr.IsDereferencing)
                return this.Dereference();
            else
                return this;
        }

        public void Assign(ObjectData source)
        {
            if (this.isDisposed)
                throw new ObjectDisposedException($"{this}");
            if (source.isDisposed)
                throw new ObjectDisposedException($"{source}");

            if (source.DebugId.Id == 2942 || source.DebugId.Id == 2938)
            {
                ;
            }
            if (this.DebugId.Id == 5421)
            {
                ;
            }
            this.data = new Data(source.data);
        }

    }
}
