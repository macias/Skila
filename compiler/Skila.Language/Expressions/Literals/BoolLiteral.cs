using System.Diagnostics;

namespace Skila.Language.Expressions.Literals
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class BoolLiteral : Literal
    {
        public static BoolLiteral CreateTrue()
        {
            return new BoolLiteral(true);
        }
        public static BoolLiteral CreateFalse()
        {
            return new BoolLiteral(false);
        }

        public override object LiteralValue => this.Value;
        public bool Value { get; }

        private BoolLiteral(bool value)
            : base(value ? "true" : "false", NameFactory.BoolNameReference(  ))
        {
            this.Value = value;
        }
    }
}
