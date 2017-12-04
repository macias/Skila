using System.Collections.Generic;
using System.Linq;
using Skila.Language.Builders;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Flow;
using Skila.Language.Extensions;

namespace Skila.Language
{
    public sealed class Environment
    {
        public static Environment Create(IOptions options = null)
        {
            return new Environment(options);
        }

        // at index "i" there is functor which takes "i" in-parameters
        // so for example at index 2 we have Function<T0,T1,R>
        private readonly List<TypeDefinition> functionTypes;
        public IReadOnlyList<TypeDefinition> FunctionTypes => this.functionTypes;
        public TypeDefinition ReferenceType { get; }
        public TypeDefinition PointerType { get; }
        public TypeDefinition VoidType { get; }
        public TypeDefinition UnitType { get; }
        public TypeDefinition BoolType { get; }
        public TypeDefinition ExceptionType { get; }
        public TypeDefinition OptionType { get; }
        public TypeDefinition ChannelType { get; }

        public Namespace Root { get; }
        public Namespace SystemNamespace { get; }
        public Namespace ConcurrencyNamespace { get; }
        public Namespace CollectionsNamespace { get; }

        public TypeDefinition IntType { get; }
        // public TypeDefinition BoolType { get; }
        public TypeDefinition StringType { get; }
        public TypeDefinition DoubleType { get; }
        public TypeDefinition ObjectType { get; }
        public TypeDefinition ISequenceType { get; }
        public TypeDefinition IIterableType { get; }
        public TypeDefinition IEquatableType { get; }

        public FunctionDefinition OptionValueConstructor { get; }
        public FunctionDefinition OptionEmptyConstructor { get; }

        public EvaluationInfo VoidEvaluation { get; }

        public IOptions Options { get; }
        private Environment(IOptions options)
        {
            this.Options = options ?? new Options();

            this.Root = Namespace.Create(NameFactory.RootNamespace);
            this.SystemNamespace = this.Root.AddNode(Namespace.Create(NameFactory.SystemNamespace));
            this.ConcurrencyNamespace = this.SystemNamespace.AddNode(Namespace.Create(NameFactory.ConcurrencyNamespace));
            this.CollectionsNamespace = this.SystemNamespace.AddNode(Namespace.Create(NameFactory.CollectionsNamespace));

            this.ObjectType = this.Root.AddBuilder(TypeBuilder.CreateInterface(
                NameDefinition.Create(NameFactory.ObjectTypeName)));

            this.IntType = this.Root.AddBuilder(TypeBuilder.Create(NameFactory.IntTypeName)
                .Modifier(EntityModifier.Native)
                .Parents(NameFactory.ObjectTypeReference(), NameFactory.EquatableTypeReference())
                .With(FunctionDefinition.CreateInitConstructor(EntityModifier.Native, 
                    null, Block.CreateStatement()))
                .With(FunctionDefinition.CreateInitConstructor(EntityModifier.Native,
                    new[] { FunctionParameter.Create("source", NameFactory.IntTypeReference()) },
                    Block.CreateStatement(new[] {
                       ExpressionFactory.Readout("source"),
                    })))
                .With(FunctionBuilder.Create(NameDefinition.Create(NameFactory.AddOperator),
                    ExpressionReadMode.ReadRequired, NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] {
                       ExpressionFactory.Readout("x"),
                        Return.Create(Undef.Create())
                    }))
                    .Modifier(EntityModifier.Native)
                    .Parameters(FunctionParameter.Create("x", NameFactory.IntTypeReference())))
                .With(FunctionBuilder.Create(NameDefinition.Create(NameFactory.EqualOperator),
                    ExpressionReadMode.ReadRequired, NameFactory.BoolTypeReference(),
                    Block.CreateStatement(new[] {
                       ExpressionFactory.Readout("cmp"),
                        Return.Create(Undef.Create())
                    }))
                    .Modifier(EntityModifier.Native)
                    .Parameters(FunctionParameter.Create("cmp", NameFactory.IntTypeReference())))
                .With(FunctionBuilder.Create(NameDefinition.Create(NameFactory.EqualOperator),
                    ExpressionReadMode.ReadRequired, NameFactory.BoolTypeReference(),
                    Block.CreateStatement(new[] {
                        // let obj = cmp cast? Int
                        VariableDeclaration.CreateStatement("obj",null,ExpressionFactory.Cast(NameReference.Create("cmp"),
                            NameFactory.ReferenceTypeReference( NameFactory.IntTypeReference()))),
                        // if not obj.hasValue then return false
                        ExpressionFactory.IfOptionEmpty(NameReference.Create("obj"),Return.Create(BoolLiteral.CreateFalse())),
                        // return this==obj.value
                        Return.Create(ExpressionFactory.Equal(NameReference.Create(NameFactory.ThisVariableName),
                            ExpressionFactory.OptionValue(NameReference.Create("obj")))),
                    }))
                    .Modifier(EntityModifier.Refines)
                    .Parameters(FunctionParameter.Create("cmp",
                        NameFactory.ReferenceTypeReference(NameFactory.EquatableTypeReference()))))
                );

            this.DoubleType = this.Root.AddBuilder(TypeBuilder.Create(NameFactory.DoubleTypeName)
                .Modifier(EntityModifier.Native)
                .Parents(NameFactory.ObjectTypeReference()));

            // spread functions family
            {
                // no limits
                var decl = VariableDeclaration.CreateStatement("result", NameFactory.ISequenceTypeReference("T"), Undef.Create());
                this.SystemNamespace.AddBuilder(FunctionBuilder.Create(NameDefinition.Create(NameFactory.SpreadFunctionName, "T", VarianceMode.None),
                   new[] { FunctionParameter.Create("coll", NameFactory.IIterableTypeReference("T")) },
                   ExpressionReadMode.ReadRequired,
                   NameFactory.ISequenceTypeReference("T"),
                   Block.CreateStatement(new IExpression[] {
                       ExpressionFactory.Readout("coll"),
                       decl,
                       Return.Create(NameReference.Create("result"))
                   })));
            }
            {
                // with min limit
                var decl = VariableDeclaration.CreateStatement("result", NameFactory.ISequenceTypeReference("T"), Undef.Create());
                this.SystemNamespace.AddBuilder(FunctionBuilder.Create(NameDefinition.Create(NameFactory.SpreadFunctionName, "T", VarianceMode.None),
                   new[] {
                        FunctionParameter.Create("coll", NameFactory.IIterableTypeReference("T")),
                        FunctionParameter.Create("min", NameFactory.IntTypeReference()),
                   },
                   ExpressionReadMode.ReadRequired,
                   NameFactory.ISequenceTypeReference("T"),
                   Block.CreateStatement(new IExpression[] {
                       ExpressionFactory.Readout("coll"),
                       ExpressionFactory.Readout("min"),
                       decl,
                       Return.Create(NameReference.Create("result"))
                   })));
            }
            {
                // with min+max limit
                var decl = VariableDeclaration.CreateStatement("result", NameFactory.ISequenceTypeReference("T"), Undef.Create());
                this.SystemNamespace.AddBuilder(FunctionBuilder.Create(NameDefinition.Create(NameFactory.SpreadFunctionName, "T", VarianceMode.None),
                    new[] {
                        FunctionParameter.Create("coll", NameFactory.IIterableTypeReference("T")),
                        FunctionParameter.Create("min", NameFactory.IntTypeReference()),
                        FunctionParameter.Create("max", NameFactory.IntTypeReference()),
                    },
                    ExpressionReadMode.ReadRequired,
                    NameFactory.ISequenceTypeReference("T"),
                    Block.CreateStatement(new IExpression[] {
                       ExpressionFactory.Readout("coll"),
                       ExpressionFactory.Readout("min"),
                       ExpressionFactory.Readout("max"),
                        decl,
                        Return.Create(NameReference.Create("result"))
                    })));

            }

            this.ISequenceType = this.CollectionsNamespace.AddBuilder(
                TypeBuilder.Create(NameDefinition.Create(NameFactory.ISequenceTypeName, "T", VarianceMode.Out))
                    .Modifier(EntityModifier.Interface)
                    .Parents(NameFactory.ObjectTypeReference()));
            this.IIterableType = this.CollectionsNamespace.AddBuilder(
                TypeBuilder.Create(NameDefinition.Create(NameFactory.IIterableTypeName, "T", VarianceMode.Out))
                    .Modifier(EntityModifier.Interface)
                    .Parents(NameFactory.ObjectTypeReference()));


            this.IEquatableType = this.SystemNamespace.AddBuilder(
                TypeBuilder.Create(NameDefinition.Create(NameFactory.IEquatableTypeName))
                    .Modifier(EntityModifier.Interface)
                    .Parents(NameFactory.ObjectTypeReference())
                    .With(FunctionBuilder.Create(NameFactory.NotEqualOperator, ExpressionReadMode.ReadRequired, NameFactory.BoolTypeReference(),
                        Block.CreateStatement(new[] {
                            Return.Create(ExpressionFactory.Not(
                                ExpressionFactory.Equal(NameFactory.ThisReference(),NameReference.Create("cmp"))))
                        }))
                            .Parameters(FunctionParameter.Create("cmp",
                                NameFactory.ReferenceTypeReference(NameFactory.ShouldBeThisTypeReference(NameFactory.IEquatableTypeName)))))
                    .With(FunctionBuilder.CreateDeclaration(NameFactory.EqualOperator, ExpressionReadMode.ReadRequired,
                        NameFactory.BoolTypeReference())
                            .Parameters(FunctionParameter.Create("cmp",
                                NameFactory.ReferenceTypeReference(NameFactory.ShouldBeThisTypeReference(NameFactory.IEquatableTypeName))))));

            this.functionTypes = new List<TypeDefinition>();

            this.VoidType = Root.AddBuilder(TypeBuilder.Create(NameFactory.VoidTypeName)
                .Modifier(EntityModifier.Native));

            this.BoolType = Root.AddBuilder(TypeBuilder.Create(NameFactory.BoolTypeName)
                .Modifier(EntityModifier.Native)
                .With(FunctionDefinition.CreateInitConstructor(EntityModifier.Native, null, Block.CreateStatement()))
                .With(FunctionDefinition.CreateInitConstructor(EntityModifier.Native,
                    new[] { FunctionParameter.Create("source", NameFactory.BoolTypeReference()) },
                    Block.CreateStatement(new[] {
                       ExpressionFactory.Readout("source"),
                    })))
                .With(FunctionBuilder.Create(NameDefinition.Create(NameFactory.NotOperator),
                    ExpressionReadMode.ReadRequired, NameFactory.BoolTypeReference(),
                    Block.CreateStatement(new[] { Return.Create(Undef.Create()) }))
                    .Modifier(EntityModifier.Native))
                .Parents(NameFactory.ObjectTypeReference()));

            this.UnitType = Root.AddBuilder(TypeBuilder.Create(NameFactory.UnitTypeName)
                .Modifier(EntityModifier.Native)
                .Parents(NameFactory.ObjectTypeReference()));
            // pointer and reference are not of Object type (otherwise we could have common root for String and pointer to Int)
            this.ReferenceType = Root.AddBuilder(TypeBuilder.Create(NameDefinition.Create(NameFactory.ReferenceTypeName, "T", VarianceMode.Out))
                .Modifier(EntityModifier.Native)
                .With(FunctionDefinition.CreateInitConstructor(EntityModifier.Private | EntityModifier.Native, null, Block.CreateStatement()))
                .Slicing(true));
            /*  this.ReferenceType.AddNode(FunctionDefinition.CreateInitConstructor(EntityModifier.Implicit,
                  new[] { FunctionParameter.Create("value", NameReference.Create("T"), Variadic.None, null, isNameRequired: false) },
                  Block.CreateStatement(new IExpression[] { })));
              this.ReferenceType.AddNode(FunctionDefinition.CreateInitConstructor(EntityModifier.Implicit,
                  new[] { FunctionParameter.Create("value", NameFactory.PointerTypeReference(NameReference.Create("T")), Variadic.None, null, isNameRequired: false) },
                  Block.CreateStatement(new IExpression[] { })));*/
            this.PointerType = Root.AddBuilder(TypeBuilder.Create(NameDefinition.Create(NameFactory.PointerTypeName, "T", VarianceMode.Out))
                .Modifier(EntityModifier.Native)
                .With(FunctionDefinition.CreateInitConstructor(EntityModifier.Private | EntityModifier.Native, null, Block.CreateStatement()))
                .Slicing(true));

            /*this.PointerType.AddNode(FunctionDefinition.CreateFunction(EntityModifier.Implicit, NameDefinition.Create(NameFactory.ConvertFunctionName),
                null, ExpressionReadMode.ReadRequired, NameReference.Create("T"),
                Block.CreateStatement(new IExpression[] { Return.Create(Undef.Create()) })));*/

            this.StringType = this.SystemNamespace.AddBuilder(TypeBuilder.Create(NameFactory.StringTypeName)
                .Modifier(EntityModifier.HeapOnly)
                //.Slicing(true)
                .Parents(NameFactory.ObjectTypeReference()));

            this.ChannelType = this.ConcurrencyNamespace.AddNode(createChannelType());

            this.ExceptionType = this.SystemNamespace.AddBuilder(TypeBuilder.Create(NameFactory.ExceptionTypeName)
                .Modifier(EntityModifier.HeapOnly)
                .Parents(NameFactory.ObjectTypeReference()));

            {
                FunctionDefinition empty, value;
                this.OptionType = this.SystemNamespace.AddNode(createOptionType(out empty, out value));
                this.OptionEmptyConstructor = empty;
                this.OptionValueConstructor = value;
            }

            foreach (int param_count in Enumerable.Range(0, 15))
                this.functionTypes.Add(createFunction(Root, param_count));
            this.functionTypes.ForEach(it => Root.AddNode(it));

            this.VoidEvaluation = new EvaluationInfo(this.VoidType.InstanceOf);
        }

        private static TypeDefinition createChannelType()
        {
            return TypeBuilder.Create(NameDefinition.Create(NameFactory.ChannelTypeName,
                    TemplateParametersBuffer.Create().Add("T").Values))
                .Modifier(EntityModifier.HeapOnly | EntityModifier.Native)
                .Constraints(ConstraintBuilder.Create("T").Modifier(EntityModifier.Const))
                // default constructor
                .With(FunctionDefinition.CreateInitConstructor(EntityModifier.Native,
                    null, Block.CreateStatement()))
                .With(FunctionBuilder.Create(NameDefinition.Create(NameFactory.ChannelSend),
                    ExpressionReadMode.ReadRequired, NameFactory.BoolTypeReference(), Block.CreateStatement(new[] {
                        ExpressionFactory.Readout("value"),
                        ExpressionFactory.Readout(NameFactory.ThisVariableName),
                        Return.Create(Undef.Create())
                    }))
                    .Modifier(EntityModifier.Native)
                    .Parameters(FunctionParameter.Create("value", NameReference.Create("T"))))
                .With(FunctionBuilder.Create(NameDefinition.Create(NameFactory.ChannelClose), ExpressionReadMode.CannotBeRead,
                    NameFactory.VoidTypeReference(), Block.CreateStatement(new[] {
                        ExpressionFactory.Readout(NameFactory.ThisVariableName)
                    }))
                    .Modifier(EntityModifier.Native))
                .With(FunctionBuilder.Create(NameDefinition.Create(NameFactory.ChannelReceive),
                    ExpressionReadMode.ReadRequired, NameFactory.OptionTypeReference(NameReference.Create("T")),
                    Block.CreateStatement(new[] {
                        ExpressionFactory.Readout(NameFactory.ThisVariableName),
                        Return.Create(Undef.Create())
                    }))
                    .Modifier(EntityModifier.Native))
                /*.With(FunctionDefinition.CreateFunction(EntityModifier.None, NameDefinition.Create(NameFactory.ChannelTryReceive),
                    null,
                    ExpressionReadMode.ReadRequired, NameFactory.OptionTypeReference(NameReference.Create("T")),
                    Block.CreateStatement(new[] { Return.Create(Undef.Create()) })))*/
                .Parents(NameFactory.ObjectTypeReference())
                .Build();
        }

        private TypeDefinition createOptionType(out FunctionDefinition empty_constructor, out FunctionDefinition value_constructor)
        {
            const string value_field = "value";
            const string has_value_field = "hasValue";

            value_constructor = FunctionDefinition.CreateInitConstructor(EntityModifier.Public,
                                new[] { FunctionParameter.Create("value", NameReference.Create("T"), Variadic.None, null, isNameRequired: false) },
                                    Block.CreateStatement(new[] {
                                        Assignment.CreateStatement(NameReference.Create(NameFactory.ThisVariableName,"value"),
                                            NameReference.Create("value")),
                                        Assignment.CreateStatement(NameReference.Create(NameFactory.ThisVariableName,
                                            has_value_field), BoolLiteral.CreateTrue())
                                    }));

            empty_constructor = FunctionDefinition.CreateInitConstructor(EntityModifier.Public,
                                null,
                                    Block.CreateStatement(new[] {
                                        Assignment.CreateStatement(NameReference.Create(NameFactory.ThisVariableName,has_value_field),
                                            BoolLiteral.CreateFalse())
                                    }));

            return TypeBuilder.Create(NameDefinition.Create(NameFactory.OptionTypeName,
                                TemplateParametersBuffer.Create().Add("T", VarianceMode.Out).Values))
                            .Modifier(EntityModifier.Mutable)
                            .With(Property.Create(NameFactory.OptionHasValue, NameFactory.BoolTypeReference(),
                                null,
                                new[] { Property.CreateProxyGetter(NameFactory.BoolTypeReference(), NameReference.Create(has_value_field)) },
                                null
                            ))
                            .With(Property.Create(NameFactory.OptionValue, NameReference.Create("T"),
                                null,
                                new[] { FunctionBuilder.Create(NameDefinition.Create(NameFactory.PropertyGetter),
                                null, ExpressionReadMode.CannotBeRead, NameReference.Create("T"),
                                Block.CreateStatement(new IExpression[] {
                                    IfBranch.CreateIf(ExpressionFactory.Not( NameReference.Create(has_value_field)),
                                        new[]{ Throw.Create(ExpressionFactory.HeapConstructor(NameFactory.ExceptionTypeReference())) }),
                                    Return.Create(NameReference.Create(value_field))
                                })).Build() },
                                null
                            ))
                            .With(VariableDeclaration.CreateStatement(value_field, NameReference.Create("T"), Undef.Create()))
                            .With(VariableDeclaration.CreateStatement(has_value_field, NameFactory.BoolTypeReference(), Undef.Create()))
                            .With(empty_constructor)
                            .With(value_constructor)
                            .Parents(NameFactory.ObjectTypeReference())
                            .Build();
        }

        private TypeDefinition createFunction(Namespace root, int paramCount)
        {
            var type_parameters = TemplateParametersBuffer.Create();
            var function_parameters = new List<FunctionParameter>();
            foreach (int i in Enumerable.Range(0, paramCount))
            {
                var type_name = $"T{i}";
                type_parameters.Add(type_name, VarianceMode.In);
                function_parameters.Add(FunctionParameter.Create($"item{i}", NameReference.Create(type_name)));
            }

            const string result_type = "R";
            type_parameters.Add(result_type, VarianceMode.Out);

            TypeDefinition function_def = TypeBuilder.CreateInterface(
                NameDefinition.Create(NameFactory.FunctionTypeName, type_parameters.Values))
                .With(FunctionBuilder.CreateDeclaration(NameFactory.LambdaInvoke, ExpressionReadMode.ReadRequired,
                    NameReference.Create(result_type))
                    .Parameters(function_parameters.ToArray()));

            return function_def;
        }

        public bool IsFunctionType(EntityInstance instance)
        {
            if (instance == null)
                return false;

            return functionTypes.Any(it => it == instance.Target);
        }

        internal bool IsOfVoidType(INameReference typeName)
        {
            return typeName != null && IsVoidType(typeName.Evaluation.Components);//.IsSame(this.VoidType.InstanceOf, jokerMatchesAll: false);
        }

        public bool IsVoidType(IEntityInstance typeInstance)
        {
            //            return VoidType.InstanceOf.MatchesTarget(typeInstance, allowSlicing: false);
            return VoidType.InstanceOf.IsSame(typeInstance, jokerMatchesAll: false);
        }
        public bool IsReferenceOfType(IEntityInstance instance)
        {
            return instance.Enumerate().All(it => it.IsOfType(ReferenceType));
        }
        public bool IsPointerOfType(IEntityInstance instance)
        {
            return instance.Enumerate().All(it => it.IsOfType(PointerType));
        }

        public bool Dereferenced(EntityInstance instance, out IEntityInstance result, out bool viaPointer)
        {
            if (IsPointerOfType(instance))
            {
                viaPointer = true;
                result = instance.TemplateArguments.Single();
                return true;
            }
            else if (IsReferenceOfType(instance))
            {
                viaPointer = false;
                result = instance.TemplateArguments.Single();
                return true;
            }
            else
            {
                viaPointer = false;
                result = instance;
                return false;
            }
        }

        /*    public bool IsPointerLikeOfType(IEntityInstance instance,out IEnumerable<IEntityInstance> innerTypes)
            {
                var inner = new List<IEntityInstance>();
                foreach (EntityInstance elem_instance in instance.Enumerate())
                {
                    if (elem_instance.IsOfType(PointerType) || elem_instance.IsOfType(ReferenceType))
                    {
                        inner.Add(elem_instance.TemplateArguments.Single());
                    }
                    else
                    {
                        innerTypes = null;
                        return false;
                    }
                }

                innerTypes = inner;
                return true;
            }*/
        public bool IsPointerLikeOfType(IEntityInstance instance)
        {
            return instance.Enumerate().All(it => it.IsOfType(PointerType) || it.IsOfType(ReferenceType));
        }
        public bool IsOfUnitType(INameReference typeName)
        {
            return typeName.Evaluation.Components.IsSame(this.UnitType.InstanceOf, jokerMatchesAll: false);
        }
    }
}
