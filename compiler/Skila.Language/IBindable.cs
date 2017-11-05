using Skila.Language.Extensions;

namespace Skila.Language
{
    public interface IBindable : INode
    {
        NameDefinition Name { get; }
    }

}
