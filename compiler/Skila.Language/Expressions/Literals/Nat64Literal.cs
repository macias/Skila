using System;
using System.Diagnostics;

namespace Skila.Language.Expressions.Literals
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class Nat64Literal : Literal
    {
        private readonly UInt64? value;
        public UInt64 Value => this.value.Value;

        public override object LiteralValue => this.Value;

        public static Nat64Literal Create(string inputValue)
        {
            return new Nat64Literal(inputValue);
        }

        // we use textual value to preserve user formatting like separator in "140_000"
        private Nat64Literal(string inputValue)
            : base(inputValue, NameFactory.Nat64TypeReference())
        {
            if (UInt64.TryParse(inputValue, out UInt64 result))
                this.value = result;
            else
                throw new System.Exception("Internal error");
        }
    }
}
