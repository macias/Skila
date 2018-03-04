using Skila.Language.Semantics;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Language.Entities
{
    public interface IRestrictedMember : IMember
    {
        IEnumerable<LabelReference> AccessGrants { get; }
    }

}
