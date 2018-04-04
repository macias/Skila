using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Extensions;

namespace Skila.Language.Builders
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class FunctionBuilder : IBuilder<FunctionDefinition>
    {
        public static FunctionBuilder CreateLambda(INameReference result,
                 Block body,
                 params FunctionParameter[] parameters)
        {
            FunctionBuilder builder = FunctionBuilder.Create(NameFactory.LambdaInvoke,
                ExpressionReadMode.ReadRequired, result, body);
            if (parameters.Any())
                builder.Parameters(parameters);
            return builder;
        }

        public static FunctionBuilder CreateInitConstructor(Block body, FunctionCall constructorChainCall = null)
        {
            FunctionBuilder builder = Create(NameFactory.InitConstructorName,
                                NameFactory.UnitTypeReference(),
                                body);

            builder.ChainCall(constructorChainCall);
            return builder;
        }

        public static FunctionBuilder Create(
                       string name,
                       IEnumerable<TemplateParameter> nameParameters,
           INameReference result,
           Block body)
        {
            return Create(name, nameParameters, ExpressionReadMode.ReadRequired, result, body);
        }

        public static FunctionBuilder Create(
                 string name,
                 IEnumerable<TemplateParameter> nameParameters,
             ExpressionReadMode callMode,
             INameReference result,
             Block body)
        {
            return new FunctionBuilder(name, nameParameters, null, callMode, result, body);
        }
        public static FunctionBuilder Create(
                       string name,
                       string nameParameter,
                       VarianceMode variance,
                   ExpressionReadMode callMode,
                   INameReference result,
                   Block body)
        {
            return new FunctionBuilder(name, TemplateParametersBuffer.Create(variance, nameParameter).Values,
                null, callMode, result, body);
        }
        public static FunctionBuilder Create(
                       string name,
                       string nameParameter,
                       VarianceMode variance,
                   INameReference result,
                   Block body)
        {
            return Create(name, TemplateParametersBuffer.Create(variance, nameParameter).Values, result, body);
        }
        public static FunctionBuilder Create(
                   string name,
                   ExpressionReadMode callMode,
                   INameReference result,
                   Block body)
        {
            return new FunctionBuilder(name, null, null, callMode, result, body);
        }
        public static FunctionBuilder Create(
                   string name,
                   INameReference result,
                   Block body)
        {
            return new FunctionBuilder(name, null, null, ExpressionReadMode.ReadRequired, result, body);
        }
        public static FunctionBuilder CreateDeclaration(
                       string name,
                       string nameParameter,
                       VarianceMode variance,
                   ExpressionReadMode callMode,
                   INameReference result)
        {
            return new FunctionBuilder(name, TemplateParametersBuffer.Create(variance, nameParameter).Values,
                null, callMode, result, (Block)null);
        }
        public static FunctionBuilder CreateDeclaration(
                       string name,
                       IEnumerable<TemplateParameter> nameParameters,
                       ExpressionReadMode callMode,
                       INameReference result)
        {
            return new FunctionBuilder(name, nameParameters, null, callMode, result, null);
        }
        public static FunctionBuilder CreateDeclaration(
                       string name,
                       ExpressionReadMode callMode,
                       INameReference result)
        {
            return Create(name, callMode, result, (Block)null);
        }
        public static FunctionBuilder CreateDeclaration(
                       string name,
                       INameReference result)
        {
            return CreateDeclaration(name, ExpressionReadMode.ReadRequired, result);
        }

        private readonly string name;
        private readonly IEnumerable<TemplateParameter> nameParameters;
        private EntityModifier modifier;
        private IEnumerable<FunctionParameter> parameters;
        private ExpressionReadMode callMode;
        private INameReference result;
        private Block body;
        private FunctionCall chainCall;
        private IEnumerable<TemplateConstraint> constraints;

        private FunctionDefinition build;
        private IEnumerable<NameReference> includes;
        private IEnumerable<LabelReference> friends;

        private FunctionBuilder(
                  string name,
                  IEnumerable<TemplateParameter> nameParameters,
                  IEnumerable<FunctionParameter> parameters,
                  ExpressionReadMode callMode,
                  INameReference result,
                  Block body)
        {
            this.name = name;
            this.nameParameters = (nameParameters ?? Enumerable.Empty<TemplateParameter>()).StoreReadOnly();
            this.parameters = parameters;
            this.callMode = callMode;
            this.result = result;
            this.body = body;
        }

        public FunctionBuilder SetModifier(EntityModifier modifier)
        {
            if (this.modifier != null || this.build != null)
                throw new InvalidOperationException();

            this.modifier = modifier;
            return this;
        }
        public FunctionBuilder ChainCall(FunctionCall call)
        {
            if (this.chainCall != null || this.build != null)
                throw new InvalidOperationException();

            this.chainCall = call;
            return this;
        }
        public FunctionBuilder Parameters(params FunctionParameter[] parameters)
        {
            if (this.parameters != null || this.build != null)
                throw new InvalidOperationException();

            this.parameters = parameters;
            return this;
        }
        public FunctionBuilder Parameters(IEnumerable<FunctionParameter> parameters)
        {
            return this.Parameters(parameters.ToArray());
        }
        public FunctionBuilder Constraints(params TemplateConstraint[] constraints)
        {
            if (this.constraints != null || this.build != null)
                throw new InvalidOperationException();

            this.constraints = constraints;
            return this;
        }
        public FunctionBuilder Include(params NameReference[] includes)
        {
            if (this.includes != null || this.build != null)
                throw new InvalidOperationException();

            this.includes = includes;
            return this;
        }
        public FunctionBuilder GrantAccess(params LabelReference[] friends)
        {
            if (this.friends != null || this.build != null)
                throw new InvalidOperationException();

            this.friends = friends;
            return this;
        }

        public FunctionDefinition Build()
        {
            if (build == null)
            {
                NameDefinition final_name;
                if (FunctionDefinition.IsValidMutableName(this.name, this.modifier))
                    final_name = NameDefinition.Create(name, nameParameters);
                else
                    final_name = NameDefinition.Create(NameFactory.MutableName(this.name), nameParameters);

                build = FunctionDefinition.CreateFunction(
                    this.modifier ?? EntityModifier.None,
                    final_name,
                    constraints,
                    parameters ?? Enumerable.Empty<FunctionParameter>(), callMode, result,
                    chainCall,
                    body,
                    includes,
                    friends);
            }
            return build;
        }
        public static implicit operator FunctionDefinition(FunctionBuilder @this)
        {
            return @this.Build();
        }

    }
}
