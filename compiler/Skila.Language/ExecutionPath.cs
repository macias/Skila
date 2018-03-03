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

        internal static ExecutionPath Create(IEnumerable<IExpression> path)
        {
            if (path == null)
                return null;
            else
                return new ExecutionPath(path);
        }

        private readonly IReadOnlyCollection<IExpression> path;

        private ExecutionPath(IEnumerable<IExpression> path)
        {
#if DEBUG
            this.DebugId = new DebugId(this.GetType());
#endif

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