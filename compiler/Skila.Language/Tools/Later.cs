﻿using NaiveLanguageTools.Common;
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
        public bool HasValue => this.value.HasValue;

        private readonly Func<R> factory;

        public Later(Func<R> factory)
        {
            this.factory = factory;
            this.value = new Option<R>();
        }
    }

    public static class Later
    {
        public static Later<R> Create<R>(Func<R> factory)
        {
            return new Later<R>(factory);
        }
    }
}
