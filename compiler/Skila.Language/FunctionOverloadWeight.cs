using System;

namespace Skila.Language
{
    // the less the better
    public struct FunctionOverloadWeight
    {
        public int Penalty { get; set; }
        public int SubstitutionDistance { get; set; }

        public FunctionOverloadWeight(int weight) : this()
        {
            this.Penalty = weight;
        }

        private static int compare(FunctionOverloadWeight w1, FunctionOverloadWeight w2)
        {
            if (w1.Penalty < w2.Penalty)
                return -1;
            else if (w1.Penalty > w2.Penalty)
                return +1;
            else if (w1.SubstitutionDistance < w2.SubstitutionDistance)
                return -1;
            else if (w1.SubstitutionDistance > w2.SubstitutionDistance)
                return +1;
            else
                return 0;
        }

        public static bool operator<(FunctionOverloadWeight w1,FunctionOverloadWeight w2)
        {
            return compare(w1, w2) < 0;
        }

        public static bool operator >(FunctionOverloadWeight w1, FunctionOverloadWeight w2)
        {
            return compare(w1, w2) > 0;
        }

        public override string ToString()
        {
            return $"{this.Penalty} ; {this.SubstitutionDistance}";
        }
    }
}
