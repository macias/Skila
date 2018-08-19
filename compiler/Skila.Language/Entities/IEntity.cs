using System.Collections.Generic;

namespace Skila.Language.Entities
{
    public interface IEntity : IEvaluable, INameBindable
    {
        EntityModifier Modifier { get; }
        EntityInstance InstanceOf { get; }

        EntityInstance GetInstance(TypeMutability overrideMutability, TemplateTranslation translation, Lifetime lifetime);
    }

}
