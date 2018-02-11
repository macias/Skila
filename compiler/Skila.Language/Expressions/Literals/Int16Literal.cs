using System;
using System.Diagnostics;

namespace Skila.Language.Expressions.Literals
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class Int16Literal : Literal
    {
        private readonly Int16? value;
        public Int16 Value => this.value.Value;

        public override object LiteralValue => this.Value;

        public static Int16Literal Create(string inputValue)
        {
            return new Int16Literal(inputValue);
        }

        // we use textual value to preserve user formatting like separator in "140_000"
        private Int16Literal(string inputValue)
            : base(inputValue, NameFactory.Int16TypeReference())
        {
            if (Int16.TryParse(inputValue, out Int16 result))
                this.value = result;
            else
                throw new System.Exception("Internal error");
        }
    }
}
