using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Skila.Language.Entities;
using Skila.Language.Expressions;

namespace Skila.Language.Builders
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class FunctionBuilder : IBuilder<FunctionDefinition>
    {
        public static FunctionBuilder Create(
                   NameDefinition name,
                   IEnumerable<FunctionParameter> parameters,
                   ExpressionReadMode callMode,
                   INameReference result,
                   Block body)
        {
            return new FunctionBuilder(name, parameters, callMode, result, body);
        }
        public static FunctionBuilder Create(
                   string name,                   
                   ExpressionReadMode callMode,
                   INameReference result,
                   Block body)
        {
            return new FunctionBuilder(NameDefinition.Create( name), null, callMode, result, body);
        }
        public static FunctionBuilder CreateDeclaration(
                       NameDefinition name,
                       ExpressionReadMode callMode,
                       INameReference result)
        {
            return new FunctionBuilder(name, null, callMode, result, null);
        }
        public static FunctionBuilder CreateDeclaration(
                       string name,
                       ExpressionReadMode callMode,
                       INameReference result)
        {
            return CreateDeclaration(NameDefinition.Create(name),callMode,result);
        }
        public static FunctionBuilder Create(
                   NameDefinition name,
                   ExpressionReadMode callMode,
                   INameReference result,
                   Block body)
        {
            return new FunctionBuilder(name, null, callMode, result, body);
        }

        private readonly NameDefinition name;
        private EntityModifier modifier;
        private IEnumerable<FunctionParameter> parameters;
        private ExpressionReadMode callMode;
        private INameReference result;
        private Block body;
        private FunctionCall chainCall;
        private IEnumerable<TemplateConstraint> constraints;

        private FunctionDefinition build;

        public FunctionBuilder(
                  NameDefinition name,
                  IEnumerable<FunctionParameter> parameters,
                  ExpressionReadMode callMode,
                  INameReference result,
                  Block body)
        {
            this.name = name;
            this.parameters = parameters;
            this.callMode = callMode;
            this.result = result;
            this.body = body;
        }

        public FunctionBuilder Modifier(EntityModifier modifier)
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
        public FunctionBuilder Constraints(params TemplateConstraint[] constraints)
        {
            if (this.constraints!=null || this.build!=null)
                throw new InvalidOperationException();

            this.constraints = constraints;
            return this;
        }


        public FunctionDefinition Build()
        {
            if (build == null)
                build = FunctionDefinition.CreateFunction(this.modifier ?? EntityModifier.None,
                    this.name,
                    constraints,
                    parameters?? Enumerable.Empty<FunctionParameter>(), callMode, result,
                    chainCall,
                    body);
            return build;
        }
        public static implicit operator FunctionDefinition(FunctionBuilder @this)
        {
            return @this.Build();
        }
    }
}
