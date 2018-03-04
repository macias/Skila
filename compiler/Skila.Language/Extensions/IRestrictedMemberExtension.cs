using Skila.Language.Semantics;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Language.Entities
{ 
    public static class IRestrictedMemberExtension
    {
        public static void ValidateRestrictedMember(this IRestrictedMember member,ComputationContext ctx)
        {
            if (member.AccessGrants.Any() && !member.Modifier.HasPrivate)
                ctx.AddError(ErrorCode.AccessGrantsOnExposedMember, member.AccessGrants.First());
        }

    }
}
