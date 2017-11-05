using System;

namespace Skila.Interpreter
{
    public struct ExecValue
    {
        public static ExecValue Undefined { get; } = new ExecValue(false, null);

        public static ExecValue CreateReturn(ObjectData value)
        {
            return new ExecValue(true, value);
        }
        public static ExecValue CreateExpression(ObjectData value)
        {
            return new ExecValue(false, value);
        }

        public bool IsReturn { get; }
        private readonly ObjectData value;

        public ObjectData RetValue
        {
            get
            {
                if (!IsReturn)
                    throw new Exception();
                return value;
            }
        }

        public ObjectData ExprValue
        {
            get
            {
                if (IsReturn)
                    throw new Exception();
                return value;
            }
        }

        private ExecValue(bool isReturn, ObjectData value)
        {
            this.IsReturn = isReturn;
            this.value = value;
        }
    }
}
