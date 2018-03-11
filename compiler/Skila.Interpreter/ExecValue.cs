using NaiveLanguageTools.Common;
using System;
using System.Collections.Generic;

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

    }
}