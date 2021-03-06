﻿using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Semantics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Language.Extensions
{
    public static partial class ISurfableExtension
    {
        public static void Surfed(this ISurfable node, ComputationContext ctx)
        {
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

            node.Surf(ctx);

            node.IsSurfed = true;

            ctx.RemoveVisited(node);
        }
    }

}
