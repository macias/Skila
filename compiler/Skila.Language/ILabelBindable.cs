using Skila.Language.Extensions;

namespace Skila.Language
{
    public interface ILabelBindable : IBindable
    {
        NameDefinition Label { get; }
    }
}
