using Skila.Language.Extensions;
using System.Collections.Generic;

namespace Skila.Language
{
    public interface INameReference : IReferentialName
    {
        bool IsBindingComputed { get; }
    }

    public static class INameReferenceExtensions
    {
        public static bool TryGetSingleType(this INameReference @this,out NameReference nameReference,out EntityInstance typeInstance)
        {
            nameReference = @this as NameReference;
            if (nameReference==null)
            {
                typeInstance = null;
                return false;
            }
            else
            {
                typeInstance = nameReference.Evaluation.Cast<EntityInstance>();
                return true;
            }
        }
    }
}