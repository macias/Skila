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

        public static Variadic Create(int min, int max)
        {
            return new Variadic(min, max);
        }
        public static Variadic Create(int min)
        {
            return new Variadic(min, null);
        }
        public static Variadic Create()
        {
            // variadic without limits
            return Create(0);
        }

        private readonly bool isSet;
        public int MinLimit { get; }
        // when set, it is exclusive
        private readonly int? max1Limit;
        public int Max1Limit => this.max1Limit ?? int.MaxValue;

        public bool HasValidLimits { get { return MinLimit>=0 && IsWithinLimits(MinLimit); } }

        public bool HasUpperLimit => this.max1Limit.HasValue;
        public bool HasLowerLimit => this.MinLimit > 0;

        private Variadic()
        {
        }
        private Variadic(int min, int? max1)
        {
            this.isSet = true;
            this.MinLimit = min;
            this.max1Limit = max1;
        }
        public override string ToString()
        {
            if (!this.isSet)
                return "";
            else
                return new[] { MinLimit>0 ? MinLimit.ToString() : "",
                "...",
                max1Limit.HasValue ? max1Limit.ToString() : "" }
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

            return this.isSet == comp.isSet && MinLimit.Equals(comp.MinLimit) && Max1Limit.Equals(comp.Max1Limit);
        }

        public override int GetHashCode()
        {
            return isSet.GetHashCode() ^ MinLimit.GetHashCode() ^ Max1Limit.GetHashCode();
        }

        public bool IsWithinLimits(int count)
        {
            return MinLimit <= count && count < Max1Limit;
        }
    }

}


