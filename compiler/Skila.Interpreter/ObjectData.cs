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

        public static ObjectData Create(IEntityInstance typeInstance, object value)
        {
            TypeDefinition type_def = typeInstance.Cast<EntityInstance>().TargetType;
            if (type_def.IsPlain)
                return new ObjectData(true, value, typeInstance);
            else
                return new ObjectData(false, value, typeInstance);
        }

        public static ObjectData CreateEmpty(IEntityInstance typeInstance)
        {
            return Create(typeInstance, null);
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
            internal bool IsPlain { get; }
            public EntityInstance RunTimeTypeInstance { get; }

            public Data(bool plain, object value, IEntityInstance typeInstance)
            {
                this.PlainValue = value;
                this.IsPlain = plain;
                this.RunTimeTypeInstance = typeInstance.Cast<EntityInstance>();
                if (!this.IsPlain)
                {
                    this.fields = new Dictionary<VariableDeclaration, ObjectData>(ReferenceEqualityComparer<VariableDeclaration>.Instance);
                    foreach (VariableDeclaration field in this.RunTimeTypeInstance.TargetType.AllNestedFields)
                    {
                        EntityInstance field_type = field.Evaluation.Cast<EntityInstance>();
                        field_type = field_type.TranslateThrough(typeInstance);
                        this.fields.Add(field, ObjectData.CreateEmpty(field_type));
                    }
                }
            }

            public Data(Data src)
            {
                this.PlainValue = src.PlainValue;
                this.IsPlain = src.IsPlain;
                this.RunTimeTypeInstance = src.RunTimeTypeInstance;
                this.fields = src.fields?.ToDictionary(it => it.Key, it => it.Value);
            }

            internal ObjectData GetField(IEntity entity)
            {
                return this.fields[entity.Cast<VariableDeclaration>()];
            }
        }

        private ObjectData(bool plain, object value, IEntityInstance typeInstance)
        {
            if (this.DebugId.Id == 9167)
            {
                ;
            }
            this.data = new Data(plain, value, typeInstance);
        }

        private ObjectData(ObjectData src)
        {
            if (src.isDisposed)
                throw new ObjectDisposedException($"{src}");
            this.data = new Data(src.data);
        }


        public void Assign(ObjectData source)
        {
            if (this.isDisposed)
                throw new ObjectDisposedException($"{this}");
            if (source.isDisposed)
                throw new ObjectDisposedException($"{source}");

            if (this.DebugId.Id == 8803)
            {
                ;
            }
            this.data = source.data;
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
            if (this.data.IsPlain)
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

        internal ObjectData Reference(Language.Environment env)
        {
            return ObjectData.Create(env.ReferenceType.GetInstanceOf(new[] { this.RunTimeTypeInstance }), this);
        }

        internal ObjectData GetValue(IExpression expr)
        {
            if (expr != null && expr.IsDereferenced)
                return this.Dereference();
            else
                return this;
        }
    }
}
