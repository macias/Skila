using System.Collections.Generic;
using System.Linq;
using Skila.Language.Builders;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Flow;
using Skila.Language.Extensions;
using NaiveLanguageTools.Common;

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
        public FunctionDefinition IntParseStringFunction { get; }

        //public TypeDefinition EnumType { get; }

        public TypeDefinition StringType { get; }

        public TypeDefinition OrderingType { get; }
        public VariableDeclaration OrderingLess { get; }
        public VariableDeclaration OrderingEqual { get; }
        public VariableDeclaration OrderingGreater { get; }
        public TypeDefinition ComparableType { get; }
        public TypeDefinition DoubleType { get; }
        public TypeDefinition ObjectType { get; }

        public TypeDefinition ChunkType { get; }
        public FunctionDefinition ChunkCount { get; }
        public FunctionDefinition ChunkAtGet { get; }
        public FunctionDefinition ChunkAtSet { get; }

        public TypeDefinition ISequenceType { get; }
        public TypeDefinition IIterableType { get; }
        public TypeDefinition IIteratorType { get; }
        public TypeDefinition IndexIteratorType { get; }
        public TypeDefinition IIndexableType { get; }

        // starting with Tuple'2
        private readonly List<TypeDefinition> tupleTypes;
        public IReadOnlyList<TypeDefinition> TupleTypes => this.tupleTypes;
        private readonly List<TypeDefinition> iTupleTypes;
        public IReadOnlyList<TypeDefinition> ITupleTypes => this.iTupleTypes;

        public TypeDefinition IEquatableType { get; }

        public TypeDefinition DateType { get; }
        public FunctionDefinition DateDayOfWeekGetter { get; }
        public VariableDeclaration DateYearField { get; }
        public VariableDeclaration DateMonthField { get; }
        public VariableDeclaration DateDayField { get; }
        public TypeDefinition DayOfWeek { get; }

        public FunctionDefinition OptionValueConstructor { get; }
        public FunctionDefinition OptionEmptyConstructor { get; }

        public EvaluationInfo UnitEvaluation { get; }

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

            {
                this.IntType = this.Root.AddNode(createIntType(out FunctionDefinition parse_string));
                this.IntParseStringFunction = parse_string;
            }

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
                // todo: take iterables as input and convert them to sequence (all spreads)

                // no limits
                this.SystemNamespace.AddBuilder(FunctionBuilder.Create(
                    NameDefinition.Create(NameFactory.SpreadFunctionName, "T", VarianceMode.None),
                   new[] { FunctionParameter.Create("coll", NameFactory.ReferenceTypeReference(NameFactory.ISequenceTypeReference("T", overrideMutability: MutabilityFlag.ForceMutable))) },
                   ExpressionReadMode.ReadRequired,
                   NameFactory.ReferenceTypeReference(NameFactory.ISequenceTypeReference("T", overrideMutability: MutabilityFlag.ForceMutable)),
                   Block.CreateStatement(new IExpression[] {
                       Return.Create(NameReference.Create("coll"))
                   })));
            }
            {
                // with min limit
                this.SystemNamespace.AddBuilder(FunctionBuilder.Create(
                        NameDefinition.Create(NameFactory.SpreadFunctionName, "T", VarianceMode.None),
                   new[] {
                        FunctionParameter.Create("coll", NameFactory.ReferenceTypeReference(NameFactory.ISequenceTypeReference("T",overrideMutability:MutabilityFlag.ForceMutable))),
                        FunctionParameter.Create("min", NameFactory.IntTypeReference()),
                   },
                   ExpressionReadMode.ReadRequired,
                   NameFactory.ReferenceTypeReference(NameFactory.ISequenceTypeReference("T", overrideMutability: MutabilityFlag.ForceMutable)),
                   Block.CreateStatement(new IExpression[] {
                       IfBranch.CreateIf(ExpressionFactory.IsLess(FunctionCall.Create(NameReference.Create("coll",NameFactory.IterableCount)),
                            NameReference.Create("min")),new[]{ ExpressionFactory.GenericThrow() }),
                       Return.Create(NameReference.Create("coll"))
                   })));
            }
            {
                // with min+max limit
                this.SystemNamespace.AddBuilder(FunctionBuilder.Create(NameDefinition.Create(
                        NameFactory.SpreadFunctionName, "T", VarianceMode.None),
                    new[] {
                        FunctionParameter.Create("coll", NameFactory.ReferenceTypeReference(  NameFactory.ISequenceTypeReference("T",overrideMutability:MutabilityFlag.ForceMutable))),
                        FunctionParameter.Create("min", NameFactory.IntTypeReference()),
                        FunctionParameter.Create("max", NameFactory.IntTypeReference()),
                    },
                    ExpressionReadMode.ReadRequired,
                    NameFactory.ReferenceTypeReference(NameFactory.ISequenceTypeReference("T", overrideMutability: MutabilityFlag.ForceMutable)),
                    Block.CreateStatement(new IExpression[] {
                        VariableDeclaration.CreateStatement("count",null,
                            FunctionCall.Create(NameReference.Create("coll",NameFactory.IterableCount))),
                       IfBranch.CreateIf(ExpressionFactory.IsLess(NameReference.Create("count"),
                            NameReference.Create("min")),new[]{ ExpressionFactory.GenericThrow() }),
                       IfBranch.CreateIf(ExpressionFactory.IsGreater(NameReference.Create("count"),
                            NameReference.Create("max")),new[]{ ExpressionFactory.GenericThrow() }),
                       Return.Create(NameReference.Create("coll"))
                    })));
            }

            this.IIterableType = this.CollectionsNamespace.AddNode(createIIterable());

            this.IIteratorType = this.CollectionsNamespace.AddNode(createIterator());

            this.IndexIteratorType = this.CollectionsNamespace.AddNode(createIndexIterator());

            this.ISequenceType = this.CollectionsNamespace.AddBuilder(
                TypeBuilder.CreateInterface(NameDefinition.Create(NameFactory.ISequenceTypeName, "SQT", VarianceMode.Out))
                    .Parents(NameFactory.IIterableTypeReference("SQT"))

                    .With(PropertyBuilder.Create(NameFactory.IterableCount, NameFactory.IntTypeReference())
                        .WithGetter(body: null, modifier: EntityModifier.Override)));

            this.IIndexableType = this.CollectionsNamespace.AddNode(createIIndexable());

            {
                this.ChunkType = this.CollectionsNamespace.AddNode(createChunk(out IMember chunk_count,
                    out IMember chunk_at_get, out IMember chunk_at_set));

                this.ChunkAtGet = chunk_at_get.Cast<FunctionDefinition>();
                this.ChunkAtSet = chunk_at_set.Cast<FunctionDefinition>();
                this.ChunkCount = chunk_count.Cast<FunctionDefinition>();
            }

            this.IEquatableType = this.SystemNamespace.AddBuilder(
                TypeBuilder.Create(NameDefinition.Create(NameFactory.IEquatableTypeName))
                    .Modifier(EntityModifier.Interface)
                    .Parents(NameFactory.ObjectTypeReference())
                    .With(FunctionBuilder.Create(NameFactory.NotEqualOperator, ExpressionReadMode.ReadRequired, NameFactory.BoolTypeReference(),
                        Block.CreateStatement(new[] {
                            Return.Create(ExpressionFactory.Not(
                                ExpressionFactory.IsEqual(NameFactory.ThisReference(),NameReference.Create("cmp"))))
                        }))
                            .Parameters(FunctionParameter.Create("cmp",
                                NameFactory.ReferenceTypeReference(NameFactory.ShouldBeThisTypeReference(NameFactory.IEquatableTypeName)))))
                    .With(FunctionBuilder.CreateDeclaration(NameFactory.EqualOperator, NameFactory.BoolTypeReference())
                        .Modifier(EntityModifier.Pinned)
                            .Parameters(FunctionParameter.Create("cmp",
                                NameFactory.ReferenceTypeReference(NameFactory.ShouldBeThisTypeReference(NameFactory.IEquatableTypeName))))));

            this.DayOfWeek = this.SystemNamespace.AddBuilder(TypeBuilder.CreateEnum(NameFactory.DayOfWeekTypeName)
                .With(EnumCaseBuilder.Create(NameFactory.SundayDayOfWeekTypeName,
                    NameFactory.MondayDayOfWeekTypeName,
                    NameFactory.TuesdayDayOfWeekTypeName,
                    NameFactory.WednesdayDayOfWeekTypeName,
                    NameFactory.ThursdayDayOfWeekTypeName,
                    NameFactory.FridayDayOfWeekTypeName,
                    NameFactory.SaturdayDayOfWeekTypeName
                    )));

            {
                this.DateType = this.SystemNamespace.AddBuilder(TypeBuilder.Create(NameFactory.DateTypeName)
                    .Modifier(EntityModifier.Mutable)
                    .With(PropertyBuilder.CreateAutoFull("year", NameFactory.IntTypeReference(), out PropertyMembers year))
                    .With(PropertyBuilder.CreateAutoFull("month", NameFactory.IntTypeReference(), out PropertyMembers month,
                        IntLiteral.Create("1")))
                    .With(PropertyBuilder.CreateAutoFull("day", NameFactory.IntTypeReference(), out PropertyMembers day,
                        IntLiteral.Create("1")))
                    .With(ExpressionFactory.BasicConstructor(new[] { "year", "month", "day" },
                        new[] { NameFactory.IntTypeReference(), NameFactory.IntTypeReference(), NameFactory.IntTypeReference() }))
                    .With(PropertyBuilder.Create(NameFactory.DateDayOfWeekProperty, NameFactory.DayOfWeekTypeReference())
                        .WithGetter(ExpressionFactory.BodyReturnUndef(), out FunctionDefinition day_of_week_getter, EntityModifier.Native))
                    );

                this.DateDayOfWeekGetter = day_of_week_getter;
                this.DateYearField = year.Field;
                this.DateMonthField = month.Field;
                this.DateDayField = day.Field;
            }

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

            this.UnitEvaluation = new EvaluationInfo(this.UnitType.InstanceOf);

            // pointer and reference are not of Object type (otherwise we could have common root for String and pointer to Int)
            this.ReferenceType = Root.AddBuilder(TypeBuilder.Create(NameDefinition.Create(NameFactory.ReferenceTypeName, "RFT", VarianceMode.Out))
                .Modifier(EntityModifier.Native)
                .Slicing(true));
            /*  this.ReferenceType.AddNode(FunctionDefinition.CreateInitConstructor(EntityModifier.Implicit,
                  new[] { FunctionParameter.Create("value", NameReference.Create("T"), Variadic.None, null, isNameRequired: false) },
                  Block.CreateStatement(new IExpression[] { })));
              this.ReferenceType.AddNode(FunctionDefinition.CreateInitConstructor(EntityModifier.Implicit,
                  new[] { FunctionParameter.Create("value", NameFactory.PointerTypeReference(NameReference.Create("T")), Variadic.None, null, isNameRequired: false) },
                  Block.CreateStatement(new IExpression[] { })));*/
            this.PointerType = Root.AddBuilder(TypeBuilder.Create(NameDefinition.Create(NameFactory.PointerTypeName, "PTT", VarianceMode.Out))
                .Modifier(EntityModifier.Native)
                .Slicing(true));

            /*this.PointerType.AddNode(FunctionDefinition.CreateFunction(EntityModifier.Implicit, NameDefinition.Create(NameFactory.ConvertFunctionName),
                null, ExpressionReadMode.ReadRequired, NameReference.Create("T"),
                Block.CreateStatement(new IExpression[] { Return.Create(Undef.Create()) })));*/


            this.StringType = this.SystemNamespace.AddBuilder(TypeBuilder.Create(NameFactory.StringTypeName)
                .Modifier(EntityModifier.HeapOnly | EntityModifier.Native | EntityModifier.Mutable)
                .Parents(NameFactory.ObjectTypeReference()));


            this.OrderingType = this.SystemNamespace.AddBuilder(TypeBuilder.CreateEnum(NameFactory.OrderingTypeName)
                .With(EnumCaseBuilder.Create(NameFactory.OrderingLess, NameFactory.OrderingEqual, NameFactory.OrderingGreater)));

            this.OrderingLess = this.OrderingType.NestedFields.Single(it => it.Name.Name == NameFactory.OrderingLess);
            this.OrderingEqual = this.OrderingType.NestedFields.Single(it => it.Name.Name == NameFactory.OrderingEqual);
            this.OrderingGreater = this.OrderingType.NestedFields.Single(it => it.Name.Name == NameFactory.OrderingGreater);

            this.ComparableType = this.SystemNamespace.AddNode(createComparableType());


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

            this.functionTypes = new List<TypeDefinition>();
            foreach (int param_count in Enumerable.Range(0, 15))
                this.functionTypes.Add(createFunction(param_count));
            this.functionTypes.ForEach(it => this.Root.AddNode(it));

            {

                TypeBuilder factory_builder = TypeBuilder.Create(NameFactory.TupleTypeName)
                    .Modifier(EntityModifier.Static);
                this.tupleTypes = new List<TypeDefinition>();
                this.iTupleTypes = new List<TypeDefinition>();
                foreach (int count in Enumerable.Range(2, 15))
                {
                    this.tupleTypes.Add(createTuple(count, out FunctionDefinition factory));
                    this.iTupleTypes.Add(createITuple(count));

                    factory_builder.With(factory);
                }
                this.tupleTypes.ForEach(it => this.CollectionsNamespace.AddNode(it));
                this.iTupleTypes.ForEach(it => this.CollectionsNamespace.AddNode(it));
                this.CollectionsNamespace.AddBuilder(factory_builder);
            }
        }

        private static TypeDefinition createIIterable()
        {
            const string elem_type = "ITT";

            return TypeBuilder.CreateInterface(NameDefinition.Create(NameFactory.IIterableTypeName, elem_type, VarianceMode.Out))
                                .Parents(NameFactory.ObjectTypeReference())
                                .With(FunctionBuilder.CreateDeclaration(NameFactory.PropertyIndexerName, ExpressionReadMode.ReadRequired,
                                    NameFactory.ReferenceTypeReference(NameReference.Create(elem_type)))
                                    .Parameters(FunctionParameter.Create(NameFactory.IndexIndexerParameter, NameFactory.IntTypeReference())))

                .With(FunctionBuilder.CreateDeclaration(NameFactory.IterableGetIterator,
                    NameFactory.ReferenceTypeReference(NameFactory.IIteratorTypeReference(elem_type))))

                                .With(FunctionBuilder.CreateDeclaration(NameFactory.IterableCount, ExpressionReadMode.ReadRequired,
                                    NameFactory.IntTypeReference()));
        }

        private static TypeDefinition createChunk(out IMember countGetter, out IMember atGetter, out IMember atSetter)
        {
            // todo: in this form this type is broken, size is runtime info, yet we allow the assignment on the stack
            // however it is not yet decided what we will do, maybe this type will be used only internally, 
            // maybe we will introduce two kinds of it, etc.

            const string elem_type = "CHT";

            return TypeBuilder.Create(NameDefinition.Create(NameFactory.ChunkTypeName, elem_type, VarianceMode.None))
                    .Modifier(EntityModifier.Mutable)
                    .Parents(NameFactory.IIndexableTypeReference(elem_type))
                    .With(FunctionDefinition.CreateInitConstructor(EntityModifier.Native, new[] {
                            FunctionParameter.Create(NameFactory.ChunkSizeConstructorParameter,NameFactory.IntTypeReference(),
                                ExpressionReadMode.CannotBeRead)
                        },
                        Block.CreateStatement()))

                .With(PropertyBuilder.Create(NameFactory.IterableCount, NameFactory.IntTypeReference())
                        .With(PropertyMemberBuilder.CreateGetter(Block.CreateStatement())
                            .Modifier(EntityModifier.Native | EntityModifier.Override), out countGetter))

                     .With(PropertyBuilder.CreateIndexer(NameFactory.ReferenceTypeReference(elem_type))
                        .Parameters(FunctionParameter.Create(NameFactory.IndexIndexerParameter, NameFactory.IntTypeReference(),
                            ExpressionReadMode.CannotBeRead))
                        .With(PropertyMemberBuilder.CreateIndexerSetter(Block.CreateStatement())
                            .Modifier(EntityModifier.Native), out atSetter)
                        .With(PropertyMemberBuilder.CreateIndexerGetter(Block.CreateStatement())
                            .Modifier(EntityModifier.Native | EntityModifier.Override), out atGetter));
        }

        private static TypeDefinition createIntType(out FunctionDefinition parseString)
        {
            parseString = FunctionBuilder.Create(NameFactory.ParseFunctionName, NameFactory.OptionTypeReference(NameFactory.IntTypeReference()),
                    ExpressionFactory.BodyReturnUndef())
                    .Parameters(FunctionParameter.Create("s", NameFactory.StringTypeReference(), ExpressionReadMode.CannotBeRead))
                    .Modifier(EntityModifier.Native | EntityModifier.Static);

            return TypeBuilder.Create(NameFactory.IntTypeName)
                .Modifier(EntityModifier.Native)
                .Parents(NameFactory.ObjectTypeReference(), NameFactory.ComparableTypeReference())
                .With(parseString)
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
                .With(FunctionBuilder.Create(NameDefinition.Create(NameFactory.SubOperator),
                    ExpressionReadMode.ReadRequired, NameFactory.IntTypeReference(),
                    Block.CreateStatement())
                    .Modifier(EntityModifier.Native)
                    .Parameters(FunctionParameter.Create("x", NameFactory.IntTypeReference(), ExpressionReadMode.CannotBeRead)))

                .WithComparableCompare()
                .With(FunctionBuilder.Create(NameDefinition.Create(NameFactory.ComparableCompare),
                    ExpressionReadMode.ReadRequired, NameFactory.OrderingTypeReference(),
                    Block.CreateStatement())
                    .Modifier(EntityModifier.Native)
                    .Parameters(FunctionParameter.Create("cmp", NameFactory.IntTypeReference(), ExpressionReadMode.CannotBeRead)))

                .With(FunctionBuilder.Create(NameDefinition.Create(NameFactory.EqualOperator),
                    ExpressionReadMode.ReadRequired, NameFactory.BoolTypeReference(),
                    Block.CreateStatement())
                    .Modifier(EntityModifier.Native)
                    .Parameters(FunctionParameter.Create("cmp", NameFactory.IntTypeReference(), ExpressionReadMode.CannotBeRead)))
                ;
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
                                new[] { Property.CreateGetter(NameFactory.BoolTypeReference(),
                                Block.CreateStatement(Return.Create(
                                    NameReference.Create(NameFactory.ThisVariableName, has_value_field)))) },
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

        private static TypeDefinition createComparableType()
        {

            /*
             *   interface Comparable refines Equatable
    def compare(cmp Self) Ordering;

    final refines def ==(cmp Self) Bool => this.compare(cmp)==Ordering.equal;

    def <(cmp Self) Bool => this.compare(cmp)==Ordering.less;
    def <=(cmp Self) Bool => this.compare(cmp) != Ordering.greater;
    def >(cmp Self) Bool => this.compare(cmp)==Ordering.greater;
    def >=(cmp Self) Bool => this.compare(cmp) != Ordering.less;

    def min(cmp `Self) `Self => this<cmp ? this : cmp;
    def max(cmp `Self) `Self => this>cmp ? this : cmp;
  end

             */
            var eq = FunctionBuilder.Create(NameFactory.EqualOperator, NameFactory.BoolTypeReference(),
                Block.CreateStatement(
                    Return.Create(ExpressionFactory.IsEqual(FunctionCall.Create(NameReference.CreateThised(NameFactory.ComparableCompare),
                        NameReference.Create("cmp")), NameFactory.OrderingEqualReference()))))
                .Parameters(FunctionParameter.Create("cmp", NameFactory.ReferenceTypeReference(NameFactory.ComparableTypeReference())));

            var gt = FunctionBuilder.Create(NameFactory.GreaterOperator, NameFactory.BoolTypeReference(),
                Block.CreateStatement(
                    Return.Create(ExpressionFactory.IsEqual(FunctionCall.Create(NameReference.CreateThised(NameFactory.ComparableCompare),
                        NameReference.Create("cmp")), NameFactory.OrderingGreaterReference()))))
                .Parameters(FunctionParameter.Create("cmp", NameFactory.ReferenceTypeReference(NameFactory.ComparableTypeReference())));
            var lt = FunctionBuilder.Create(NameFactory.LessOperator, NameFactory.BoolTypeReference(),
                Block.CreateStatement(
                    Return.Create(ExpressionFactory.IsEqual(FunctionCall.Create(NameReference.CreateThised(NameFactory.ComparableCompare),
                        NameReference.Create("cmp")), NameFactory.OrderingLessReference()))))
                .Parameters(FunctionParameter.Create("cmp", NameFactory.ReferenceTypeReference(NameFactory.ComparableTypeReference())));
            var ge = FunctionBuilder.Create(NameFactory.GreaterEqualOperator, NameFactory.BoolTypeReference(),
                Block.CreateStatement(
                    Return.Create(ExpressionFactory.IsNotEqual(FunctionCall.Create(NameReference.CreateThised(NameFactory.ComparableCompare),
                        NameReference.Create("cmp")), NameFactory.OrderingLessReference()))))
                .Parameters(FunctionParameter.Create("cmp", NameFactory.ReferenceTypeReference(NameFactory.ComparableTypeReference())));
            var le = FunctionBuilder.Create(NameFactory.LessEqualOperator, NameFactory.BoolTypeReference(),
                Block.CreateStatement(
                    Return.Create(ExpressionFactory.IsNotEqual(FunctionCall.Create(NameReference.CreateThised(NameFactory.ComparableCompare),
                        NameReference.Create("cmp")), NameFactory.OrderingGreaterReference()))))
                .Parameters(FunctionParameter.Create("cmp", NameFactory.ReferenceTypeReference(NameFactory.ComparableTypeReference())));

            return TypeBuilder.CreateInterface(NameFactory.ComparableTypeName)
                .Parents(NameFactory.EquatableTypeReference())
                .Modifier(EntityModifier.Base)
                .With(FunctionBuilder.CreateDeclaration(NameFactory.ComparableCompare, NameFactory.OrderingTypeReference())
                    .Modifier(EntityModifier.Pinned)
                    .Parameters(FunctionParameter.Create("cmp",
                        NameFactory.ReferenceTypeReference(NameFactory.ShouldBeThisTypeReference(NameFactory.ComparableTypeName)))))
                .WithEquatableEquals(EntityModifier.Final)
                .With(eq)
                .With(gt)
                .With(lt)
                .With(ge)
                .With(le);

        }

        private static TypeDefinition createFunction(int paramCount)
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

        private static TypeDefinition createTuple(int count, out FunctionDefinition factory)
        {
            var type_parameters = new List<string>();
            var properties = new List<Property>();
            for (int i = 0; i < count; ++i)
            {
                var type_name = $"T{i}";
                type_parameters.Add(type_name);
                properties.Add(PropertyBuilder.Create(NameFactory.TupleItemName(i), NameReference.Create(type_name))
                    .WithAutoField(Undef.Create(), EntityModifier.Reassignable)
                    .WithAutoSetter()
                    .WithAutoGetter(EntityModifier.Override));
            }

            IfBranch item_selector = IfBranch.CreateElse(new[] { ExpressionFactory.GenericThrow() });

            for (int i = count - 1; i >= 0; --i)
            {
                item_selector = IfBranch.CreateIf(ExpressionFactory.IsEqual(NameReference.Create(NameFactory.IndexIndexerParameter),
                    IntLiteral.Create($"{i}")), new[] { Return.Create(NameReference.CreateThised(NameFactory.TupleItemName(i))) },
                    item_selector);
            }

            const string base_type_name = "C";

            TypeBuilder builder = TypeBuilder.Create(
                NameDefinition.Create(NameFactory.TupleTypeName,
                    TemplateParametersBuffer.Create(type_parameters.ToArray()).Add(base_type_name, VarianceMode.Out).Values))
                .Modifier(EntityModifier.Mutable)
                .Parents(NameFactory.ITupleTypeReference(type_parameters.Concat(base_type_name).Select(it => NameReference.Create(it)).ToArray()))
                .With(ExpressionFactory.BasicConstructor(properties.Select(it => it.Name.Name).ToArray(),
                     type_parameters.Select(it => NameReference.Create(it)).ToArray()))

                .With(properties)

                .With(PropertyBuilder.CreateIndexer(NameFactory.ReferenceTypeReference(base_type_name))
                    .Parameters(FunctionParameter.Create(NameFactory.IndexIndexerParameter, NameFactory.IntTypeReference()))
                    .With(PropertyMemberBuilder.CreateIndexerGetter(Block.CreateStatement(item_selector))
                        .Modifier(EntityModifier.Override)))

                .Constraints(TemplateConstraint.Create(base_type_name, null, null, null, type_parameters.Select(it => NameReference.Create(it))));

            // creating static factory method for the above tuple

            NameReference func_result_typename = NameFactory.TupleTypeReference(type_parameters.Concat(base_type_name)
                .Select(it => NameReference.Create(it)).ToArray());
            factory = FunctionBuilder.Create(NameDefinition.Create(NameFactory.CreateFunctionName,
                TemplateParametersBuffer.Create(type_parameters.Concat(base_type_name).ToArray()).Values),
                    func_result_typename,
                    Block.CreateStatement(Return.Create(
                        ExpressionFactory.StackConstructor(func_result_typename,
                            Enumerable.Range(0, count).Select(i => NameReference.Create(NameFactory.TupleItemName(i))).ToArray()))))
                    .Modifier(EntityModifier.Static)
                    .Parameters(Enumerable.Range(0, count).Select(i => FunctionParameter.Create(NameFactory.TupleItemName(i), NameReference.Create(type_parameters[i]))).ToArray())
                .Constraints(TemplateConstraint.Create(base_type_name, null, null, null,
                    type_parameters.Select(it => NameReference.Create(it))));

            return builder;
        }

        private static TypeDefinition createIterator()
        {
            const string elem_type = "IRT";
            return TypeBuilder.CreateInterface(NameDefinition.Create(NameFactory.IIteratorTypeName, elem_type, VarianceMode.Out))
                            .Modifier(EntityModifier.Mutable)

                            .With(FunctionBuilder.CreateDeclaration(NameFactory.IteratorNext, ExpressionReadMode.ReadRequired,
                                    NameFactory.BoolTypeReference())
                                    .Modifier(EntityModifier.Mutable))

                            .With(FunctionBuilder.CreateDeclaration(NameFactory.IteratorGet, ExpressionReadMode.ReadRequired,
                                    NameFactory.ReferenceTypeReference(NameReference.Create(elem_type))));
        }

        private static TypeDefinition createIndexIterator()
        {
            const string elem_type_name = "XIT";
            const string coll_name = "coll";
            const string index_name = "index";

            TypeBuilder builder = TypeBuilder.Create(
                NameDefinition.Create(NameFactory.IndexIteratorTypeName,
                    TemplateParametersBuffer.Create(VarianceMode.Out, elem_type_name).Values))

                .Modifier(EntityModifier.Mutable | EntityModifier.AssociatedReference)
                .Parents(NameFactory.IIteratorTypeReference(elem_type_name))

                .With(VariableDeclaration.CreateStatement(index_name,
                    NameFactory.IntTypeReference(), IntLiteral.Create("-1"), EntityModifier.Reassignable))
                .With(VariableDeclaration.CreateStatement(coll_name,
                    NameFactory.ReferenceTypeReference(NameFactory.IIndexableTypeReference(elem_type_name,
                        overrideMutability: MutabilityFlag.Neutral)),
                    Undef.Create()))

                .With(ExpressionFactory.BasicConstructor(new[] { coll_name },
                    new[] { NameFactory.ReferenceTypeReference(NameFactory.IIndexableTypeReference(elem_type_name,
                        overrideMutability: MutabilityFlag.Neutral)) }))

                 .With(FunctionBuilder.Create(NameFactory.IteratorNext, NameFactory.BoolTypeReference(),
                    Block.CreateStatement(
                        Assignment.CreateStatement(NameReference.CreateThised(index_name),
                            ExpressionFactory.Add(NameReference.CreateThised(index_name), IntLiteral.Create("1"))),
                        Return.Create(ExpressionFactory.IsLess(NameReference.CreateThised(index_name),
                            NameReference.CreateThised(coll_name, NameFactory.IterableCount)))
                        ))
                      .Modifier(EntityModifier.Mutable | EntityModifier.Override))

                  .With(FunctionBuilder.Create(NameFactory.IteratorGet,
                          NameFactory.ReferenceTypeReference(NameReference.Create(elem_type_name)),
                          Block.CreateStatement(
                        IfBranch.CreateIf(ExpressionFactory.IsGreaterEqual(NameReference.CreateThised(index_name),
                            NameReference.CreateThised(coll_name, NameFactory.IterableCount)),
                            new[] { ExpressionFactory.GenericThrow() }),
                        Return.Create(FunctionCall.Indexer(NameReference.CreateThised(coll_name), NameReference.CreateThised(index_name)))
                              ))
                      .Modifier(EntityModifier.Override))
            ;

            return builder;

        }
        private static TypeDefinition createIIndexable()
        {
            const string elem_type_name = "IXT";

            TypeBuilder builder = TypeBuilder.CreateInterface(
                NameDefinition.Create(NameFactory.IIndexableTypeName,
                    TemplateParametersBuffer.Create(VarianceMode.Out, elem_type_name).Values))

                .Parents(NameFactory.ISequenceTypeReference(NameReference.Create(elem_type_name)))

                .With(FunctionBuilder.Create(NameFactory.IterableGetIterator,
                    NameFactory.ReferenceTypeReference(NameFactory.IIteratorTypeReference(elem_type_name)),
                    Block.CreateStatement(
                        Return.Create(ExpressionFactory.HeapConstructor(NameFactory.IndexIteratorTypeReference(elem_type_name),
                            NameFactory.ThisReference()))))
                    .Modifier(EntityModifier.Override))

                .With(PropertyBuilder.CreateIndexer(NameFactory.ReferenceTypeReference(elem_type_name))
                    .Parameters(FunctionParameter.Create(NameFactory.IndexIndexerParameter, NameFactory.IntTypeReference()))
                    .With(PropertyMemberBuilder.CreateIndexerGetter(body: null)
                        .Modifier(EntityModifier.Override)));

            return builder;
        }

        private static TypeDefinition createITuple(int count)
        {
            var type_parameters = new List<string>();
            var properties = new List<Property>();
            foreach (int i in Enumerable.Range(0, count))
            {
                var type_name = $"T{i}";
                type_parameters.Add(type_name);
                properties.Add(PropertyBuilder.Create(NameFactory.TupleItemName(i), NameReference.Create(type_name))
                    .WithGetter(body: null).Build());
            }

            const string base_type_name = "C";

            TypeBuilder builder = TypeBuilder.CreateInterface(
                NameDefinition.Create(NameFactory.ITupleTypeName,
                    TemplateParametersBuffer.Create(VarianceMode.Out, type_parameters.Concat(base_type_name).ToArray()).Values))
                .Parents(NameFactory.IIndexableTypeReference(NameReference.Create(base_type_name)))

                .With(properties)

                .With(PropertyBuilder.Create(NameFactory.IterableCount, NameFactory.IntTypeReference())
                    .WithGetter(Block.CreateStatement(Return.Create(IntLiteral.Create($"{count}"))), EntityModifier.Override))

                .Constraints(TemplateConstraint.Create(base_type_name, null, null, null,
                    type_parameters.Select(it => NameReference.Create(it))));

            return builder;
        }

        public bool IsFunctionType(EntityInstance instance)
        {
            if (instance == null)
                return false;

            return functionTypes.Any(it => it == instance.Target);
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
            return instance.EnumerateAll().All(it => it.IsOfType(ReferenceType));
        }
        public bool IsPointerOfType(IEntityInstance instance)
        {
            return instance.EnumerateAll().All(it => it.IsOfType(PointerType));
        }

        public bool Dereferenced(IEntityInstance instance, out IEntityInstance result, out bool viaPointer)
        {
            if (IsPointerOfType(instance))
            {
                viaPointer = true;
                result = instance.Map(it => it.TemplateArguments.Single());
                return true;
            }
            else if (IsReferenceOfType(instance))
            {
                viaPointer = false;
                result = instance.Map(it => it.TemplateArguments.Single());
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
            return instance.EnumerateAll().All(it => it.IsOfType(PointerType) || it.IsOfType(ReferenceType));
        }
    }
}
