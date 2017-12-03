using System.Collections.Generic;

namespace Skila.Language.Builders
{
    public interface IMultiBuilder<out T>
      //  where T : INode
    {
        IEnumerable<T> Build();
    }

}
