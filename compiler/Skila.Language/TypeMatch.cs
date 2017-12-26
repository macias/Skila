using System;

namespace Skila.Language
{
    /*   [Flags]
       public enum TypeMatch
       {
           No = 0,

           Same = 1 << 1,
           Substitute = 1 << 2,
           InConversion = 1 << 3,
           OutConversion = 1 << 4,
           ImplicitReference = 1 << 5,
           AutoDereference = 1 << 6,
       }
       */

    public struct TypeMatch
    {
        public static readonly TypeMatch No = new TypeMatch(0);
        public static readonly TypeMatch Same = new TypeMatch(1 << 1);
        public static readonly TypeMatch Substitute = new TypeMatch(1 << 2);
        public static readonly TypeMatch InConversion = new TypeMatch(1 << 3);
        public static readonly TypeMatch OutConversion = new TypeMatch(1 << 4);
        public static readonly TypeMatch ImplicitReference = new TypeMatch(1 << 5);
        public static readonly TypeMatch AutoDereference = new TypeMatch(1 << 6);

        public static TypeMatch Substitution(int distance)
        {
            return new TypeMatch(TypeMatch.Substitute.flag, distance);
        }

        private readonly int flag;
        public int Distance { get; } // makes sense for substitution

        private TypeMatch(int flag, int distance = 0)
        {
            this.flag = flag;
            this.Distance = distance;
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

        internal bool HasFlag(TypeMatch other)
        {
            return (this.flag & other.flag) != 0;
        }
    }
}
