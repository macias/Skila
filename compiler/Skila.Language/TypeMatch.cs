using System;

namespace Skila.Language
{
    [Flags]
    public enum TypeMatch
    {
        No = 0,

        Same = 1 << 1,
        Substitute = 1 << 2,
        InConversion = 1 << 3,
        OutConversion = 1 << 4,
        ImplicitReference = 1 << 5,
        AutoDereference = 1 << 6,
    }
}
