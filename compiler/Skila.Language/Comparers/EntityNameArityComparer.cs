using System.Collections.Generic;

namespace Skila.Language.Comparers
{
    public sealed class EntityNameArityComparer : IEqualityComparer<ITemplateName>
    {
        public static IEqualityComparer<ITemplateName> Instance = new EntityNameArityComparer();

        private EntityNameArityComparer()
        {

        }
        public bool Equals(ITemplateName x, ITemplateName y)
        {
            return EntityBareNameComparer.Instance.Equals(x,y) && x.Arity == y.Arity;
        }

        public int GetHashCode(ITemplateName obj)
        {
            return EntityBareNameComparer.Instance.GetHashCode(obj)
                ^ obj.Arity.GetHashCode();
        }
    }
}
