using Skila.Language.Builders;
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
    public static class ILambdaTransferExtension
    {
        private static TypeDefinition buildTypeOfLambda(ComputationContext ctx,
            FunctionDefinition lambda, IEnumerable<VariableDefiniton> fields)
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

            lambda.SetModifier(EntityModifier.Derived | lambda.Modifier);

            TypeDefinition functor = TypeBuilder.Create(NameDefinition.Create(ctx.AutoName.CreateNew("Closure")))
                .With(fields)
                .With(cons)
                .With(lambda)
                .Parents(lambda.CreateFunctionInterface());

            return functor;
        }

        private static TypeDefinition buildTypeOfReference(ComputationContext ctx,
            NameReference funcReference, IExpression thisObject)
        {
            if (funcReference.Owner != null)
                throw new Exception("Detach it first.");

            const string meta_this = "self";

            FunctionDefinition function = funcReference.Binding.Match.Target.CastFunction();

            FunctionDefinition cons;
            NameReference func_field_ref;
            if (thisObject != null)
            {
                cons = FunctionDefinition.CreateInitConstructor(EntityModifier.None,
                    new[] { FunctionParameter.Create(meta_this, thisObject.Evaluation.NameOf) },
                    Block.CreateStatement(
                        new[] {
                        Assignment.CreateStatement(NameReference.Create(NameFactory.ThisVariableName, meta_this),
                            NameReference.Create(meta_this)),
                        }));

                func_field_ref = NameReference.Create(NameFactory.ThisVariableName, meta_this);
            }
            else
            {
                func_field_ref = null;
                cons = null;
            }

            IEnumerable<FunctionParameter> trans_parameters = function.Parameters.Select(pit =>
                           FunctionParameter.Create(pit.Name.Name, pit.TypeName.Evaluated(ctx)
                               .TranslateThrough(funcReference.Binding.Match).NameOf));
            FunctionDefinition invoke = FunctionBuilder.Create(NameFactory.LambdaInvoke, ExpressionReadMode.ReadRequired,
                function.ResultTypeName,
                    Block.CreateStatement(new[] {
                        Return.Create(FunctionCall.Create(
                            NameReference.Create(func_field_ref, funcReference.Name,funcReference.TemplateArguments.ToArray()),
                                function.Parameters.Select(it => FunctionArgument.Create(NameReference.Create(it.Name.Name))).ToArray()))
                    }))
                    .Modifier(EntityModifier.Derived)
                    .Parameters(trans_parameters.ToArray());


            TypeBuilder closure_builder = TypeBuilder.Create(NameDefinition.Create(ctx.AutoName.CreateNew("Closure")))
                .With(invoke)
                .Parents(invoke.CreateFunctionInterface());

            if (thisObject != null)
            {
                VariableDefiniton this_field = VariableDefiniton.CreateStatement(meta_this,
                    thisObject.Evaluation.NameOf, Undef.Create());
                closure_builder
                    .With(cons)
                    .With(this_field);

                if (!thisObject.Evaluation.IsImmutableType(ctx))
                    closure_builder.Modifier(EntityModifier.Mutable);
            }

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
            else if (source is NameReference name_ref && name_ref.Binding.Match.Target is FunctionDefinition func)
            {
                if (func.Name.Arity > 0 && !name_ref.TemplateArguments.Any())
                    ctx.AddError(ErrorCode.SelectingAmbiguousTemplateFunction,name_ref);
                IExpression this_obj = name_ref.GetContext(func);
                // example scenario
                // f = my_object.my_square
                // f(4)

                // so we have to grab "my_object", make closure around it, and then put it instead of "my_object.my_square"

                name_ref.DetachFrom(node);
                if (name_ref.Prefix != null)
                {
                    name_ref.Prefix.DetachFrom(name_ref);
                    name_ref.Prefix.IsDereferenced = false; // we have to clear it because we will reuse it
                }
                TypeDefinition closure_type = buildTypeOfReference(ctx, name_ref, this_obj);
                node.AddClosure(closure_type);

                if (this_obj != null)
                    source = ExpressionFactory.HeapConstructorCall(closure_type.InstanceOf.NameOf,
                        FunctionArgument.Create(name_ref.Prefix));
                else
                    source = ExpressionFactory.HeapConstructorCall(closure_type.InstanceOf.NameOf);
                source.AttachTo(node);

                closure_type.Evaluated(ctx);

                closure_type.InvokeFunctions().First().MetaThisParameter.Evaluated(ctx);
                source.Evaluated(ctx);
            }
        }
    }
}