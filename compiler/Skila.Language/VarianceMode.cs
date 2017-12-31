using System;

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
        public static VarianceMode Flipped(this VarianceMode position, VarianceMode paramMode)
        {
            switch (paramMode)
            {
                case VarianceMode.None: return VarianceMode.None;
                case VarianceMode.In: return position.Inversed();
                case VarianceMode.Out: return position;
                default: throw new NotImplementedException();
            }
        }

        public static bool PositionCollides(this VarianceMode position, VarianceMode paramMode)
        {
            if (position == VarianceMode.None)
                return paramMode != VarianceMode.None;
            else
                return paramMode != VarianceMode.None && position != paramMode;
        }

        public static VarianceMode Inversed(this VarianceMode @this)
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
