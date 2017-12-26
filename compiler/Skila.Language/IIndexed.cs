using System.Collections.Generic;

namespace Skila.Language
{
    public interface IIndexed
    {
        int Index { get; }
        void SetIndex(int index);
    }

    public static class IIndexedExtensions
    {
        public static IEnumerable<T> Indexed<T>(this IEnumerable<T> @this)
            where T : IIndexed
        {
            int index = 0;
            foreach (T elem in @this)
            {
                elem.SetIndex(index);
                ++index;
                yield return elem;
            }
        }
    }
}