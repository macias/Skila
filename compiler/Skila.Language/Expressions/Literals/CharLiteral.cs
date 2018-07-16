using System.Diagnostics;

namespace Skila.Language.Expressions.Literals
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class CharLiteral : Literal
    {
        public char Value { get; }
        public override object LiteralValue => this.Value;

        public static CharLiteral Create(char value)
        {
            return new CharLiteral(value);
        }

        private CharLiteral(char value)
            : base(value.ToString(), NameFactory.CharNameReference())
        {
            this.Value = value;
        }
    }
}
