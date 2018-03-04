using Skila.Language.Extensions;

namespace Skila.Language
{
    public interface INameBindable : IBindable
    {
        NameDefinition Name { get; }
    }
}
