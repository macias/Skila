using System.Diagnostics;

namespace Skila.Language.Expressions
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

        public bool Value { get; }

        private BoolLiteral(bool value)
            : base(value ? "true" : "false", NameFactory.BoolTypeReference())
        {
            this.Value = value;
        }
    }
}
