using System.Collections.Generic;

namespace Skila.Language
{
    public interface ITemplateParameters
    {
        IReadOnlyList<TemplateParameter> Parameters { get; }
    }
}
