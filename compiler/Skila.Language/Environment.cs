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
        //public TypeDefinition EnumType { get; }
        public TypeDefinition StringType { get; }
        public TypeDefinition DoubleType { get; }
        public TypeDefinition ObjectType { get; }

        public TypeDefinition ISequenceType { get; }
        public TypeDefinition ChunkType { get; }
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
                    new[] { FunctionParameter.Create(NameFactory.SourceCopyConstructorParameter, NameFactory.IntTypeReference(), ExpressionReadMode.CannotBeRead) },
                    Block.CreateStatement()))
                .With(FunctionBuilder.Create(NameDefinition.Create(NameFactory.AddOperator),
                    ExpressionReadMode.ReadRequired, NameFactory.IntTypeReference(),
                    Block.CreateStatement())
                    .Modifier(EntityModifier.Native)
                    .Parameters(FunctionParameter.Create("x", NameFactory.IntTypeReference(), ExpressionReadMode.CannotBeRead)))
                .WithEquatableEquals()
                .With(FunctionBuilder.Create(NameDefinition.Create(NameFactory.EqualOperator),
                    ExpressionReadMode.ReadRequired, NameFactory.BoolTypeReference(),
                    Block.CreateStatement())
                    .Modifier(EntityModifier.Native)
                    .Parameters(FunctionParameter.Create("cmp", NameFactory.IntTypeReference(), ExpressionReadMode.CannotBeRead)))
                );

            /*this.EnumType = this.Root.AddBuilder(TypeBuilder.CreateInterface(NameFactory.EnumTypeName,EntityModifier.Native)
                            .Parents(NameFactory.ObjectTypeReference(), NameFactory.EquatableTypeReference()));
                */
            this.DoubleType = this.Root.AddBuilder(TypeBuilder.Create(NameFactory.DoubleTypeName)
                .Modifier(EntityModifier.Native)
                .Parents(NameFactory.ObjectTypeReference())
                .With(FunctionDefinition.CreateInitConstructor(EntityModifier.Native,
                    null, Block.CreateStatement()))
                );

            // spread functions family
            {
                // no limits
                var decl = VariableDeclaration.CreateStatement("result", NameFactory.ISequenceTypeReference("T"), Undef.Create());
                this.SystemNamespace.AddBuilder(FunctionBuilder.Create(NameDefinition.Create(NameFactory.SpreadFunctionName, "T", VarianceMode.None),
                   new[] { FunctionParameter.Create("coll", NameFactory.IIterableTypeReference("T"), ExpressionReadMode.CannotBeRead) },
                   ExpressionReadMode.ReadRequired,
                   NameFactory.ISequenceTypeReference("T"),
                   Block.CreateStatement(new IExpression[] {
                       decl,
                       Return.Create(NameReference.Create("result"))
                   })));
            }
            {
                // with min limit
                var decl = VariableDeclaration.CreateStatement("result", NameFactory.ISequenceTypeReference("T"), Undef.Create());
                this.SystemNamespace.AddBuilder(FunctionBuilder.Create(NameDefinition.Create(NameFactory.SpreadFunctionName, "T", VarianceMode.None),
                   new[] {
                        FunctionParameter.Create("coll", NameFactory.IIterableTypeReference("T"),ExpressionReadMode.CannotBeRead),
                        FunctionParameter.Create("min", NameFactory.IntTypeReference(),ExpressionReadMode.CannotBeRead),
                   },
                   ExpressionReadMode.ReadRequired,
                   NameFactory.ISequenceTypeReference("T"),
                   Block.CreateStatement(new IExpression[] {
                       decl,
                       Return.Create(NameReference.Create("result"))
                   })));
            }
            {
                // with min+max limit
                var decl = VariableDeclaration.CreateStatement("result", NameFactory.ISequenceTypeReference("T"), Undef.Create());
                this.SystemNamespace.AddBuilder(FunctionBuilder.Create(NameDefinition.Create(NameFactory.SpreadFunctionName, "T", VarianceMode.None),
                    new[] {
                        FunctionParameter.Create("coll", NameFactory.IIterableTypeReference("T"),ExpressionReadMode.CannotBeRead),
                        FunctionParameter.Create("min", NameFactory.IntTypeReference(),ExpressionReadMode.CannotBeRead),
                        FunctionParameter.Create("max", NameFactory.IntTypeReference(),ExpressionReadMode.CannotBeRead),
                    },
                    ExpressionReadMode.ReadRequired,
                    NameFactory.ISequenceTypeReference("T"),
                    Block.CreateStatement(new IExpression[] {
                        decl,
                        Return.Create(NameReference.Create("result"))
                    })));

            }

            this.IIterableType = this.CollectionsNamespace.AddBuilder(
                TypeBuilder.CreateInterface(NameDefinition.Create(NameFactory.IIterableTypeName, "T", VarianceMode.Out))
                    .Parents(NameFactory.ObjectTypeReference()));
            this.ISequenceType = this.CollectionsNamespace.AddBuilder(
                TypeBuilder.CreateInterface(NameDefinition.Create(NameFactory.ISequenceTypeName, "T", VarianceMode.Out))
                    .Parents(NameFactory.IIterableTypeReference("T")));

            // todo: in this form this type is broken, size is runtime info, yet we allow the assignment on the stack
            // however it is not yet decided what we will do, maybe this type will be used only internally, 
            // maybe we will introduce two kinds of it, etc.
            this.ChunkType = this.CollectionsNamespace.AddBuilder(
                TypeBuilder.Create(NameDefinition.Create(NameFactory.ChunkTypeName, "T", VarianceMode.Out))
                    .Modifier(EntityModifier.Mutable)
                    .Parents(NameFactory.ISequenceTypeReference("T"))
                    .With(FunctionDefinition.CreateInitConstructor(EntityModifier.Native, new[] {
                            FunctionParameter.Create(NameFactory.ChunkSizeConstructorParameter,NameFactory.IntTypeReference(), 
                                ExpressionReadMode.CannotBeRead)
                        },
                        Block.CreateStatement()))
                    .With(PropertyBuilder.CreateIndexer(NameFactory.ReferenceTypeReference("T"))
                        .With(PropertyMemberBuilder.CreateIndexerSetter(
                                FunctionParameter.Create(NameFactory.ChunkIndexIndexerParameter, NameFactory.IntTypeReference(), ExpressionReadMode.CannotBeRead))
                            .Modifier(EntityModifier.Native))
                        .With(PropertyMemberBuilder.CreateIndexerGetter(
                                FunctionParameter.Create(NameFactory.ChunkIndexIndexerParameter, NameFactory.IntTypeReference(), ExpressionReadMode.CannotBeRead))
                            .Modifier(EntityModifier.Native))));


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
                    new[] { FunctionParameter.Create(NameFactory.SourceCopyConstructorParameter, NameFactory.BoolTypeReference(), ExpressionReadMode.CannotBeRead) },
                    Block.CreateStatement()))
                .With(FunctionBuilder.Create(NameDefinition.Create(NameFactory.NotOperator),
                    ExpressionReadMode.ReadRequired, NameFactory.BoolTypeReference(),
                    Block.CreateStatement())
                    .Modifier(EntityModifier.Native))
                .Parents(NameFactory.ObjectTypeReference()));

            this.UnitType = Root.AddBuilder(TypeBuilder.Create(NameFactory.UnitTypeName)
                .Modifier(EntityModifier.Native)
                .With(VariableDeclaration.CreateStatement(NameFactory.UnitValue, NameFactory.UnitTypeReference(), null,
                    EntityModifier.Static | EntityModifier.Native))
                .With(FunctionDefinition.CreateInitConstructor(EntityModifier.Native | EntityModifier.Private, null, Block.CreateStatement()))
                .Parents(NameFactory.ObjectTypeReference()));

            // pointer and reference are not of Object type (otherwise we could have common root for String and pointer to Int)
            this.ReferenceType = Root.AddBuilder(TypeBuilder.Create(NameDefinition.Create(NameFactory.ReferenceTypeName, "T", VarianceMode.Out))
                .Modifier(EntityModifier.Native)
                .Slicing(true));
            /*  this.ReferenceType.AddNode(FunctionDefinition.CreateInitConstructor(EntityModifier.Implicit,
                  new[] { FunctionParameter.Create("value", NameReference.Create("T"), Variadic.None, null, isNameRequired: false) },
                  Block.CreateStatement(new IExpression[] { })));
              this.ReferenceType.AddNode(FunctionDefinition.CreateInitConstructor(EntityModifier.Implicit,
                  new[] { FunctionParameter.Create("value", NameFactory.PointerTypeReference(NameReference.Create("T")), Variadic.None, null, isNameRequired: false) },
                  Block.CreateStatement(new IExpression[] { })));*/
            this.PointerType = Root.AddBuilder(TypeBuilder.Create(NameDefinition.Create(NameFactory.PointerTypeName, "T", VarianceMode.Out))
                .Modifier(EntityModifier.Native)
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
                    ExpressionReadMode.ReadRequired, NameFactory.BoolTypeReference(), Block.CreateStatement())
                    .Modifier(EntityModifier.Native)
                    .Parameters(FunctionParameter.Create("value", NameReference.Create("T"), ExpressionReadMode.CannotBeRead)))
                .With(FunctionBuilder.Create(NameDefinition.Create(NameFactory.ChannelClose),
                    ExpressionReadMode.OptionalUse, NameFactory.UnitTypeReference(),
                    Block.CreateStatement())
                    .Modifier(EntityModifier.Native))
                .With(FunctionBuilder.Create(NameDefinition.Create(NameFactory.ChannelReceive),
                    ExpressionReadMode.ReadRequired, NameFactory.OptionTypeReference(NameReference.Create("T")),
                    Block.CreateStatement())
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
                                new[] { Property.CreateProxyGetter(NameFactory.BoolTypeReference(),
                                    NameReference.Create(NameFactory.ThisVariableName, has_value_field)) },
                                null
                            ))
                            .With(Property.Create(NameFactory.OptionValue, NameReference.Create("T"),
                                null,
                                new[] { FunctionBuilder.Create(NameDefinition.Create(NameFactory.PropertyGetter),
                                null, ExpressionReadMode.CannotBeRead, NameReference.Create("T"),
                                Block.CreateStatement(new IExpression[] {
                                    IfBranch.CreateIf(ExpressionFactory.Not( NameReference.Create(NameFactory.ThisVariableName, has_value_field)),
                                        new[]{ Throw.Create(ExpressionFactory.HeapConstructor(NameFactory.ExceptionTypeReference())) }),
                                    Return.Create(NameReference.Create(NameFactory.ThisVariableName, value_field))
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
        public bool IsUnitType(IEntityInstance typeInstance)
        {
            return typeInstance.IsSame(this.UnitType.InstanceOf, jokerMatchesAll: false);
        }
        public bool IsIntType(IEntityInstance typeInstance)
        {
            return typeInstance.IsSame(this.IntType.InstanceOf, jokerMatchesAll: false);
        }
        public bool IsOfUnitType(INameReference typeName)
        {
            return IsUnitType(typeName.Evaluation.Components);
        }
        public bool IsReferenceOfType(IEntityInstance instance)
        {
            return instance.Enumerate().All(it => it.IsOfType(ReferenceType));
        }
        public bool IsPointerOfType(IEntityInstance instance)
        {
            return instance.Enumerate().All(it => it.IsOfType(PointerType));
        }

        public bool Dereferenced(IEntityInstance instance, out IEntityInstance result, out bool viaPointer)
        {
            if (IsPointerOfType(instance))
            {
                viaPointer = true;
                result = instance.Cast<EntityInstance>().TemplateArguments.Single();
                return true;
            }
            else if (IsReferenceOfType(instance))
            {
                viaPointer = false;
                result = instance.Cast<EntityInstance>().TemplateArguments.Single();
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
    }
}
