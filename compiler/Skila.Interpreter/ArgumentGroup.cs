using NaiveLanguageTools.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Interpreter
{
    internal struct ArgumentGroup
    {
        private enum Mode
        {
            None,
            Single,
            Many
        }

        public static ArgumentGroup None()
        {
            return new ArgumentGroup(Mode.None);
        }
        public static ArgumentGroup Single(ObjectData arg)
        {
            return new ArgumentGroup(Mode.Single, arg);
        }
        public static ArgumentGroup Many(params ObjectData[] arg)
        {
            return new ArgumentGroup(Mode.Many, arg);
        }

        private readonly Mode mode;
        public IReadOnlyCollection<ObjectData> Arguments { get; }

        public bool IsNone => this.mode == Mode.None;
        public bool IsSingle => this.mode == Mode.Single;

        private ArgumentGroup(Mode mode, params ObjectData[] arg)
        {
            this.mode = mode;
            this.Arguments = arg;
        }

        static internal void TryReleaseVariadic(ExecutionContext ctx, IEnumerable<ArgumentGroup> groups)
        {
            foreach (ArgumentGroup grp in groups)
                grp.TryReleaseVariadic(ctx);
        }
        internal void TryReleaseVariadic(ExecutionContext ctx)
        {
            // we need to clean "manually" after variadic arguments, because entire group is passed by reference
            // and references are not removed from heap automatically, we cannot remove it inside function
            // because variadic arguments can be legally created from collections
            if (this.mode == Mode.Many)
                foreach (ObjectData arg in this.Arguments)
                    ctx.Heap.TryRelease(ctx, arg, null, false, RefCountDecReason.DeconstructingVariadic, "");
        }
    }

}
