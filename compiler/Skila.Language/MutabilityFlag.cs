using System;

namespace Skila.Language
{
    public enum MutabilityOverride
    {
        NotGiven,

        // todo: remove this
        Neutral,

        ForceMutable,
        ForceConst,
        GenericUnknownMutability,
        DualConstMutable, // for literals
    }

    public enum TypeMutability
    {
        // todo: remove this
        ConstAsSource,

        ReadOnly,
        Mutable,
        Const,
        GenericUnknownMutability,
        DualConstMutable, // for literals
    }

    public static class MutabilityOverrideExtension
    {
        public static string StringPrefix(this MutabilityOverride flag)
        {
            switch (flag)
            {
                case MutabilityOverride.DualConstMutable: return $"dual ";
                case MutabilityOverride.ForceConst: return $"fconst ";
                case MutabilityOverride.ForceMutable: return $"mut ";
                case MutabilityOverride.Neutral: return $"neut ";
                case MutabilityOverride.NotGiven: return "";
                case MutabilityOverride.GenericUnknownMutability: return "g ";
                default: throw new Exception();
            }

        }
    }
}