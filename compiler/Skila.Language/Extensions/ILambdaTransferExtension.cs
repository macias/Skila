using Skila.Language.Builders;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Extensions;
using Skila.Language.Flow;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Language.Extensions
{
    public static class ILambdaTransferExtension
    {
        private static TypeDefinition buildTypeOfLambda(ComputationContext ctx,
            FunctionDefinition lambda, IEnumerable<VariableDeclaration> fields)
        {
            if (lambda.Owner != null)
                throw new Exception("Internal error");

            FunctionDefinition cons = FunctionDefinition.CreateInitConstructor(EntityModifier.None,
                fields.Select(it => FunctionParameter.Create(it.Name.Name, it.TypeName)),
                Block.CreateStatement(
                    fields.Select(it
                        => Assignment.CreateStatement(NameReference.Create(NameFactory.ThisVariableName, it.Name.Name),
                        NameReference.Create(it.Name.Name)))
                    ));

            TypeDefinition functor = TypeBuilder.Create(NameDefinition.Create(ctx.AutoName.CreateNew("Closure")))
                .With(fields)
                .With(cons)
                .WithInvoke(lambda);

            return functor;
        }

        private static TypeDefinition buildTypeOfReference(ComputationContext ctx,
            NameReference funcReference, IExpression thisObject)
        {
            if (funcReference.Owner != null)
                throw new Exception("Detach it first.");

            const string this_name = "self";

            FunctionDefinition cons = FunctionDefinition.CreateInitConstructor(EntityModifier.None,
                new[] { FunctionParameter.Create(this_name, thisObject.Evaluation.NameOf) },
                Block.CreateStatement(
                    new[] {
                        Assignment.CreateStatement(NameReference.Create(NameFactory.ThisVariableName, this_name),
                            NameReference.Create(this_name)),
                    }));

            FunctionDefinition function = funcReference.Binding.Match.Target.CastFunction();

            FunctionBuilder invoke = FunctionBuilder.Create(NameFactory.LambdaInvoke, ExpressionReadMode.ReadRequired,
                function.ResultTypeName,
                    Block.CreateStatement(new[] {
                        Return.Create(FunctionCall.Create(
                            NameReference.Create(NameFactory.ThisVariableName, this_name,funcReference.Name),
                                function.Parameters.Select(it => FunctionArgument.Create(NameReference.Create(it.Name.Name))).ToArray()))
                    }))
                    .Parameters(function.Parameters.ToArray());

            VariableDeclaration this_field = VariableDeclaration.CreateStatement(this_name,
                thisObject.Evaluation.NameOf, Undef.Create());

            TypeBuilder closure_builder = TypeBuilder.Create(NameDefinition.Create(ctx.AutoName.CreateNew("Closure")))
                .With(this_field)
                .With(cons)
                .WithInvoke(invoke);
            if (!thisObject.Evaluation.IsImmutableType(ctx))
                closure_builder.Modifier(EntityModifier.Mutable);
            return closure_builder;
        }

        public static void TrapClosure(this ILambdaTransfer node, ComputationContext ctx, ref IExpression source)
        {
            if (source is FunctionDefinition lambda)
            {
                // example scenario
                // f = (x) => x*x
                // f(4)

                // we already have tracked all the variables used inside lambda (which are declared outside, locals are OK), 
                // so all we have to do it is to remove it and put into closure type

                if (!lambda.IsComputed)
                    throw new Exception("Internal error");

                lambda.DetachFrom(node);
                TypeDefinition closure_type = buildTypeOfLambda(ctx, lambda, lambda.LambdaTrap.Fields);
                node.AddClosure(closure_type);

                source = ExpressionFactory.HeapConstructorCall(closure_type.InstanceOf.NameOf,
                    lambda.LambdaTrap.Fields.Select(it => FunctionArgument.Create(NameReference.Create(it.Name.Name))).ToArray());
                source.AttachTo(node);

                lambda.LambdaTrap = null;

                // we have to manually evaluate this expression, because it is child of current node, and child nodes
                // are evaluated before their parents
                closure_type.Evaluated(ctx);
                // todo: this is ugly -- we are breaking into details of separate type
                // since the function is already computed, it won't evaluate meta this parameter
                lambda.MetaThisParameter.Evaluated(ctx);
                source.Evaluated(ctx);
            }
            else if (source is NameReference name_ref
                && name_ref.Binding.Match.Target is FunctionDefinition func
                )
            {
                IExpression this_obj = name_ref.GetContext(func);
                if (this_obj != null)
                {
                    // example scenario
                    // f = my_object.my_square
                    // f(4)

                    // so we have to grab "my_object", make closure around it, and then put it instead of "my_object.my_square"

                    name_ref.DetachFrom(node);
                    name_ref.Prefix.DetachFrom(name_ref);
                    name_ref.Prefix.IsDereferenced = false; // we have to clear it because we will reuse it
                    TypeDefinition closure_type = buildTypeOfReference(ctx, name_ref, this_obj);
                    node.AddClosure(closure_type);

                    source = ExpressionFactory.HeapConstructorCall(closure_type.InstanceOf.NameOf,
                        FunctionArgument.Create(name_ref.Prefix));
                    source.AttachTo(node);

                    closure_type.Evaluated(ctx);

                    closure_type.FunctorInvokeSignature.Cast<FunctionDefinition>().MetaThisParameter.Evaluated(ctx);
                    source.Evaluated(ctx);
                }
            }
        }
    }
}