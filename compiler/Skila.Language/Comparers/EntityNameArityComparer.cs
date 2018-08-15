using System.Collections.Generic;

namespace Skila.Language.Comparers
{
    public sealed class EntityNameArityComparer : IEqualityComparer<ITemplateName>
    {
        public static EntityNameArityComparer Instance = new EntityNameArityComparer();

        private EntityNameArityComparer()
        {

        }
        public bool Equals(ITemplateName x, ITemplateName y)
        {
            return x.Arity == y.Arity && EntityBareNameComparer.Instance.Equals(x,y) ;
        }

        public int GetHashCode(ITemplateName obj)
        {
            return EntityBareNameComparer.Instance.GetHashCode(obj)
                ^ obj.Arity.GetHashCode();
        }
    }
}
