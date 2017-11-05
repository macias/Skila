using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Extensions;
using Skila.Language.Flow;
using Skila.Language.Semantics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Language.Extensions
{
    public static partial class IEvaluableExtension
    {
        private static void validateReadingValues( IExpression node, ComputationContext ctx)
        {
            if (node.DebugId.Id == 7584)
            {
                ;
            }
            var parent = node.Owner as IExpression;
            if (parent == null)
                return;

            bool parent_reading = parent.IsReadingValueOfNode(node);
            node.IsRead = parent_reading;
            if (parent_reading && ctx.ValAssignTracker != null && node is VariableDeclaration decl)
            {
                if (!ctx.ValAssignTracker.CanRead(decl))
                    ctx.AddError(ErrorCode.VariableNotInitialized, parent, decl);
            }

            if (node.ReadMode == ExpressionReadMode.CannotBeRead)
            {
                if (parent_reading)
                    ctx.AddError(ErrorCode.CannotReadExpression, node);
            }
            else
            {
                if (parent_reading && ctx.Env.IsVoidType(node.Evaluation))
                    ctx.AddError(ErrorCode.PassingVoidValue, node);

                if (node.ReadMode == ExpressionReadMode.OptionalUse)
                    return;
                else if (node.ReadMode == ExpressionReadMode.ReadRequired)
                {
                    if (!parent_reading)
                        ctx.AddError(ErrorCode.ExpressionValueNotUsed, node);
                }
                else
                    throw new InvalidOperationException();
            }
        }

        private static void validateExecutionPath(INode node, IEnumerable<IExpression> path,
            ComputationContext ctx, ref ValidationData result)
        {
            if (node.DebugId.Id == 259)
            {
                ;
            }
            foreach (IExpression step in path)
            {
                if (!result.UnreachableCodeFound && (result.IsTerminated))
                {
                    result.UnreachableCodeFound = true;
                    ctx.AddError(ErrorCode.UnreachableCode, step);
                }

                ValidationData val = Validated(step, ctx);

                ctx.ValAssignTracker?.UpdateMode(val.GetMode());

                result.AddStep(val);
            }

        }


        public static bool DataTransfer(this IEvaluable @this, ComputationContext ctx, ref IExpression source,
            IEntityInstance targetTypeName)
        {
            if (source == null)
                return true;

            IEntityInstance src_type = source.Evaluated(ctx);
            TypeMatch match = src_type.MatchesTarget(ctx, targetTypeName, allowSlicing: false);

            if (match == TypeMatch.No)
            {
                ctx.ErrorManager.AddError(ErrorCode.TypeMismatch, source);
                return false;
            }
            else if (match == TypeMatch.InConversion)
            {
                source.DetachFrom(@this);
                source = ExpressionFactory.StackConstructorCall((targetTypeName as EntityInstance).NameOf, FunctionArgument.Create(source));
                source.AttachTo(@this);
                if (source.Evaluated(ctx).MatchesTarget(ctx, targetTypeName, allowSlicing: false) != TypeMatch.Pass)
                    throw new Exception("Internal error");
            }
            else if (match == TypeMatch.ImplicitReference)
            {
                source.DetachFrom(@this);
                source = AddressOf.CreateReference(source);
                source.AttachTo(@this);
                if (source.Evaluated(ctx).MatchesTarget(ctx, targetTypeName, allowSlicing: true) != TypeMatch.Pass)
                    throw new Exception("Internal error");
            }
            else if (match == TypeMatch.OutConversion)
            {
                source.DetachFrom(@this);
                source = FunctionCall.CreateToCall(source, (targetTypeName as EntityInstance).NameOf);
                source.AttachTo(@this);
                if (source.Evaluated(ctx).MatchesTarget(ctx, targetTypeName, allowSlicing: false) != TypeMatch.Pass)
                    throw new Exception("Internal error");
            }
            else if (match == TypeMatch.AutoDereference)
            {
                source.IsDereferenced = true;
            }
            else if (match != TypeMatch.Pass)
                throw new NotImplementedException();

            return true;
        }

    }

}
