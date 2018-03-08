using System.Diagnostics;

namespace Skila.Language.Expressions.Literals
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class UnitLiteral : Literal
    {
        public static readonly char UnitValue = 'µ';

        public static UnitLiteral Create()
        {
            return new UnitLiteral();
        }

        public override object LiteralValue => this.Value;
        public char Value { get; }

        private UnitLiteral() : base(" ", NameFactory.UnitTypeReference(), ExpressionReadMode.OptionalUse)
        {
            this.Value = UnitValue;
        }
    }
}
