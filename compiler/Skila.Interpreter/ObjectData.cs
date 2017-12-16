using Skila.Language.Entities;
using System.Collections.Generic;
using NaiveLanguageTools.Common;
using Skila.Language;
using Skila.Language.Extensions;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

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

        internal static ObjectData CreateInstance(ExecutionContext ctx, IEntityInstance typeInstance, object value)
        {
            TypeDefinition type_def = typeInstance.Cast<EntityInstance>().TargetType;
            return new ObjectData(ctx, type_def.Modifier.HasNative, value, typeInstance, isStatic: false);
        }
        internal static ObjectData CreateType(ExecutionContext ctx, IEntityInstance typeInstance)
        {
            if (typeInstance.DebugId.Id == 652)
            {
                ;
            }
            TypeDefinition type_def = typeInstance.Cast<EntityInstance>().TargetType;
            return new ObjectData(ctx, type_def.Modifier.HasNative, null, typeInstance, isStatic: true);
        }

        internal static ObjectData CreateEmpty(ExecutionContext ctx, IEntityInstance typeInstance)
        {
            return CreateInstance(ctx, typeInstance, null);
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

            private readonly Dictionary<VariableDeclaration, ObjectData> fields;

            public object PlainValue { get; }
            internal bool IsNative { get; }
            public EntityInstance RunTimeTypeInstance { get; }

            public Data(ExecutionContext ctx, bool isNative, object value, IEntityInstance typeInstance, bool isStatic)
            {
                this.PlainValue = value;
                this.IsNative = isNative;
                this.RunTimeTypeInstance = typeInstance.Cast<EntityInstance>();

                if (!isStatic)
                    ctx.TypeRegistry.Add(ctx, this.RunTimeTypeInstance);

                this.fields = new Dictionary<VariableDeclaration, ObjectData>(ReferenceEqualityComparer<VariableDeclaration>.Instance);
                var translators = new List<EntityInstance>();
                foreach (EntityInstance type_instance in this.RunTimeTypeInstance
                    .PrimaryAncestors(ComputationContext.CreateBare(ctx.Env)).Concat(this.RunTimeTypeInstance))
                {
                    translators.Add(type_instance);

                    foreach (VariableDeclaration field in type_instance.TargetType.AllNestedFields
                        .Where(it => it.Modifier.HasStatic == isStatic))
                    {
                        EntityInstance field_type = field.Evaluation.Components.Cast<EntityInstance>();
                        field_type = field_type.TranslateThrough((translators as IEnumerable<EntityInstance>).Reverse());
                        this.fields.Add(field, ObjectData.CreateEmpty(ctx, field_type));
                    }
                }
            }

            public Data(Data src)
            {
                // pointer/references sits here, so on copy simply assign the pointer/reference value
                if (src.PlainValue is ICopyableValue val)
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
                return this.fields[entity.Cast<VariableDeclaration>()];
            }
        }

        private ObjectData(ExecutionContext ctx, bool isNative, object value, IEntityInstance typeInstance, bool isStatic)
        {
            if (this.DebugId.Id == 9167)
            {
                ;
            }
            this.data = new Data(ctx, isNative, value, typeInstance, isStatic);
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

        internal ObjectData Reference(ExecutionContext ctx)
        {
            return ObjectData.CreateInstance(ctx, ctx.Env.ReferenceType.GetInstanceOf(new[] { this.RunTimeTypeInstance }, overrideMutability: false), this);
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
                throw new Exception("Internal error");

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
            if (this.DebugId.Id == 2802)
            {
                ;
            }
            this.data = new Data(source.data);
        }

    }
}
