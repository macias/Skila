using Skila.Language.Entities;
using System.Collections.Generic;

namespace Skila.Language
{
    // scope which can hold unordered entities (i.e. it is not executed like block)
    public interface IEntityScope : IScope, IEntity
    {
        ScopeTable AvailableEntities { get; }
    }
}
