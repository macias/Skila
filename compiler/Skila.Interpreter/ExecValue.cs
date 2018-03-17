using NaiveLanguageTools.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Interpreter
{
    public partial struct ExecValue
    {
        public static ExecValue UndefinedExpression { get; } = new ExecValue(DataMode.Expression, null);

        internal static ExecValue CreateRecall(CallInfo callInfo)
        {
            return new ExecValue(DataMode.Recall, callInfo);
        }
        internal static ExecValue CreateReturn(ObjectData value)
        {
            return new ExecValue(DataMode.Return, value);
        }
        internal static ExecValue CreateExpression(ObjectData value)
        {
            return new ExecValue(DataMode.Expression, value);
        }
        internal static ExecValue CreateThrow(ObjectData value)
        {
            return new ExecValue(DataMode.Throw, value);
        }

        private readonly DataMode mode;
        public bool IsThrow => this.mode == DataMode.Throw;
        public bool IsRecall => this.mode == DataMode.Recall;
        public bool IsReturn => this.mode == DataMode.Return;
        public bool IsExpression => this.mode == DataMode.Expression;
        private readonly Variant<object, ObjectData, CallInfo> value;
        private ObjectData valueObjectData => this.value.As<ObjectData>();

        public ObjectData RetValue
        {
            get
            {
                if (this.mode != DataMode.Return)
                    throw new Exception($"Current mode {this.mode} {ExceptionCode.SourceInfo()}");
                return valueObjectData;
            }
        }

        public ObjectData ExprValue
        {
            get
            {
                if (this.mode != DataMode.Expression)
                    throw new Exception($"Current mode {this.mode} {ExceptionCode.SourceInfo()}");
                return valueObjectData;
            }
        }

        public ObjectData ThrowValue
        {
            get
            {
                if (this.mode != DataMode.Throw)
                    throw new Exception($"Current mode {this.mode} {ExceptionCode.SourceInfo()}");
                return valueObjectData;
            }
        }

        internal CallInfo RecallData
        {
            get
            {
                if (!this.IsRecall)
                    throw new Exception($"Current mode {this.mode} {ExceptionCode.SourceInfo()}");
                return value.As<CallInfo>();
            }
        }

        private ExecValue(DataMode mode, object value)
        {
            this.mode = mode;
            this.value = new Variant<object, ObjectData, CallInfo>(value);
        }

        public override string ToString()
        {
            return $"{mode} {value}";
        }

    }

}