using System;
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
        private IReadOnlyCollection<BindingMatch> matches;
        public IReadOnlyCollection<BindingMatch> Matches => this.matches;
        public bool HasMatch => this.Matches.Count > 0;

        public BindingMatch Match
        {
            get
            {
                if (!this.IsComputed)
                    throw new InvalidOperationException();
                return this.Matches.Any() ? this.Matches.First() : BindingMatch.Joker;
            }
        }

        public Binding()
        {
        }
        public void Set(IEnumerable<BindingMatch> matches)
        {
            this.matches = matches.StoreReadOnly();
            //if (this.Matches.Any(it => it.IsJoker))
            //   throw new ArgumentException("Cannot pass joker for binding.");
            if (this.IsComputed)
                throw new InvalidOperationException("Matches already set.");
            this.IsComputed = true;
        }

        public void SetNone()
        {
            Set(Enumerable.Empty<BindingMatch>());
        }
        public void Filter(Func<EntityInstance, bool> matching)
        {
            this.matches = this.Matches.Where(it => matching(it.Instance)).ToArray();
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