using System.Collections.Generic;

namespace Skila.Language.Entities
{
    public interface IEntity : IEvaluable, INameBindable
    {
        EntityModifier Modifier { get; }
        EntityInstance InstanceOf { get; }

        EntityInstance GetInstance(IEnumerable<IEntityInstance> arguments, TypeMutability overrideMutability,
            TemplateTranslation translation,Lifetime lifetime);
    }

}
