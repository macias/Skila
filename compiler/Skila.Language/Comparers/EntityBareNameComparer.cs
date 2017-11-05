using System.Collections.Generic;

namespace Skila.Language.Comparers
{
    public sealed class EntityBareNameComparer : IEqualityComparer<ITemplateName>
    {
        public static IEqualityComparer<ITemplateName> Instance = new EntityBareNameComparer();

        private EntityBareNameComparer()
        {

        }
        public bool Equals(ITemplateName x, ITemplateName y)
        {
            return x.Name == y.Name;
        }

        public int GetHashCode(ITemplateName obj)
        {
            return obj.Name.GetHashCode();
        }
    }
}
