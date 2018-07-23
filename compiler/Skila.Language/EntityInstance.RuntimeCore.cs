﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NaiveLanguageTools.Common;

namespace Skila.Language
{
    public sealed partial class EntityInstance
    {
        // we need to split runtime data and semantic check data, runtime data (core) is shared among
        // various instances (varying on mutability or lifetime for example)

        internal sealed class RuntimeCore
        {
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
        }
    }
}