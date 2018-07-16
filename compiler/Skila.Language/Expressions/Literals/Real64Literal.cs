using System.Diagnostics;

namespace Skila.Language.Expressions.Literals
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class Real64Literal : Literal
    {
        private readonly double? value;
        public double Value => this.value.Value;

        public override object LiteralValue => this.Value;

        public static Real64Literal Create(string inputValue)
        {
            return new Real64Literal(inputValue);
        }

        public static Real64Literal Create(double inputValue)
        {
            return new Real64Literal(inputValue);
        }

        // we use textual value to preserve user formatting like separator in "140_000"
        private Real64Literal(double inputValue)
            : base($"{inputValue}", NameFactory.Real64NameReference())
        {
            this.value = inputValue;
        }

        private Real64Literal(string inputValue) : this(parsed(inputValue))
        {
        }

        private static double parsed(string input)
        {
            if (double.TryParse(input, out double result))
                return result;
            else
                throw new System.Exception("Internal error");
        }
    }
}
