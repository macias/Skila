using Skila.Language.Entities;

namespace Skila.Language.Builders
{
   public struct PropertyMembers
    {
        public VariableDeclaration Field { get; set; }
        public FunctionDefinition Getter { get; set; }
        public FunctionDefinition Setter { get; set; }
    }
}
