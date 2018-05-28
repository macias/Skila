using System;

namespace Skila.Language
{
    [Flags]
    public enum MutabilityOverride
    {
        None = 0,
        ForceMutable = 1 << 1,
        ForceConst = 1 << 2,
        GenericUnknownMutability = 1 << 3,
        DualConstMutable = 1 << 4, // for literals
        Reassignable = 1 << 5,

        // todo: remove this
        Neutral = 1 <<0,

    }

    [Flags]
    public enum TypeMutability
    {
        None = 0,  // used when building result, cannot happen as result
        ForceMutable = 1 << 2,
        ForceConst = 1 << 3,
        GenericUnknownMutability = 1 << 4,
        DualConstMutable = 1 << 5, // for literals
        Reassignable = 1 << 6,

        // todo: remove this
        ConstAsSource = 1 << 0,

        ReadOnly = 1 << 1,
    }

    public static class MutabilityOverrideExtension
    {
        public static string StringPrefix(this MutabilityOverride flag)
        {
            string s = "";
            if (flag.HasFlag( MutabilityOverride.Reassignable))
            {
                s = "=";
                flag ^= MutabilityOverride.Reassignable;
            }
            switch (flag)
            {
                case MutabilityOverride.DualConstMutable: return $"{s}dual ";
                case MutabilityOverride.ForceConst: return $"{s}fconst ";
                case MutabilityOverride.ForceMutable: return $"{s}mut ";
                case MutabilityOverride.Neutral: return $"{s}neut ";
                case MutabilityOverride.None: return $"{s}";
                case MutabilityOverride.GenericUnknownMutability: return "{s}g ";
                default: throw new Exception();
            }

        }
    }
}