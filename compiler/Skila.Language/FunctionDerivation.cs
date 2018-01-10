using Skila.Language.Entities;

namespace Skila.Language
{
    public struct FunctionDerivation
    {
        public FunctionDefinition Base { get; }
        public FunctionDefinition Derived { get; }

        public FunctionDerivation(FunctionDefinition @base,FunctionDefinition derived)
        {
            this.Base = @base;
            this.Derived = derived;
        }

        public override string ToString()
        {
            return $"{this.Base}  -->  {this.Derived}";
        }
    }
}
