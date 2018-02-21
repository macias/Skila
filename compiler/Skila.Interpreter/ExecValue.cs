using System;

namespace Skila.Interpreter
{
    public struct ExecValue
    {
        public static ExecValue UndefinedExpression { get; } = new ExecValue(DataMode.Expression, null);

        public static ExecValue CreateReturn(ObjectData value)
        {
            return new ExecValue(DataMode.Return, value);
        }
        public static ExecValue CreateExpression(ObjectData value)
        {
            return new ExecValue(DataMode.Expression, value);
        }
        internal static ExecValue CreateThrow(ObjectData value)
        {
            return new ExecValue(DataMode.Throw, value);
        }

        public DataMode Mode { get; }
        public bool IsThrow => this.Mode == DataMode.Throw;
        private readonly ObjectData value;

        public ObjectData RetValue
        {
            get
            {
                if (this.Mode != DataMode.Return)
                    throw new Exception($"Current mode {this.Mode} {ExceptionCode.SourceInfo()}");
                return value;
            }
        }

        public ObjectData ExprValue
        {
            get
            {
                if (this.Mode != DataMode.Expression)
                    throw new Exception($"Current mode {this.Mode} {ExceptionCode.SourceInfo()}");
                return value;
            }
        }

        public ObjectData ThrowValue
        {
            get
            {
                if (this.Mode != DataMode.Throw)
                    throw new Exception($"Current mode {this.Mode} {ExceptionCode.SourceInfo()}");
                return value;
            }
        }

        private ExecValue(DataMode mode, ObjectData value)
        {
            this.Mode = mode;
            this.value = value;
        }

        public override string ToString()
        {
            return $"{Mode} {value}";
        }

    }
}
