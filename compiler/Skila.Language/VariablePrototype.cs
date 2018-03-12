using System.Diagnostics;

namespace Skila.Language
{
    // just something more meaningful than a Tuple to hold essentials for VariableDeclaration

    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public struct VariablePrototype
    {
        public string Name { get; }
        public INameReference TypeName { get; }

        public VariablePrototype(string name,INameReference typeName)
        {
            this.Name = name;
            this.TypeName = typeName;
        }

        public override string ToString()
        {
            return $"{Name} {TypeName}";
        }
    }
}
