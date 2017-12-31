﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Extensions;
using Skila.Language.Entities;

namespace Skila.Language.Expressions
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class Block : Expression, IExecutableScope
    {
        public enum Purpose
        {
            Initialization,
            Regular
        }
        public static Block Create(ExpressionReadMode readMode, IEnumerable<IExpression> body)
        {
            return constructor(Purpose.Regular, readMode, body);
        }
        public static Block CreateStatement(params IExpression[] body)
        {
            return constructor(Purpose.Regular, ExpressionReadMode.CannotBeRead, body);
        }
        public static Block CreateStatement(IEnumerable<IExpression> body)
        {
            return constructor(Purpose.Regular, ExpressionReadMode.CannotBeRead, body);
        }
        public static Block CreateStatement()
        {
            return constructor(Purpose.Regular, ExpressionReadMode.CannotBeRead, null);
        }
        public static Block CreateExpression(IEnumerable<IExpression> body)
        {
            return constructor(Purpose.Regular, ExpressionReadMode.ReadRequired, body);
        }
        public static Block CreateInitialization(VariableDeclaration decl, FunctionCall init, NameReference outcome)
        {
            return constructor(Purpose.Initialization, ExpressionReadMode.ReadRequired, new IExpression[] { decl, init, outcome });
        }

        internal FunctionCall constructorChainCall { get; private set; } // used in constructors
        private IExpression zeroConstructorCall;

        private readonly List<IExpression> instructions;
        public IEnumerable<IExpression> Instructions => new[] { constructorChainCall, zeroConstructorCall }
            .Where(it => it != null)
            .Concat(this.instructions);

        public override IEnumerable<INode> OwnedNodes => Instructions.Select(it => it.Cast<INode>());

        public Purpose Mode { get; }
        // applies only for initialization block
        public FunctionCall InitializationStep => this.instructions[1].Cast<FunctionCall>();

        private static Block constructor(Purpose purpose, ExpressionReadMode readMode, IEnumerable<IExpression> instructions)
        {
            List<IExpression> body = (instructions ?? Enumerable.Empty<IExpression>()).ToList();
            return new Block(purpose, readMode, body);
        }
        private Block(Purpose purpose, ExpressionReadMode readMode, List<IExpression> body) : base(readMode)
        {
            this.instructions = body;
            this.Mode = purpose;

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }
        public override string ToString()
        {
            int count = this.Instructions.Count();
            return (this.Instructions.FirstOrDefault()?.ToString() ?? "") + (count > 1 ? $"...{{{count}}}" : "");
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
                this.Evaluation = last?.Evaluation ?? ctx.Env.UnitEvaluation;
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
