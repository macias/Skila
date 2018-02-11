using System;
using System.Diagnostics;

namespace Skila.Language.Expressions.Literals
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class IntLiteral 
    {
        public static Int64Literal Create(string inputValue)
        {
            return Int64Literal.Create(inputValue);
        }
    }
}
