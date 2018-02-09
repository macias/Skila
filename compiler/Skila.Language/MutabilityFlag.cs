using System;

namespace Skila.Language
{
    public enum MutabilityFlag
    {
        ConstAsSource,
        Neutral,
        ForceMutable,
        ForceConst,
        GenericUnknownMutability
    }


    public static class MutabilityFlagExtension
    {
        public static string StringPrefix(this MutabilityFlag flag)
        {
            switch (flag)
            {
                case MutabilityFlag.ForceConst: return $"const ";
                case MutabilityFlag.ForceMutable: return $"mut ";
                case MutabilityFlag.Neutral: return $"neut ";
                case MutabilityFlag.ConstAsSource: return "";
                case MutabilityFlag.GenericUnknownMutability: return "";
                default: throw new Exception();
            }

        }
    }
}