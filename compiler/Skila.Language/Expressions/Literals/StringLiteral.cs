using System.Diagnostics;

namespace Skila.Language.Expressions.Literals
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class StringLiteral : Literal
    {
        public string Value => InputValue;
        public override object LiteralValue => this.Value;

        public static StringLiteral Create(string value)
        {
            return new StringLiteral(value);
        }

        private StringLiteral(string value)
            : base(value, NameFactory.StringPointerTypeReference( MutabilityFlag.DualConstMutable))
        {
        }
    }
}
