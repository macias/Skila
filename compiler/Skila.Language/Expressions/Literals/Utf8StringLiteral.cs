using System.Diagnostics;

namespace Skila.Language.Expressions.Literals
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class Utf8StringLiteral : Literal
    {
        public string Value => InputValue;
        public override object LiteralValue => this.Value;

        public static Utf8StringLiteral Create(string value)
        {
            return new Utf8StringLiteral(value);
        }

        private Utf8StringLiteral(string value)
            : base(value, NameFactory.StringPointerTypeReference(TypeMutability.DualConstMutable))
        {
        }
    }
}
