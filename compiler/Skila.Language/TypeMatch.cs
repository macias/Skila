using System;

namespace Skila.Language
{
    public struct TypeMatch
    {
        [Flags]
        private enum MatchFlag
        {
            No = 0,

            Same = 1 << 1,
            Substitute = 1 << 2,
            InConversion = 1 << 3,
            OutConversion = 1 << 4,
            ImplicitReference = 1 << 5,
            AutoDereference = 1 << 6,
        }

        public static TypeMatch No
        {
            get
            {
                return new TypeMatch(MatchFlag.No);
            }
        }
        public static readonly TypeMatch Same = new TypeMatch(MatchFlag.Same);
        public static readonly TypeMatch Substitute = new TypeMatch(MatchFlag.Substitute);
        public static readonly TypeMatch InConversion = new TypeMatch(MatchFlag.InConversion);
        public static readonly TypeMatch OutConversion = new TypeMatch(MatchFlag.OutConversion);
        public static readonly TypeMatch ImplicitReference = new TypeMatch(MatchFlag.ImplicitReference);
        public static readonly TypeMatch AutoDereference = new TypeMatch(MatchFlag.AutoDereference);

        public static TypeMatch Substituted(int distance)
        {
            if (distance == 1)
            {
                ;
            }
            return new TypeMatch(TypeMatch.Substitute.flag, distance);
        }
        public static TypeMatch Mismatched(bool mutability)
        {
            return new TypeMatch(TypeMatch.No.flag, mutability ? 1 : 0);
        }

        private readonly MatchFlag flag;
        private readonly int data;

        public int Distance => this.data; // makes sense for substitution
        public bool Mutability => this.data == 1; // make sense for rejection

        private TypeMatch(MatchFlag flag, int data = 0)
        {
            this.flag = flag;
            this.data = data;
        }

        public override string ToString()
        {
            string s = this.flag.ToString();

            if (this.flag.HasFlag(MatchFlag.Substitute))
                s += $"({Distance})";

            return s;
        }
        public override bool Equals(object obj)
        {
            if (obj is TypeMatch m)
                return this.Equals(m);
            else
                throw new Exception();
        }
        public bool Equals(TypeMatch obj)
        {
            return this.flag == obj.flag;
        }
        public override int GetHashCode()
        {
            return this.flag.GetHashCode();
        }
        public static bool operator ==(TypeMatch m1, TypeMatch m2)
        {
            return m1.flag == m2.flag;
        }
        public static TypeMatch operator ^(TypeMatch m1, TypeMatch m2)
        {
            return new TypeMatch(m1.flag ^ m2.flag, m1.Distance);
        }
        public static TypeMatch operator |(TypeMatch m1, TypeMatch m2)
        {
            return new TypeMatch(m1.flag | m2.flag, m1.Distance);
        }
        public static bool operator !=(TypeMatch m1, TypeMatch m2)
        {
            return !(m1 == m2);
        }

        public bool HasFlag(TypeMatch other)
        {
            return (this.flag & other.flag) != 0;
        }
    }
}
