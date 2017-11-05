using System.Collections.Generic;
using System.Diagnostics;

namespace Skila.Language.Expressions
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class IntLiteral : Literal
    {
        private readonly int? value;
        public int Value => this.value.Value;

        public static IntLiteral Create(string inputValue)
        {
            return new IntLiteral(inputValue);
        }

        // we use textual value to preserve user formatting like separator in "140_000"
        private IntLiteral(string inputValue)
            : base(inputValue, NameFactory.IntTypeReference())
        {
            if (int.TryParse(inputValue, out int result))
                this.value = result;
            else
                throw new System.Exception("Internal error");
        }
    }
}
