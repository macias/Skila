using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Extensions;
using Skila.Language.Printout;

namespace Skila.Language.Expressions
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class Block : Expression, IExecutableScope
    {
        public static Block Create(ExpressionReadMode readMode, IEnumerable<IExpression> body)
        {
            return constructor(readMode,null, body);
        }
        public static Block Create(Func<Block, ExpressionReadMode> readModeCalc, IEnumerable<IExpression> body)
        {
            return constructor( ExpressionReadMode.OptionalUse, readModeCalc,body);
        }
        public static Block CreateStatement(params IExpression[] body)
        {
            return constructor( ExpressionReadMode.CannotBeRead,null, body);
        }
        public static Block CreateStatement(IEnumerable<IExpression> body)
        {
            return constructor( ExpressionReadMode.CannotBeRead,null, body);
        }
        public static Block CreateStatement()
        {
            return constructor(ExpressionReadMode.CannotBeRead,null, null);
        }
        public static Block CreateExpression(params IExpression[] body)
        {
            return constructor(ExpressionReadMode.ReadRequired, null, body);
        }
        public static Block CreateExpression(IEnumerable<IExpression> body)
        {
            return constructor(ExpressionReadMode.ReadRequired,null, body);
        }

        internal FunctionCall constructorChainCall { get; private set; } // used in constructors
        private IExpression zeroConstructorCall;

        private readonly List<IExpression> instructions;
        public IEnumerable<IExpression> Instructions => new[] { constructorChainCall, zeroConstructorCall }
            .Where(it => it != null)
            .Concat(this.instructions);

        public override IEnumerable<INode> ChildrenNodes => Instructions.Select(it => it.Cast<IOwnedNode>());

        private readonly Func<Block, ExpressionReadMode> readModeCalc;

        private static Block constructor( ExpressionReadMode readMode, Func<Block, ExpressionReadMode> readModeCalc,
            IEnumerable<IExpression> instructions)
        {
            List<IExpression> body = (instructions ?? Enumerable.Empty<IExpression>()).ToList();
            return new Block( readMode,readModeCalc, body);
        }

        private Block( ExpressionReadMode readMode, Func<Block, ExpressionReadMode> readModeCalc, List<IExpression> body)
            : base(readModeCalc == null ? new Option<ExpressionReadMode>(readMode) : new Option<ExpressionReadMode>())
        {
            this.instructions = body;
            this.readModeCalc = readModeCalc;

            this.attachPostConstructor();
        }
        public override string ToString()
        {
            int count = this.Instructions.Count();
            return (this.Instructions.FirstOrDefault()?.Printout()?.ToString() ?? "") + (count > 1 ? $"...{{{count}}}" : "");
        }
        public override ICode Printout()
        {
            var code = new CodeDiv(this,this.instructions.Select(it => new CodeSpan(it).Append(";")).ToArray()).Indent();
            code.Prepend("{").Append("}");
            return code;
        }
        public override bool IsReadingValueOfNode(IExpression node)
        {
            if (this.ReadMode == ExpressionReadMode.CannotBeRead || this.Instructions.LastOrDefault() != node)
                return false;

            return this.ReadMode == ExpressionReadMode.ReadRequired
                    || (this.Owner is IExpression owner_expr && owner_expr.IsReadingValueOfNode(this));
        }
        public override void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation == null)
            {
                IExpression last = this.Instructions.LastOrDefault();
                if (!this.isReadModeSet)
                    this.setReadMode(readModeCalc(this));
                this.Evaluation = (last?.Evaluation ?? ctx.Env.UnitEvaluation).PromotLifetime(ctx,this);
            }
        }

        internal void Append(IExpression instruction)
        {
            if (this.Evaluation != null)
                throw new Exception("Internal error");

            this.instructions.Add(instruction);
            instruction.AttachTo(this);
        }

        internal void SetZeroConstructorCall(FunctionCall call)
        {
            if (this.zeroConstructorCall != null)
                throw new Exception("Internal error");

            this.zeroConstructorCall = call;
            call.AttachTo(this);
        }
        internal void SetConstructorChainCall(FunctionCall call)
        {
            if (this.constructorChainCall != null)
                throw new Exception("Internal error");

            this.constructorChainCall = call;
            call.AttachTo(this);
        }
    }
}
