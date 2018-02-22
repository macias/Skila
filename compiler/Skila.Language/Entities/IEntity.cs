using Skila.Language.Extensions;
using System.Collections.Generic;

namespace Skila.Language.Entities
{
    public interface IEntity : IEvaluable, IBindable
    {
        EntityModifier Modifier { get; }
        EntityInstance InstanceOf { get; }

        EntityInstance GetInstance(IEnumerable<IEntityInstance> arguments, MutabilityOverride overrideMutability,TemplateTranslation translation);
    }

}
