using System.Collections.Generic;
using System.Diagnostics;

namespace Skila.Language.Expressions
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class StringLiteral : Literal
    {
        public string Value => InputValue;

        public static StringLiteral Create(string value)
        {
            return new StringLiteral(value);
        }

        private StringLiteral(string value)
            : base(value, NameFactory.StringTypeReference())
        {
        }
    }
}
