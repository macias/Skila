using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Semantics;
using System;
using System.Collections.Generic;

namespace Skila.Language.Extensions
{
    public static partial class IEvaluableExtension
    {
        private static void validateReadingValues(IExpression node, ComputationContext ctx)
        {
            var parent = node.Owner as IExpression;
            if (parent == null)
                return;

            bool parent_reading = parent.IsReadingValueOfNode(node);
            node.IsRead = parent_reading;
            if (parent_reading && ctx.ValAssignTracker != null && node is VariableDeclaration decl)
            {
                if (decl.InitValue == null)
                    ctx.AddError(ErrorCode.VariableNotInitialized, parent, decl);
            }

            if (node.ReadMode == ExpressionReadMode.CannotBeRead)
            {
                // consider such code:
                // let x = if true then 5 else throw new Exception();
                // the `else` branch is read (because entire `if` is read) but we don't report an error
                // despite we cannot read this branch because it is terminated
                if (parent_reading && !node.Validation.IsTerminated)
                    ctx.AddError(ErrorCode.CannotReadExpression, node);
            }
            else
            {
                if (node.ReadMode == ExpressionReadMode.OptionalUse)
                    return;
                else if (node.ReadMode == ExpressionReadMode.ReadRequired)
                {
                    if (!parent_reading)
                        ctx.AddError(ErrorCode.ExpressionValueNotUsed, node, parent);
                }
                else
                    throw new InvalidOperationException();
            }
        }

        public static bool DataTransfer(this IEvaluable @this, ComputationContext ctx, ref IExpression source,
            IEntityInstance targetTypeName, bool ignoreMutability = false)
        {
            if (source == null)
                return true;

            IEntityInstance src_type = source.Evaluation.Components;

            if (@this.DebugId== (17, 380))
            {
                ;
            }
            TypeMatch match = src_type.MatchesTarget(ctx, targetTypeName,
                TypeMatching.Create(ctx.Env.Options.InterfaceDuckTyping, allowSlicing: false)
                .WithIgnoredMutability(ignoreMutability)
                .AllowedLifetimeChecking(true));

            if (match.HasFlag(TypeMatch.Attachment))
                match ^= TypeMatch.Attachment;

            if (match == TypeMatch.No)
            {
                ctx.ErrorManager.AddError(ErrorCode.TypeMismatch, source);
                return false;
            }
            else if (match == TypeMatch.Lifetime)
            {
                ctx.ErrorManager.AddError(ErrorCode.EscapingReference, source);
                return false;
            }
            else if (match == TypeMatch.InConversion)
            {
                source.DetachFrom(@this);
                source = ExpressionFactory.StackConstructor((targetTypeName as EntityInstance).NameOf, FunctionArgument.Create(source));
                source.AttachTo(@this);
                TypeMatch m = source.Evaluated(ctx, EvaluationCall.AdHocCrossJump).MatchesTarget(ctx, targetTypeName, TypeMatching.Create(ctx.Env.Options.InterfaceDuckTyping, allowSlicing: false));
                if (m != TypeMatch.Same && m != TypeMatch.Substitute)
                    throw new Exception("Internal error");
            }
            else if (match.HasFlag(TypeMatch.ImplicitReference))
            {
                match ^= TypeMatch.ImplicitReference;
                if (match != TypeMatch.Substitute && match != TypeMatch.Same)
                    throw new NotImplementedException();

                if (@this.DebugId == (29, 335))
                {
                    ;
                }
                source.DetachFrom(@this);
                source = AddressOf.CreateReference(source);
                source.AttachTo(@this);
                IEntityInstance source_eval = source.Evaluated(ctx, EvaluationCall.AdHocCrossJump);
                TypeMatch m = source_eval.MatchesTarget(ctx, targetTypeName, TypeMatching.Create(ctx.Env.Options.InterfaceDuckTyping,
                    allowSlicing: true));
                if (m != TypeMatch.Same && m != TypeMatch.Substitute)
                    throw new Exception($"Internal error: matching result {m}");
            }
            else if (match == TypeMatch.OutConversion)
            {
                source.DetachFrom(@this);
                source = FunctionCall.ConvCall(source, (targetTypeName as EntityInstance).NameOf);
                source.AttachTo(@this);
                TypeMatch m = source.Evaluated(ctx, EvaluationCall.AdHocCrossJump).MatchesTarget(ctx, targetTypeName, TypeMatching.Create(ctx.Env.Options.InterfaceDuckTyping, allowSlicing: false));
                if (m != TypeMatch.Same && m != TypeMatch.Substitute)
                    throw new Exception("Internal error");
            }
            else if (match.HasFlag(TypeMatch.AutoDereference))
            {
                source.DereferencedCount_LEGACY = match.Dereferences;
                @this.Cast<IExpression>().DereferencingCount = match.Dereferences;

                match ^= TypeMatch.AutoDereference;
                if (match != TypeMatch.Substitute && match != TypeMatch.Same)
                    throw new NotImplementedException();

            }
            else if (match != TypeMatch.Same && match != TypeMatch.Substitute)
                throw new NotImplementedException();

            return true;
        }

    }

}
