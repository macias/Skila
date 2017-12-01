using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Semantics;

namespace Skila.Language
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class EntityModifier : Node, IValidable
    {
        private enum ModifierIndex
        {
            Static,
            Implicit,
            HeapOnly,
            Public,
            Private,
            Reassignable, // "var x ..."
            //InOut // for parameters only, reassignment is visible outside function
            Mutable, // to allow (deeply) mutable types, used when defining types
            Const, // reverse of "Mutable", used in constraints
            Base, // unseal types
            Interface,
            Protocol, // same as interface, but supports duck type matching 
            Refines, // modifier for methods ("override" in C#)
            Abstract,
            Protected,
            UnchainBase, // despite we derive base function we won't call it
        }

        public static readonly EntityModifier None = new EntityModifier();
        public static readonly EntityModifier Static = new EntityModifier(ModifierIndex.Static);
        public static readonly EntityModifier Implicit = new EntityModifier(ModifierIndex.Implicit);
        public static readonly EntityModifier HeapOnly = new EntityModifier(ModifierIndex.HeapOnly);
        public static readonly EntityModifier Public = new EntityModifier(ModifierIndex.Public);
        public static readonly EntityModifier Private = new EntityModifier(ModifierIndex.Private);
        public static readonly EntityModifier Reassignable = new EntityModifier(ModifierIndex.Reassignable);
        public static readonly EntityModifier Mutable = new EntityModifier(ModifierIndex.Mutable);
        public static readonly EntityModifier Base = new EntityModifier(ModifierIndex.Base);
        public static readonly EntityModifier Interface = new EntityModifier(ModifierIndex.Interface);
        public static readonly EntityModifier Protocol = new EntityModifier(ModifierIndex.Protocol);
        public static readonly EntityModifier Refines = new EntityModifier(ModifierIndex.Refines);
        public static readonly EntityModifier Abstract = new EntityModifier(ModifierIndex.Abstract);
        public static readonly EntityModifier Const = new EntityModifier(ModifierIndex.Const);
        public static readonly EntityModifier Protected = new EntityModifier(ModifierIndex.Protected);
        public static readonly EntityModifier UnchainBase = new EntityModifier(ModifierIndex.UnchainBase);

        private readonly IReadOnlyList<int> flags; // value tells how many times given modifier was specified

        public bool HasAny => this.flags.Any(it => it > 0);
        public bool HasStatic => this.flags[(int)ModifierIndex.Static] > 0;
        public bool HasImplicit => this.flags[(int)ModifierIndex.Implicit] > 0;
        public bool HasHeapOnly => this.flags[(int)ModifierIndex.HeapOnly] > 0;
        public bool HasPublic => this.flags[(int)ModifierIndex.Public] > 0;
        public bool HasPrivate => this.flags[(int)ModifierIndex.Private] > 0;
        public bool HasReassignable => this.flags[(int)ModifierIndex.Reassignable] > 0;
        public bool HasMutable => this.flags[(int)ModifierIndex.Mutable] > 0;
        public bool HasConst => this.flags[(int)ModifierIndex.Const] > 0;
        public bool HasBase => this.flags[(int)ModifierIndex.Base] > 0;
        public bool HasInterface => this.flags[(int)ModifierIndex.Interface] > 0;
        public bool HasProtocol => this.flags[(int)ModifierIndex.Protocol] > 0;
        public bool HasRefines => this.flags[(int)ModifierIndex.Refines] > 0;
        public bool HasAbstract => this.flags[(int)ModifierIndex.Abstract] > 0;
        public bool HasProtected => this.flags[(int)ModifierIndex.Protected] > 0;
        public bool HasUnchainBase => this.flags[(int)ModifierIndex.UnchainBase] > 0;

        public bool HasSealed => !this.HasInterface // makes sense only for types
                                 && !this.HasVirtual;

        public bool HasVirtual => this.HasRefines || this.HasBase || this.HasAbstract;
        public bool HasImmutable => !this.HasMutable;
        public bool HasAccessSet => this.HasPublic || this.HasPrivate || this.HasProtected;

        public override IEnumerable<INode> OwnedNodes { get { yield break; } }

        private EntityModifier(ModifierIndex index)
        {
            var flags = new int[EnumExtensions.GetValues<ModifierIndex>().Count()];
            this.flags = flags;

            ++flags[(int)index];

        }
        private EntityModifier(params EntityModifier[] modifiers)
        {
            var flags = new int[EnumExtensions.GetValues<ModifierIndex>().Count()];
            this.flags = flags;

            for (int i = 0; i < flags.Length; ++i)
                flags[i] = modifiers.Sum(it => it.flags[i]);
        }

        public override string ToString()
        {
            return EnumExtensions.GetValues<ModifierIndex>().Select(idx =>
            {
                int count = flags[(int)idx];
                if (count == 0)
                    return null;
                string s = idx.ToString().ToLowerInvariant();
                if (count > 1)
                    s += $"({count})";
                return s;
            })
            .Where(it => it != null)
            .Join(" ");
        }

        public static EntityModifier operator |(EntityModifier a, EntityModifier b)
        {
            if (b == null || b == EntityModifier.None)
                return a;
            else
                return new EntityModifier(a, b);
        }

        internal bool SameAccess(EntityModifier other)
        {
            return this.HasPublic == other.HasPublic
                && this.HasProtected == other.HasProtected
                && this.HasPrivate == other.HasPrivate;
        }

        public void Validate(ComputationContext ctx)
        {
            if (this.HasConst && this.HasMutable)
                ctx.AddError(ErrorCode.ConflictingModifier, this);
        }
    }

}
