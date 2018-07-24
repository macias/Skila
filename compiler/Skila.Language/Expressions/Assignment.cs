using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Extensions;
using Skila.Language.Entities;
using Skila.Language.Semantics;
using Skila.Language.Expressions.Literals;
using Skila.Language.Printout;

namespace Skila.Language.Expressions
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class Assignment : Expression, ILambdaTransfer
    {
        public static IExpression CreateStatement(string lhs, string rhsValue)
        {
            return create(ExpressionReadMode.CannotBeRead, NameReference.Create(lhs), NameReference.Create(rhsValue),
                isPostInitialization: false);
        }
        public static IExpression CreateStatement(IExpression lhs, IExpression rhsValue)
        {
            return create(ExpressionReadMode.CannotBeRead, lhs, rhsValue, isPostInitialization: false);
        }
        public static Assignment CreateInitialization(IExpression lhs, IExpression rhsValue)
        {
            return create(ExpressionReadMode.CannotBeRead, lhs, rhsValue, isPostInitialization: true).Cast<Assignment>();
        }
        public static IExpression CreateStatement(IEnumerable<IExpression> lhs, IEnumerable<IExpression> rhsValue)
        {
            return create(ExpressionReadMode.CannotBeRead, lhs, rhsValue);
        }
        public static IExpression CreateExpression(IExpression lhs, IExpression rhsValue)
        {
            return create(ExpressionReadMode.ReadRequired, lhs, rhsValue, isPostInitialization: false);
        }
        public static IExpression CreateExpression(IEnumerable<IExpression> lhs, IEnumerable<IExpression> rhsValue)
        {
            return create(ExpressionReadMode.ReadRequired, lhs, rhsValue);
        }
        private static IExpression create(ExpressionReadMode readMode, IExpression lhs, IExpression rhsValue, bool isPostInitialization)
        {
            if (lhs is FunctionCall call && call.IsIndexer)
            {
                if (readMode != ExpressionReadMode.CannotBeRead)
                    throw new NotImplementedException();

                return call.ConvertIndexerIntoSetter(rhsValue);
            }
            else
                return new Assignment(readMode, lhs, rhsValue, isPostInitialization);
        }
        private static IExpression create(ExpressionReadMode readMode, IEnumerable<IExpression> lhsExpr,
            IEnumerable<IExpression> rhsValue)
        {
            if (lhsExpr.Count() == 1 && rhsValue.Count() == 1)
            {
                return create(readMode, lhsExpr.Single(), rhsValue.Single(), isPostInitialization: false);
            }
            else if (lhsExpr.Count() == 1)
            {
                return create(readMode, lhsExpr.Single(), ExpressionFactory.Tuple(rhsValue.ToArray()), isPostInitialization: false);
            }
            else
            {
                string rhs_temp_name = AutoName.Instance.CreateNew("par");
                var code = new List<IExpression>();

                if (rhsValue.Count() == 1 && rhsValue.Single() is Spread spread)
                {
                    spread.RouteSetup(lhsExpr.Count(), lhsExpr.Count() + 1);

                    // let par = RHS 
                    code.Add(VariableDeclaration.CreateStatement(rhs_temp_name, null, spread));
                    int i = 0;
                    foreach (IExpression lhs in lhsExpr)
                    {
                        // LHS[i] = par.at(i)
                        code.Add(Assignment.CreateStatement(lhs, FunctionCall.Create(NameReference.Create(rhs_temp_name, NameFactory.PropertyIndexerName),
                            NatLiteral.Create($"{i}"))));
                        ++i;
                    }
                }
                else
                {
                    // let par = new Tuple(RHS)
                    code.Add(VariableDeclaration.CreateStatement(rhs_temp_name, null, ExpressionFactory.Tuple(rhsValue.ToArray())));
                    int i = 0;
                    foreach (IExpression lhs in lhsExpr)
                    {
                        // LHS[i] = par.Item[i]
                        code.Add(Assignment.CreateStatement(lhs, NameReference.Create(rhs_temp_name, NameFactory.TupleItemName(i))));
                        ++i;
                    }

                }

                if (readMode != ExpressionReadMode.CannotBeRead)
                    code.Add(NameReference.Create(rhs_temp_name));

                return Block.Create(readMode, code);
            }
        }


        // we don't override IsLValue in Assignment and VariableDeclaration 
        // because while technically it is lvalue, it is ridiculous to see
        // (var x = y) = 7;
        // it looks more like a bug in code and besides, if anything it should be chain assignment
        // var x = (y = 7);

        public IExpression Lhs { get; }
        private IExpression rhsValue;
        public IExpression RhsValue => this.rhsValue;

        private readonly List<TypeDefinition> closures;

        public override IEnumerable<INode> OwnedNodes => new INode[] { RhsValue, Lhs }.Concat(closures).Where(it => it != null);

        private readonly bool isPostInitialization;

        private Assignment(ExpressionReadMode readMode, IExpression lhs, IExpression rhsValue, bool isPostInitialization)
            : base(readMode)
        {
            // C#-like object initialization -- `new Point() { X = 5, Y = 7 }`
            this.isPostInitialization = isPostInitialization;

            this.Lhs = lhs;
            this.rhsValue = rhsValue;

            this.closures = new List<TypeDefinition>();

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }
        public override ICode Printout()
        {
            var code = new CodeSpan(this.Lhs.Printout(), 
                new CodeText(" " + (this.ReadMode == ExpressionReadMode.ReadRequired ? "<-" : "=") + " "), 
                this.RhsValue.Printout());
            if (this.ReadMode == ExpressionReadMode.ReadRequired)
            {
                code.Prepend("(");
                code.Append(")");
            }

            return code;
        }

        public override string ToString()
        {
            return Printout().ToString();
        }

        public override bool IsReadingValueOfNode(IExpression node)
        {
            return true;
        }

        public override void Validate(ComputationContext ctx)
        {
            if (ctx.ValAssignTracker != null)
            {
                if (!ctx.ValAssignTracker.AssignedLocal(ctx, this.Lhs))
                    ctx.AddError(ErrorCode.CannotReassignReadOnlyVariable, this);
            }

            if (!ctx.Env.Options.DiscardingAnyExpressionDuringTests && this.Lhs.IsSink() && !(this.RhsValue is FunctionCall))
                ctx.AddError(ErrorCode.DiscardingNonFunctionCall, this);

            RhsValue.ValidateValueExpression(ctx);

            bool targets_this_type = this.Lhs.TargetsCurrentTypeMember(out IMember dummy);
            if (targets_this_type)
            {
                FunctionDefinition func = this.EnclosingScope<FunctionDefinition>();
                if (!func.Modifier.HasMutable && !func.IsAnyConstructor())
                {
                    ctx.AddError(ErrorCode.AlteringCurrentInImmutableMethod, this);
                }
            }

            if (this.Lhs is Dereference)
            {
                TypeMutability lhs_mutability = this.Lhs.Evaluation.Components.MutabilityOfType(ctx);
                if (!lhs_mutability.HasFlag(ctx.Env.Options.ReassignableTypeMutability()))
                    ctx.AddError(ErrorCode.AssigningToNonReassignableData, this);
            }

            {
                IEntityVariable lhs_var = this.Lhs.TryGetTargetEntity<IEntityVariable>(out NameReference name_ref);
                if (lhs_var != null)
                {
                    if (this.DebugId== (29, 29))
                    {
                        ;
                    }

                    FunctionDefinition current_func = this.EnclosingScope<FunctionDefinition>();
                    // we deal with local assignments using assignment tracker
                    bool can_reassign = name_ref.Binding.Match.IsLocal;
                    if (!can_reassign)
                    {
                        TypeDefinition current_type = current_func?.ContainingType();
                        TypeDefinition lhs_owner_type = lhs_var.ContainingType();
                        can_reassign = lhs_var.Modifier.Has(ctx.Env.Options.ReassignableModifier())
                            || (lhs_var.Modifier.HasPostInitialization && this.isPostInitialization)
                            || name_ref.Binding.Match.IsLocal
                            || (current_func != null && current_type == lhs_owner_type
                            && (current_func.IsInitConstructor() || current_func.IsZeroConstructor()));
                    }

                    if (!can_reassign)
                        ctx.AddError(ErrorCode.CannotReassignReadOnlyVariable, this);
                    else
                    {
                        if (lhs_var is Property prop && prop.Setter == null
                            && !prop.Getter.Modifier.HasAutoGenerated
                            // custom getter cannot have post initialization in the first place
                            && !this.isPostInitialization)
                            ctx.AddError(ErrorCode.CannotAssignCustomProperty, this);

                        if (sameTargets(this.Lhs, this.RhsValue))
                            ctx.AddError(ErrorCode.SelfAssignment, this);
                        // for controling mutating "this/base" instance we have other checks
                        else if (name_ref.Prefix != null && !targets_this_type && !this.isPostInitialization)
                        {
                            // we cannot call mutable methods on neutral instance as well, because in such case we could
                            // pass const instance (of mutable type) as neutral instance (aliasing const instance)
                            // and then call mutable method making "const" guarantee invalid

                            TypeMutability this_mutability = name_ref.Prefix.Evaluation.Components.MutabilityOfType(ctx);
                            if (!this_mutability.HasFlag(TypeMutability.ForceMutable))
                                ctx.AddError(ErrorCode.AlteringNonMutableInstance, this);
                        }
                    }
                }
            }


            if (!this.Lhs.IsLValue(ctx))
                ctx.AddError(ErrorCode.AssigningRValue, this.Lhs);

        }

        public override void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation == null)
            {
                this.TrapClosure(ctx, ref this.rhsValue);

                if (this.DebugId== (27, 336))
                {
                    ;
                }
                this.DataTransfer(ctx, ref this.rhsValue, Lhs.Evaluation.Components);

                this.Evaluation = this.RhsValue.Evaluation;
            }
        }

        private static bool sameTargets(IExpression lhs, IExpression rhs)
        {
            NameReference lhs_ref;
            IEntity lhs_target = lhs.TryGetTargetEntity<IEntity>(out lhs_ref);
            if (lhs_target == null)
                return false;
            NameReference rhs_ref;
            IEntity rhs_target = rhs.TryGetTargetEntity<IEntity>(out rhs_ref);
            if (lhs_target != rhs_target)
                return false;

            if (lhs_ref.Prefix == null && rhs_ref.Prefix == null)
                return true;
            else
                return sameTargets(lhs_ref.Prefix, rhs_ref.Prefix);
        }

        public void AddClosure(TypeDefinition closure)
        {
            this.closures.Add(closure);
            closure.AttachTo(this);
        }
    }
}
