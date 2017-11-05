using System;
using System.Collections.Generic;

namespace Skila.Language.Semantics
{
   public enum ExecutionMode
    {
        Certain,
        Maybe,
        Unreachable,
    }

    public static class ExecutionModeExtensions
    {
        internal static ExecutionMode GetMoreUncertain(this ExecutionMode @this,ExecutionMode other)
        {
            return (ExecutionMode)Math.Max((int)other, (int)@this);
        }
    }
}