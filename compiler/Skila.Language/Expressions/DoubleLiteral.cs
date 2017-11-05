using System.Collections.Generic;
using System.Diagnostics;

namespace Skila.Language.Expressions
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class DoubleLiteral : Literal
    {
        private readonly double? value;
        public double Value => this.value.Value;

        public static DoubleLiteral Create(string inputValue)
        {
            return new DoubleLiteral(inputValue);
        }

        // we use textual value to preserve user formatting like separator in "140_000"
        private DoubleLiteral(string inputValue)
            : base(inputValue, NameFactory.DoubleTypeReference())
        {
            if (double.TryParse(inputValue, out double result))
                this.value = result;
            else
                throw new System.Exception("Internal error");
        }
    }
}
