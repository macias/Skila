using System;

namespace Skila.Language
{
    [Flags]
    public enum TypeMutability
    {
        None = 0,
        ReadOnly = 1 << 0,
        ForceMutable = 1 << 1,
        ForceConst = 1 << 2,
        GenericUnknownMutability = 1 << 3,
        DualConstMutable = 1 << 4, // for literals
        Reassignable = 1 << 5,

        // todo: remove this
        ConstAsSource = 1 << 6,
    }

    public static class TypeMutabilityExtension
    {
        public static string StringPrefix(this TypeMutability flag)
        {
            string s = "";
            if (flag.HasFlag( TypeMutability.Reassignable))
            {
                s = "=";
                flag ^= TypeMutability.Reassignable;
            }
            switch (flag)
            {
                case TypeMutability.DualConstMutable: return $"{s}dual ";
                case TypeMutability.ForceConst: return $"{s}fconst ";
                case TypeMutability.ForceMutable: return $"{s}mut ";
                case TypeMutability.ReadOnly: return $"{s}neut ";
                case TypeMutability.None: return $"{s}";
                case TypeMutability.GenericUnknownMutability: return "{s}g ";
                default: throw new Exception();
            }

        }
    }
}