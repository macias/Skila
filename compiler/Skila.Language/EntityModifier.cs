using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;

namespace Skila.Language
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class EntityModifier
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
            Const, // for (deeply) immutable types 
            Base, // unseal types
            Interface,
            Protocol, // same as interface, but supports duck type matching 
            Derived, // modifier for methods ("override" in C#)
            Abstract,
        }

        public static readonly EntityModifier None = new EntityModifier();
        public static readonly EntityModifier Static = new EntityModifier(ModifierIndex.Static);
        public static readonly EntityModifier Implicit = new EntityModifier(ModifierIndex.Implicit);
        public static readonly EntityModifier HeapOnly = new EntityModifier(ModifierIndex.HeapOnly);
        public static readonly EntityModifier Public = new EntityModifier(ModifierIndex.Public);
        public static readonly EntityModifier Private = new EntityModifier(ModifierIndex.Private);
        public static readonly EntityModifier Reassignable = new EntityModifier(ModifierIndex.Reassignable);
        public static readonly EntityModifier Const = new EntityModifier(ModifierIndex.Const);
        public static readonly EntityModifier Base = new EntityModifier(ModifierIndex.Base);
        public static readonly EntityModifier Interface = new EntityModifier(ModifierIndex.Interface);
        public static readonly EntityModifier Protocol = new EntityModifier(ModifierIndex.Protocol);
        public static readonly EntityModifier Derived = new EntityModifier(ModifierIndex.Derived);
        public static readonly EntityModifier Abstract = new EntityModifier(ModifierIndex.Abstract);

        private readonly IReadOnlyList<int> flags; // value tells how many times given modifier was specified

        public bool HasAny => this.flags.Any(it => it > 0);
        public bool HasStatic => this.flags[(int)ModifierIndex.Static] > 0;
        public bool HasImplicit => this.flags[(int)ModifierIndex.Implicit] > 0;
        public bool HasHeapOnly => this.flags[(int)ModifierIndex.HeapOnly] > 0;
        public bool HasPublic => this.flags[(int)ModifierIndex.Public] > 0;
        public bool HasPrivate => this.flags[(int)ModifierIndex.Private] > 0;
        public bool HasReassignable => this.flags[(int)ModifierIndex.Reassignable] > 0;
        public bool HasConst => this.flags[(int)ModifierIndex.Const] > 0;
        public bool HasBase => this.flags[(int)ModifierIndex.Base] > 0;
        public bool HasInterface => this.flags[(int)ModifierIndex.Interface] > 0;
        public bool HasProtocol => this.flags[(int)ModifierIndex.Protocol] > 0;
        public bool HasDerived => this.flags[(int)ModifierIndex.Derived] > 0;
        public bool HasAbstract => this.flags[(int)ModifierIndex.Abstract] > 0;

        public bool HasSealed => !this.HasBase && !this.HasInterface;

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
            if (b == null)
                return a;
            else
                return new EntityModifier(a, b);
        }

    }

}
