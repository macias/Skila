using System;

namespace Skila.Language
{
    public enum MutabilityFlag
    {
        ConstAsSource,
        Neutral,
        ForceMutable,
        ForceConst,
        GenericUnknownMutability,
        DualConstMutable, // for literals
    }


    public static class MutabilityFlagExtension
    {
        public static string StringPrefix(this MutabilityFlag flag)
        {
            switch (flag)
            {
                case MutabilityFlag.DualConstMutable: return $"dual ";
                case MutabilityFlag.ForceConst: return $"fconst ";
                case MutabilityFlag.ForceMutable: return $"mut ";
                case MutabilityFlag.Neutral: return $"neut ";
                case MutabilityFlag.ConstAsSource: return "";
                case MutabilityFlag.GenericUnknownMutability: return "g ";
                default: throw new Exception();
            }

        }
    }
}