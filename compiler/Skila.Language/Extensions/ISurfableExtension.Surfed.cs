using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Semantics;
using System;
using System.Linq;

namespace Skila.Language.Extensions
{
    public static partial class ISurfableExtension
    {
        public static void Surfed(this ISurfable node, ComputationContext ctx)
        {
            if (node.DebugId.Id == 2554)
            {
                ;
            }

            if (node.IsSurfed)
                return;

            if (!ctx.AddVisited(node))
            {
                if (node != null)
                {
                    if (!node.IsSurfed)
                        ctx.AddError(ErrorCode.CircularReference, node);
                }

                return;
            }

            node.Surfables.ForEach(it => Surfed(it, ctx));
            node.Surf(ctx);

            node.IsSurfed = true;

            ctx.RemoveVisited(node);

            return;
        }
    }

}
