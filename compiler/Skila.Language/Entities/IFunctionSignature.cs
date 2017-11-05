using NaiveLanguageTools.Common;
using System.Collections.Generic;

namespace Skila.Language.Entities
{
    public interface IFunctionSignature : INode
    {
        INameReference ResultTypeName { get; }
        IReadOnlyList<FunctionParameter> Parameters { get; }
    }

    public static class IFunctionSignatureExtension
    {
        public static bool NOT_USED_CounterpartParameters(this IFunctionSignature @this, IFunctionSignature other)
        {
            if (@this.Parameters.Count != other.Parameters.Count)
                return false;

            foreach (var pair in @this.Parameters.SyncZip(other.Parameters))
                if (!pair.Item1.NOT_USED_CounterpartParameter(@this, pair.Item2, other))
                    return false;

            return true;
        }
    }
}