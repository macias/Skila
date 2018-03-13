using System.Collections.Generic;
using Skila.Language.Extensions;
using System.Collections;
using System.Diagnostics;

namespace Skila.Language
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class ExecutionPath : IEnumerable<IExpression>
    {
#if DEBUG
        public DebugId DebugId { get; }
#endif

        internal static ExecutionPath Create(IEnumerable<IExpression> path,bool isRepeated = false)
        {
            if (path == null)
                return null;
            else
                return new ExecutionPath(path,isRepeated);
        }

        private readonly IReadOnlyCollection<IExpression> path;
        public bool IsRepeated { get; }

        private ExecutionPath(IEnumerable<IExpression> path,bool isRepeated)
        {
#if DEBUG
            this.DebugId = new DebugId(this.GetType());
#endif
            this.IsRepeated = isRepeated;
            this.path = path.StoreReadOnly();
        }

        public IEnumerator<IExpression> GetEnumerator()
        {
            return path.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return path.GetEnumerator();
        }

        public override string ToString()
        {
            return $"{this.path.Count} steps";
        }
    }
}