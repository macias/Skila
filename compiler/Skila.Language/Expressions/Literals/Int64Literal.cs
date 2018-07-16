using System;
using System.Diagnostics;

namespace Skila.Language.Expressions.Literals
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class Int64Literal : Literal
    {
        private readonly Int64? value;
        public Int64 Value => this.value.Value;

        public override object LiteralValue => this.Value;

        public static Int64Literal Create(string inputValue)
        {
            return new Int64Literal(inputValue);
        }

        // we use textual value to preserve user formatting like separator in "140_000"
        private Int64Literal(string inputValue)
            : base(inputValue, NameFactory.Int64NameReference( ))
        {
            if (Int64.TryParse(inputValue, out Int64 result))
                this.value = result;
            else
                throw new System.Exception("Internal error");
        }
    }
}
