using System;

namespace Skila.Language
{
    [Flags]
    public enum BrowseMode
    {
        None = 0,

        InstanceToStatic = 1 << 0,
        Decompose = 1 << 1, // gives access to private members, not for user (not directly)
    }
}