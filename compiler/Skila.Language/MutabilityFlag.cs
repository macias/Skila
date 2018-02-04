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

}
