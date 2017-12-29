using Skila.Language.Entities;

namespace Skila.Language.Builders
{
   internal struct PropertyMembers
    {
        public VariableDeclaration Field { get; set; }
        public FunctionDefinition Getter { get; set; }
        public FunctionDefinition Setter { get; set; }
    }
}
