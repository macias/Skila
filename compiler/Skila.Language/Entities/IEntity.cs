using System.Collections.Generic;

namespace Skila.Language.Entities
{
    public interface IEntity : IEvaluable, IBindable
    {
        EntityModifier Modifier { get; }

        EntityInstance GetInstanceOf(IEnumerable<IEntityInstance> arguments, bool overrideMutability);
  }

    public static class EntityExtensions
    {
        /// <returns>null for non-methods and alike</returns>
        public static TypeDefinition OwnerType(this IEntity @this)
        {
            TemplateDefinition scope = @this.EnclosingScope<TemplateDefinition>();
            return scope.IsType() ? scope.CastType() : null;
        }
    }
}
