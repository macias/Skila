using NaiveLanguageTools.Common;
using System;

namespace Skila.Language.Tools
{
    // with Lazy is too easy to forget disabling thread-protection which adds 1 SECOND per unit-test run
    public sealed class Later<R>
    {
        private Option<R> value;
        public R Value
        {
            get
            {
                if (!value.HasValue)
                    value = new Option<R>(factory());
                return value.Value;
            }
        }
        private readonly Func<R> factory;

        public Later(Func<R> factory)
        {
            this.factory = factory;
        }
    }
}
