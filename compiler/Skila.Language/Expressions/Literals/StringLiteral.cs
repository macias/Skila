using System.Diagnostics;

namespace Skila.Language.Expressions.Literals
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public static class StringLiteral 
    {
        public static Utf8StringLiteral Create(string value)
        {
            return Utf8StringLiteral.Create(value);
        }
    }
}
