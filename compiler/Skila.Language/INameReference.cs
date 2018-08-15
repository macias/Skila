using Skila.Language.Extensions;
using Skila.Language.Printout;

namespace Skila.Language
{
    public interface INameReference : IReferentialName,ISurfable,IPrintable
    {
        bool IsBindingComputed { get; }

        bool IsExactlySame(INameReference otherTypeName, EntityInstance translationTemplate, bool jokerMatchesAll);
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
                typeInstance = nameReference.Evaluation.Components.Cast<EntityInstance>();
                return true;
            }
        }
    }
}