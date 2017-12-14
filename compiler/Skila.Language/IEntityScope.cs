using NaiveLanguageTools.Common;
using Skila.Language.Comparers;
using Skila.Language.Entities;
using Skila.Language.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Language
{
    // scope which can hold unordered entities (i.e. it is not executed like block)
    public interface IEntityScope : IScope
    {
        IEnumerable<IEntity> AvailableEntities { get; }
    }

    public enum EntityFindMode
    {
        ScopeLimited,
        WithCurrentProperty,
        AvailableIndexersOnly
    }
}
