using System.Diagnostics;

namespace Skila.Language.Expressions.Literals
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class Nat8Literal : Literal
    {
        private readonly byte? value;
        public byte Value => this.value.Value;

        public override object LiteralValue => this.Value;

        public static Nat8Literal Create(string inputValue)
        {
            return new Nat8Literal(inputValue);
        }

        // we use textual value to preserve user formatting like separator in "140_000"
        private Nat8Literal(string inputValue)
            : base(inputValue, NameFactory.Nat8TypeReference( TypeMutability.DualConstMutable))
        {
            if (byte.TryParse(inputValue, out byte result))
                this.value = result;
            else
                throw new System.Exception("Internal error");
        }
    }
}
