using System;
using System.Diagnostics;

namespace Skila.Language.Expressions.Literals
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class NatLiteral 
    {
        public static Nat64Literal Create(string inputValue)
        {
            return Nat64Literal.Create(inputValue);
        }
    }
}
