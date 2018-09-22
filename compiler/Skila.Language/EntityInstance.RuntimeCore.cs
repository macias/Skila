using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using NaiveLanguageTools.Common;

namespace Skila.Language
{
    public sealed partial class EntityInstance
    {
        // we need to split runtime data and semantic check data, runtime data (core) is shared among
        // various instances (varying on mutability or lifetime for example)

        [DebuggerDisplay("{GetType().Name} {ToString()}")]
        internal sealed class RuntimeCore
        {
#if DEBUG
            public DebugId DebugId { get; } = new DebugId(typeof(RuntimeCore));
#endif
            private readonly Dictionary<RuntimeCore, VirtualTable> duckVirtualTables;

            public RuntimeCore()
            {
                this.duckVirtualTables = new Dictionary<RuntimeCore, VirtualTable>(ReferenceEqualityComparer<RuntimeCore>.Instance);
            }

            internal void AddDuckVirtualTable(ComputationContext ctx, EntityInstance target, VirtualTable vtable)
            {
                if (vtable == null)
                    throw new Exception("Internal error");

                this.duckVirtualTables.Add(target.core, vtable);
            }
            public bool TryGetDuckVirtualTable(EntityInstance target, out VirtualTable vtable)
            {
                return this.duckVirtualTables.TryGetValue(target.core, out vtable);
            }

            public override int GetHashCode()
            {
                return RuntimeHelpers.GetHashCode(this);
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as RuntimeCore);
            }

            public bool Equals(RuntimeCore obj)
            {
                return Object.ReferenceEquals(this, obj);
            }

            public override string ToString()
            {
                return $"{this.DebugId}";
            }
        }
    }
}