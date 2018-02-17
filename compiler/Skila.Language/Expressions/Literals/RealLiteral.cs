using System.Diagnostics;

namespace Skila.Language.Expressions.Literals
{
    public static class RealLiteral 
    {
        public static Real64Literal Create(string inputValue)
        {
            return Real64Literal.Create(inputValue);
        }

        public static Real64Literal Create(double inputValue)
        {
            return Real64Literal.Create(inputValue);
        }
    }
}
