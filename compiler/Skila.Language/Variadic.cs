using System;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;

namespace Skila.Language
{
    // unlike other languages, in Skila arguments for variadic parameter have to be given
    // UNLESS there is default value set (as for regular parameter)

    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class Variadic
    {
        public static readonly Variadic None = new Variadic();

        public static Variadic Create(int min, int? max)
        {
            return new Variadic(min, max);
        }

        private readonly bool isSet;
        public int MinLimit { get; }
        public int? MaxLimit { get; }

        public bool HasLimits { get { return MinLimit > 0 || MaxLimit.HasValue; } }

        private Variadic()
        {
        }
        private Variadic(int min, int? max)
        {
            this.isSet = true;
            this.MinLimit = min;
            this.MaxLimit = max;
        }
        public override string ToString()
        {
            return new[] { MinLimit>0 ? MinLimit.ToString() : "",
                "...",
                MaxLimit.HasValue ? MaxLimit.ToString() : "" }
                .Where(it => it != "")
                .Join("");
        }

        public override bool Equals(object obj)
        {
            return this.Equals((Variadic)obj);
        }
        public bool Equals(Variadic comp)
        {
            if (Object.ReferenceEquals(comp, null))
                return false;

            if (Object.ReferenceEquals(this, comp))
                return true;

            return this.isSet==comp.isSet && MinLimit.Equals(comp.MinLimit) && MaxLimit.Equals(comp.MaxLimit);
        }

        public override int GetHashCode()
        {
            return isSet.GetHashCode() ^ MinLimit.GetHashCode() ^ (MaxLimit ?? 0).GetHashCode();
        }

        public bool IsWithinLimits(int count)
        {
            return MinLimit <= count && (!MaxLimit.HasValue || count<=MaxLimit.Value);
        }
    }

}


