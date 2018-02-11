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
        public DebugId DebugId { get; }
#endif

        internal static Task<ObjectData> CreateEmptyAsync(ExecutionContext ctx, IEntityInstance typeInstance)
        {
            return CreateInstanceAsync(ctx, typeInstance, null);
        }

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

        private static async Task<ObjectData> constructorAsync(ExecutionContext ctx, bool isNative, object value,
            IEntityInstance typeInstance, bool isStatic)
        {
            Data data = await buildInternalData(ctx, isNative, value, typeInstance, isStatic).ConfigureAwait(false);
            return new ObjectData(data);
        }

        private static async Task<Data> buildInternalData(ExecutionContext ctx, bool isNative, object value,
            IEntityInstance typeInstance, bool isStatic)
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

            Data data1 = new Data(isNative, value, runtime_instance, primary_parent, fields);
            return data1;
        }

        private int isFreedFlag;
        private bool isFreed
        {
            get
            {
                return Interlocked.CompareExchange(ref this.isFreedFlag, 0, 0) != 0;
            }
            set
            {
                Interlocked.Exchange(ref this.isFreedFlag, value ? 1 : 0);
            }
        }

        private Data data;

        internal IEnumerable<KeyValuePair<VariableDeclaration, ObjectData>> Fields
        {
            get
            {
                if (this.isFreed)
                    throw new ObjectDisposedException($"{this}");
                return this.data.Fields;
            }
        }
        public object PlainValue
        {
            get
            {
                if (this.isFreed)
                    throw new ObjectDisposedException($"{this}");
                return this.data.PlainValue;
            }
        }
        public string NativeString => this.PlainValue.Cast<string>();
        public Int16 NativeInt16 => this.PlainValue.Cast<Int16>();
        public Int64 NativeInt64 => this.PlainValue.Cast<Int64>();
        public UInt64 NativeNat64 => this.PlainValue.Cast<UInt64>();
        public byte NativeNat8 => this.PlainValue.Cast<byte>();

        internal VirtualTable InheritanceVirtualTable
        {
            get
            {
                if (this.isFreed)
                    throw new ObjectDisposedException($"{this}");
                return this.RunTimeTypeInstance.TargetType.InheritanceVirtualTable;
            }
        }
        public EntityInstance RunTimeTypeInstance => this.data.RunTimeTypeInstance;

        private sealed class Data
        {
#if DEBUG
            public DebugId DebugId { get; } = new DebugId(typeof(Data));
#endif
            internal IEnumerable<KeyValuePair<VariableDeclaration, ObjectData>> Fields => this.fields ?? Enumerable.Empty<KeyValuePair<VariableDeclaration, ObjectData>>();

            private readonly ObjectData primaryParentType; // set only for types, not instances
            private readonly Dictionary<VariableDeclaration, ObjectData> fields;

            public object PlainValue { get; }
            internal bool IsNative { get; }
            public EntityInstance RunTimeTypeInstance { get; }

            public Data(bool isNative, object value, EntityInstance typeInstance,
                ObjectData primary_parent, Dictionary<VariableDeclaration, ObjectData> fields)
            {
                this.PlainValue = value;
                this.IsNative = isNative;
                this.RunTimeTypeInstance = typeInstance;
                this.primaryParentType = primary_parent;
                this.fields = fields;
            }

            public Data(Data src) : this(src.IsNative,
                // pointer/references sits here, so on copy simply assign the pointer/reference value
                (src.PlainValue as IInstanceValue)?.Copy() ?? src.PlainValue,
                src.RunTimeTypeInstance, src.primaryParentType,
                // however make copies of the fields
                src.fields?.ToDictionary(it => it.Key, it => new ObjectData(it.Value)))
            {
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

        private ObjectData()
        {
            this.DebugId = new DebugId(typeof(ObjectData));
            if (this.DebugId.Id==77)
            {
                ;
            }
        }
        private ObjectData(Data data) : this()
        {
            if (this.DebugId.Id == 9167)
            {
                ;
            }
            this.data = data;
        }

        private ObjectData(ObjectData src) : this()
        {
            if (src.isFreed)
                throw new ObjectDisposedException($"{src}");
            this.data = new Data(src.data);
        }


        internal ObjectData GetField(IEntity entity)
        {
            if (this.isFreed)
                throw new ObjectDisposedException($"{this}");
            return this.data.GetField(entity);
        }

        public override string ToString()
        {
            return $"{this.RunTimeTypeInstance}";
        }

        public ObjectData DereferencedOnce()
        {
            if (this.isFreed)
                throw new ObjectDisposedException($"{this}");
            ObjectData result = this.PlainValue.Cast<ObjectData>();
            if (result != null && result.isFreed)
                throw new ObjectDisposedException($"{result}");
            return result;
        }
        public ObjectData Dereferenced(int count)
        {
            ObjectData self = this;
            for (int i = 0; i < count; ++i)
                self = self.DereferencedOnce();
            return self;
        }

        internal bool Free(ExecutionContext ctx,ObjectData passingOut, bool destroy, string callInfo)
        {
            if (this.DebugId.Id == 182254)
            {
                ;
            }

            if (this.isFreed)
                throw new ObjectDisposedException($"{this}");

            foreach (KeyValuePair<VariableDeclaration, ObjectData> field in this.Fields)
            {
                // locks are re-entrant, so recursive call is OK here
                ctx.Heap.TryRelease(ctx, field.Value, passingOut, callInfo: $"field of {callInfo}");
            }

            bool host_disposed = false;
            if (this.data.IsNative)
            {
                if (this.PlainValue is IDisposable d)
                {
                    d.Dispose();
                    host_disposed = true;
                }

            }

            if (this.PlainValue is Chunk chunk)
            {
                for (UInt64 i = 0; i != chunk.Count; ++i)
                    ctx.Heap.TryRelease(ctx, chunk[i], passingOut, callInfo: $"chunk elem of {callInfo}");
            }

            this.setData(null);

            if (destroy)
                this.isFreed = true;

            return host_disposed;
        }

        private void setData(Data data)
        {
            if (this.DebugId.Id==77)
            {
                ;
            }
            this.data = data;
        }
        internal ObjectData Copy()
        {
            return new ObjectData(this);
        }

        internal Task<ObjectData> ReferenceAsync(ExecutionContext ctx)
        {
            return ObjectData.CreateInstanceAsync(ctx, ctx.Env.ReferenceType.GetInstance(new[] { this.RunTimeTypeInstance },
                overrideMutability: MutabilityFlag.ConstAsSource, translation: null), this);
        }

        internal bool TryDereferenceAnyOnce(Language.Environment env, out ObjectData dereferenced)
        {
            if (env.IsPointerLikeOfType(this.RunTimeTypeInstance))
            {
                dereferenced = this.DereferencedOnce();
                return true;
            }
            else
            {
                dereferenced = null;
                return false;
            }
        }
        internal ObjectData TryDereferenceAnyOnce(Language.Environment env)
        {
            if (TryDereferenceAnyOnce(env, out ObjectData dereferenced))
                return dereferenced;
            else
                return this;
        }
        internal ObjectData TryDereferenceAnyMany(Language.Environment env,int count)
        {
            ObjectData self = this;
            for (int i = 0; i < count; ++i)
            {
                if (!env.IsPointerLikeOfType(self.RunTimeTypeInstance))
                    break;
                else
                    self = self.DereferencedOnce();
            }
            return self;
        }
        internal bool TryDereferenceMany(Language.Environment env, IExpression parentExpr, IExpression childExpr,
            out ObjectData dereferenced)
        {
            int deref_count = dereferencedCount(parentExpr, childExpr);
            if (deref_count>0)
            {
                dereferenced = this.TryDereferenceAnyMany(env,deref_count);
                return true;
            }
            else
            {
                dereferenced = null;
                return false;
            }
        }
        internal ObjectData TryDereferenceOnce(IExpression parentExpr, IExpression childExpr)
        {
            if (dereferencedCount(parentExpr, childExpr)>0)
            {
                return this.DereferencedOnce();
            }
            else
            {
                return this;
            }
        }

        private int dereferencedCount(IExpression parentExpr, IExpression childExpr)
        {
            if (childExpr != null && childExpr.DereferencedCount_LEGACY != parentExpr.DereferencingCount)
                throw new Exception($"Internal error {ExceptionCode.SourceInfo()}");

            return parentExpr.DereferencingCount;
        }

        public void Assign(ObjectData source)
        {
            if (this.isFreed || source.isFreed)
                throw new ObjectDisposedException($"{source}");

            if (source.DebugId.Id == 2942 || source.DebugId.Id == 2938)
            {
                ;
            }
            if (this.DebugId.Id == 5421)
            {
                ;
            }

            this.setData(new Data(source.data));
        }

    }
}
