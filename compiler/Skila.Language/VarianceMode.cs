using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Skila.Language
{
    public enum VarianceMode
    {
        None,
        In,
        Out,
    }

    public static class VarianceModeExtensions
    {
        public static VarianceMode Inverse(this VarianceMode @this)
        {
            switch (@this)
            {
                case VarianceMode.In: return VarianceMode.Out;
                case VarianceMode.Out: return VarianceMode.In;
                case VarianceMode.None: return VarianceMode.None;
                default: throw new InvalidOperationException();
            }
        }
        public static string ToString(VarianceMode @this)
        {
            switch (@this)
            {
                case VarianceMode.In: return "in";
                case VarianceMode.Out: return "out";
                case VarianceMode.None: return "";
                default: throw new InvalidOperationException();
            }
        }
    }
}
