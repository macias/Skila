﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Skila.Language.Extensions;

namespace Skila.Language
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class Binding
    {
#if DEBUG
        private readonly DebugId debugId = new DebugId(typeof(Binding));
#endif

        public bool IsComputed { get; private set; }
        private IReadOnlyCollection<EntityInstance> matches;
        public IReadOnlyCollection<EntityInstance> Matches => this.matches;
        public bool HasMatch => this.Matches.Count > 0;

        public EntityInstance Match
        {
            get
            {
                if (!this.IsComputed)
                    throw new InvalidOperationException();
                return this.Matches.FirstOrDefault() ?? EntityInstance.Joker;
            }
        }

        public Binding()
        {
        }
        public void Set(IEnumerable<EntityInstance> matches)
        {
            if (this.debugId.Id == 4842)
            {
                ;
            }
            this.matches = matches.StoreReadOnly();
            //if (this.Matches.Any(it => it.IsJoker))
            //   throw new ArgumentException("Cannot pass joker for binding.");
            if (this.IsComputed)
                throw new InvalidOperationException("Matches already set.");
            this.IsComputed = true;
        }

        public void SetNone()
        {
            Set(Enumerable.Empty<EntityInstance>());
        }
        public void Filter(Func<EntityInstance, bool> matching)
        {
            if (this.debugId.Id == 4842)
            {
                ;
            }
            this.matches = this.Matches.Where(matching).ToArray();
        }
        public override string ToString()
        {
            if (!this.IsComputed)
                return "<not computed>";
            else if (!this.HasMatch)
                return "<not found>";
            else
            {
                var result = this.Match.ToString();
                if (this.Matches.Count > 1)
                    result += "(" + this.Matches.Count + ")";
                return result;
            }

        }
    }
}