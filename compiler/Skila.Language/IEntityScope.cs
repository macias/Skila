using System.Collections.Generic;

namespace Skila.Language
{
    // scope which can hold unordered entities (i.e. it is not executed like block)
    public interface IEntityScope : IScope
    {
        IEnumerable<EntityInstance> AvailableEntities { get; }
    }

    public enum EntityFindMode
    {
        ScopeLimited,
        WithCurrentProperty,
        AvailableIndexersOnly
    }
}
