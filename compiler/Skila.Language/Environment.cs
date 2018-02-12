using System.Collections.Generic;
using System.Linq;
using Skila.Language.Builders;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Flow;
using Skila.Language.Extensions;
using NaiveLanguageTools.Common;
using System;
using Skila.Language.Expressions.Literals;

namespace Skila.Language
{
    public sealed class Environment
    {
        public static Environment Create(IOptions options)
        {
            return new Environment(options);
        }

        private Option<FunctionDefinition> mainFunction;
        public FunctionDefinition MainFunction
        {
            get
            {
                if (!mainFunction.HasValue)
                    mainFunction = new Option<FunctionDefinition>(this.Root.FindEntities(NameReference.Create(NameFactory.MainFunctionName),
                        EntityFindMode.ScopeLimited).FirstOrDefault()?.TargetFunction);
                return mainFunction.Value;
            }
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
        public Namespace IoNamespace { get; }
        public Namespace TextNamespace { get; }

        public TypeDefinition Int16Type { get; }
        public FunctionDefinition Int16ParseStringFunction { get; }

        public TypeDefinition Int64Type { get; }
        public FunctionDefinition Int64ParseStringFunction { get; }
        public FunctionDefinition Int64FromNat8Constructor { get; }

        public TypeDefinition Nat64Type { get; }
        public FunctionDefinition Nat64ParseStringFunction { get; }
        public FunctionDefinition Nat64FromNat8Constructor { get; }

        public TypeDefinition Nat8Type { get; }
        public FunctionDefinition Nat8ParseStringFunction { get; }

        //public TypeDefinition EnumType { get; }

        public TypeDefinition StringType { get; }
        public FunctionDefinition StringCountGetter { get; }

        public TypeDefinition CaptureType { get; }
        public TypeDefinition MatchType { get; }
        public TypeDefinition RegexType { get; }
        public FunctionDefinition RegexContainsFunction { get; }
        public FunctionDefinition RegexMatchFunction { get; }
        public VariableDeclaration RegexPatternField { get; }

        public TypeDefinition TypeInfoType { get; }

        public TypeDefinition FileType { get; }
        public FunctionDefinition FileReadLines { get; }
        public FunctionDefinition FileExists { get; }

        public TypeDefinition OrderingType { get; }
        public VariableDeclaration OrderingLess { get; }
        public VariableDeclaration OrderingEqual { get; }
        public VariableDeclaration OrderingGreater { get; }
        public TypeDefinition ComparableType { get; }
        public TypeDefinition DoubleType { get; }
        public TypeDefinition IObjectType { get; }
        public FunctionDefinition IObjectGetTypeFunction { get; }

        public TypeDefinition ChunkType { get; }
        public FunctionDefinition ChunkSizeConstructor { get; }
        public FunctionDefinition ChunkResizeConstructor { get; }
        public FunctionDefinition ChunkCount { get; }
        public FunctionDefinition ChunkAtGet { get; }
        public FunctionDefinition ChunkAtSet { get; }

        public TypeDefinition ArrayType { get; }

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
            this.IoNamespace = this.SystemNamespace.AddNode(Namespace.Create(NameFactory.IoNamespace));
            this.TextNamespace = this.SystemNamespace.AddNode(Namespace.Create(NameFactory.TextNamespace));

            this.IObjectGetTypeFunction = FunctionBuilder.Create(NameFactory.GetTypeFunctionName, NameFactory.TypeInfoPointerTypeReference(),
                    Block.CreateStatement())
                        .Modifier(EntityModifier.Native);
            this.IObjectType = this.Root.AddBuilder(TypeBuilder.CreateInterface(NameFactory.IObjectTypeName)
                .With(IObjectGetTypeFunction));

            this.UnitType = Root.AddBuilder(TypeBuilder.Create(NameFactory.UnitTypeName)
    .Modifier(EntityModifier.Native)
    .With(VariableDeclaration.CreateStatement(NameFactory.UnitValue, NameFactory.UnitTypeReference(), null,
        EntityModifier.Static | EntityModifier.Native))
    .With(FunctionDefinition.CreateInitConstructor(EntityModifier.Native | EntityModifier.Private, null, Block.CreateStatement()))
    .Parents(NameFactory.ObjectTypeReference()));

            this.UnitEvaluation = new EvaluationInfo(this.UnitType.InstanceOf);

            // pointer and reference are not of Object type (otherwise we could have common root for String and pointer to Int)
            this.ReferenceType = Root.AddBuilder(TypeBuilder.Create(NameDefinition.Create(NameFactory.ReferenceTypeName, "RFT", VarianceMode.Out))
                // todo: uncomment this when we have traits and IReplicable interface
                // .Modifier(EntityModifier.Native | EntityModifier.Base)
                .Modifier(EntityModifier.Native)
                .Slicing(true));

            this.PointerType = Root.AddBuilder(TypeBuilder.Create(NameDefinition.Create(NameFactory.PointerTypeName, "PTT", VarianceMode.Out))
                .Modifier(EntityModifier.Native)
                // todo: uncomment this when we have traits and IReplicable interface
                // this allows to make such override of the method
                // Parent::foo() -> Ref<T>
                // Child::foo() -> Ptr<T> 
                //                .Parents(NameFactory.ReferenceTypeReference("PTT"))
                .Slicing(true));

            this.TypeInfoType = this.SystemNamespace.AddNode(createTypeInfo());

            if (this.Options.MiniEnvironment)
                return;






            {
                this.Int16Type = this.Root.AddNode(createNumXType(NameFactory.Int16TypeName,
                    Int16Literal.Create($"{Int16.MinValue}"),
                    Int16Literal.Create($"{Int16.MaxValue}"),
                    out FunctionDefinition parse_string));
                this.Int16ParseStringFunction = parse_string;
            }
            {
                this.Int64FromNat8Constructor = FunctionDefinition.CreateInitConstructor(EntityModifier.Native | EntityModifier.Implicit,
                    new[] { FunctionParameter.Create(NameFactory.SourceCopyConstructorParameter, NameFactory.Nat8TypeReference(),
                        ExpressionReadMode.CannotBeRead) },
                    Block.CreateStatement());

                this.Int64Type = this.Root.AddNode(createNumXType(NameFactory.Int64TypeName,
                    Int64Literal.Create($"{Int64.MinValue}"),
                    Int64Literal.Create($"{Int64.MaxValue}"),
                    out FunctionDefinition parse_string,
                    this.Int64FromNat8Constructor));
                // todo: make it platform-dependant
                this.Root.AddNode(Alias.Create(NameFactory.IntTypeName, NameFactory.Int64TypeReference(), EntityModifier.Public));
                this.Int64ParseStringFunction = parse_string;
            }
            {
                this.Nat8Type = this.Root.AddNode(createNumXType(NameFactory.Nat8TypeName,
                    Nat8Literal.Create($"{byte.MinValue}"),
                    Nat8Literal.Create($"{byte.MaxValue}"),
                    out FunctionDefinition parse_string));
                this.Nat8ParseStringFunction = parse_string;
            }
            {
                this.Nat64FromNat8Constructor = FunctionDefinition.CreateInitConstructor(EntityModifier.Native | EntityModifier.Implicit,
                    new[] { FunctionParameter.Create(NameFactory.SourceCopyConstructorParameter, NameFactory.Nat8TypeReference(),
                        ExpressionReadMode.CannotBeRead) },
                    Block.CreateStatement());

                this.Nat64Type = this.Root.AddNode(createNumXType(NameFactory.Nat64TypeName,
                   Nat64Literal.Create($"{UInt64.MinValue}"),
                   Nat64Literal.Create($"{UInt64.MaxValue}"),
                   out FunctionDefinition parse_string,
                   this.Nat64FromNat8Constructor));
                // todo: make it platform-dependant
                this.Root.AddNode(Alias.Create(NameFactory.NatTypeName, NameFactory.Nat64TypeReference(), EntityModifier.Public));
                this.Root.AddNode(Alias.Create(NameFactory.SizeTypeName, NameFactory.NatTypeReference(), EntityModifier.Public));
                this.Nat64ParseStringFunction = parse_string;
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

            {
                createSpreads(out FunctionDefinition spread_min, out FunctionDefinition spread_min_max);
                this.SystemNamespace.AddNode(spread_min);
                this.SystemNamespace.AddNode(spread_min_max);
            }

            {
                FunctionDefinition read_lines;
                FunctionDefinition exists;
                this.FileType = this.IoNamespace.AddNode(createFile(readLines: out read_lines, exists: out exists));
                this.FileReadLines = read_lines;
                this.FileExists = exists;
            }

            this.CaptureType = this.TextNamespace.AddNode(createCapture());
            this.MatchType = this.TextNamespace.AddNode(createMatch());
            {
                this.RegexType = this.TextNamespace.AddNode(createRegex(
                    out VariableDeclaration pattern,
                    out FunctionDefinition contains,
                    out FunctionDefinition match));
                this.RegexPatternField = pattern;
                this.RegexContainsFunction = contains;
                this.RegexMatchFunction = match;
            }

            this.IIterableType = this.CollectionsNamespace.AddNode(createIIterable());

            this.IIteratorType = this.CollectionsNamespace.AddNode(createIIterator());

            this.IndexIteratorType = this.CollectionsNamespace.AddNode(createIndexIterator());

            this.ISequenceType = this.CollectionsNamespace.AddBuilder(
                TypeBuilder.CreateInterface(NameDefinition.Create(NameFactory.ISequenceTypeName, "SQT", VarianceMode.Out))
                    .Parents(NameFactory.IIterableTypeReference("SQT"))

                    .With(PropertyBuilder.Create(NameFactory.IterableCount, NameFactory.SizeTypeReference())
                        .WithGetter(body: null, modifier: EntityModifier.Override)));

            this.IIndexableType = this.CollectionsNamespace.AddNode(createIIndexable());

            {
                this.ChunkType = this.CollectionsNamespace.AddNode(createChunk(
                    sizeConstructor: out FunctionDefinition size_cons,
                    resizeConstructor: out FunctionDefinition resize_cons,
                    countGetter: out IMember chunk_count,
                    atGetter: out IMember chunk_at_get,
                    atSetter: out IMember chunk_at_set));

                this.ChunkSizeConstructor = size_cons;
                this.ChunkResizeConstructor = resize_cons;
                this.ChunkAtGet = chunk_at_get.Cast<FunctionDefinition>();
                this.ChunkAtSet = chunk_at_set.Cast<FunctionDefinition>();
                this.ChunkCount = chunk_count.Cast<FunctionDefinition>();
            }

            this.ArrayType = this.CollectionsNamespace.AddNode(createArray());

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
                    .With(PropertyBuilder.CreateAutoFull("year", NameFactory.Int16TypeReference(), out PropertyMembers year))
                    .With(PropertyBuilder.CreateAutoFull("month", NameFactory.Nat8TypeReference(), out PropertyMembers month,
                        Nat8Literal.Create("1")))
                    .With(PropertyBuilder.CreateAutoFull("day", NameFactory.Nat8TypeReference(), out PropertyMembers day,
                        Nat8Literal.Create("1")))
                    .With(ExpressionFactory.BasicConstructor(new[] { "year", "month", "day" },
                        new[] { NameFactory.Int16TypeReference(), NameFactory.Nat8TypeReference(), NameFactory.Nat8TypeReference() }))
                    .With(PropertyBuilder.Create(NameFactory.DateDayOfWeekProperty, NameFactory.DayOfWeekTypeReference())
                        .WithGetter(ExpressionFactory.BodyReturnUndef(), out FunctionDefinition day_of_week_getter, EntityModifier.Native))
                    );

                this.DateDayOfWeekGetter = day_of_week_getter;
                this.DateYearField = year.Field;
                this.DateMonthField = month.Field;
                this.DateDayField = day.Field;
            }

            this.CollectionsNamespace.AddNode(createConcat1());
            this.CollectionsNamespace.AddNode(createConcat3());

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

            {
                FunctionDefinition count_getter;
                this.StringType = this.SystemNamespace.AddNode(createString(out count_getter));
                this.StringCountGetter = count_getter;
            }

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

        private void createSpreads(out FunctionDefinition spread_min, out FunctionDefinition spread_min_max)
        {
            Func<IfBranch> make_solid = () => IfBranch.CreateIf(ExpressionFactory.Not(IsType.Create(NameReference.Create("coll"), 
                NameFactory.ISequenceTypeReference("T"))),
                        new[] {
                                Assignment.CreateStatement(NameReference.Create("coll"),
                                    ExpressionFactory.HeapConstructor(NameFactory.ArrayTypeReference("T", MutabilityFlag.Neutral),
                                        NameReference.Create("coll")))
                        });

            // todo: take iterables as input and convert them to sequence (all spreads)

            // with min limit
                spread_min = FunctionBuilder.Create(
                        NameDefinition.Create(NameFactory.SpreadFunctionName, "T", VarianceMode.None),
                   new[] {
                        FunctionParameter.Create("coll", NameFactory.ReferenceTypeReference(NameFactory.ISequenceTypeReference("T",
//                        FunctionParameter.Create("coll", NameFactory.ReferenceTypeReference(NameFactory.IIterableTypeReference("T",
                            overrideMutability:MutabilityFlag.Neutral))
                            //, EntityModifier.Reassignable
                            ),
                        FunctionParameter.Create("min", NameFactory.SizeTypeReference()),
                   },
                   ExpressionReadMode.ReadRequired,
                   NameFactory.ReferenceTypeReference(NameFactory.ISequenceTypeReference("T",
                        overrideMutability: MutabilityFlag.Neutral)),
                   Block.CreateStatement(
                       // todo:
                       // make_solid(),

                       IfBranch.CreateIf(ExpressionFactory.IsLess(FunctionCall.Create(NameReference.Create("coll", NameFactory.IterableCount)),
                            NameReference.Create("min")), new[] { ExpressionFactory.GenericThrow() }),
                       Return.Create(NameReference.Create("coll"))
                    ));

            // with min+max limit
                spread_min_max = FunctionBuilder.Create(NameDefinition.Create(
                        NameFactory.SpreadFunctionName, "T", VarianceMode.None),
                    new[] {
                        FunctionParameter.Create("coll", NameFactory.ReferenceTypeReference(  NameFactory.ISequenceTypeReference("T",
//                        FunctionParameter.Create("coll", NameFactory.ReferenceTypeReference(  NameFactory.IIterableTypeReference("T",
                            overrideMutability:MutabilityFlag.Neutral))
                            //, EntityModifier.Reassignable
                            ),
                        FunctionParameter.Create("min", NameFactory.SizeTypeReference()),
                        FunctionParameter.Create("max", NameFactory.SizeTypeReference()),
                    },
                    ExpressionReadMode.ReadRequired,
                    NameFactory.ReferenceTypeReference(NameFactory.ISequenceTypeReference("T",
                        overrideMutability: MutabilityFlag.Neutral)),
                    Block.CreateStatement(
                        //todo:
                        // make_solid(),
                        VariableDeclaration.CreateStatement("count", null,
                            FunctionCall.Create(NameReference.Create("coll", NameFactory.IterableCount))),
                       IfBranch.CreateIf(ExpressionFactory.IsLess(NameReference.Create("count"),
                            NameReference.Create("min")), new[] { ExpressionFactory.GenericThrow() }),
                       IfBranch.CreateIf(ExpressionFactory.IsGreater(NameReference.Create("count"),
                            NameReference.Create("max")), new[] { ExpressionFactory.GenericThrow() }),
                       Return.Create(NameReference.Create("coll"))
                    ));
        }

        private TypeDefinition createTypeInfo()
        {
            return TypeBuilder.Create(NameFactory.TypeInfoTypeName)
                .Modifier(EntityModifier.HeapOnly);
        }

        private TypeDefinition createCapture()
        {
            return TypeBuilder.Create(NameFactory.CaptureTypeName)
                .With(PropertyBuilder.CreateAutoGetter(NameFactory.CaptureIndexFieldName, NameFactory.SizeTypeReference(), Undef.Create()))
                .With(PropertyBuilder.CreateAutoGetter(NameFactory.CaptureCountFieldName, NameFactory.SizeTypeReference(), Undef.Create()))
                .With(PropertyBuilder.CreateAutoGetter(NameFactory.CaptureIdFieldName, NameFactory.SizeTypeReference(), Undef.Create()))
                .With(PropertyBuilder.CreateAutoGetter(NameFactory.CaptureNameFieldName,
                    NameFactory.OptionTypeReference(NameFactory.StringPointerTypeReference(MutabilityFlag.ForceConst)), Undef.Create()))
                .With(ExpressionFactory.BasicConstructor(new[] {
                        NameFactory.CaptureIndexFieldName,
                        NameFactory.CaptureCountFieldName,
                        NameFactory.CaptureIdFieldName,
                        NameFactory.CaptureNameFieldName
                    },
                    new[] {
                        NameFactory.SizeTypeReference(),
                        NameFactory.SizeTypeReference(),
                        NameFactory.SizeTypeReference(),
                        NameFactory.OptionTypeReference(NameFactory.StringPointerTypeReference(MutabilityFlag.ForceConst))
                    }))
                    ;
        }

        private TypeDefinition createMatch()
        {
            return TypeBuilder.Create(NameFactory.MatchTypeName)
                .With(PropertyBuilder.CreateAutoGetter(NameFactory.MatchIndexFieldName, NameFactory.SizeTypeReference(), Undef.Create()))
                .With(PropertyBuilder.CreateAutoGetter(NameFactory.MatchCountFieldName, NameFactory.SizeTypeReference(), Undef.Create()))
                .With(PropertyBuilder.CreateAutoGetter(NameFactory.MatchCapturesFieldName,
                    NameFactory.PointerTypeReference(NameFactory.ArrayTypeReference(NameFactory.CaptureTypeReference(), MutabilityFlag.ForceConst)),
                        Undef.Create()))
                .With(ExpressionFactory.BasicConstructor(new[] {
                        NameFactory.MatchIndexFieldName,
                        NameFactory.MatchCountFieldName,
                        NameFactory.MatchCapturesFieldName
                    },
                    new[] {
                        NameFactory.SizeTypeReference(),
                        NameFactory.SizeTypeReference(),
                        NameFactory.PointerTypeReference( NameFactory.ArrayTypeReference(NameFactory.CaptureTypeReference(),
                            MutabilityFlag.ForceConst))
                    }))
                    ;
        }

        private TypeDefinition createRegex(out VariableDeclaration pattern, out FunctionDefinition contains, out FunctionDefinition match)
        {
            pattern = VariableDeclaration.CreateStatement(NameFactory.RegexPatternFieldName,
                    NameFactory.StringPointerTypeReference(MutabilityFlag.ForceConst),
                    Undef.Create(), EntityModifier.Native);

            contains = FunctionBuilder.Create(NameFactory.RegexContainsFunctionName, NameFactory.BoolTypeReference(), Block.CreateStatement())
                    .Modifier(EntityModifier.Native)
                    .Parameters(FunctionParameter.Create("input", NameFactory.StringPointerTypeReference(MutabilityFlag.Neutral),
                        ExpressionReadMode.CannotBeRead));

            match = FunctionBuilder.Create(NameFactory.RegexMatchFunctionName,
                NameFactory.PointerTypeReference(NameFactory.IIterableTypeReference(NameFactory.MatchTypeReference())),
                Block.CreateStatement())
                    .Modifier(EntityModifier.Native)
                    .Parameters(FunctionParameter.Create("input", NameFactory.StringPointerTypeReference(MutabilityFlag.Neutral),
                        ExpressionReadMode.CannotBeRead));

            return TypeBuilder.Create(NameFactory.RegexTypeName)
                .With(pattern)
                .With(ExpressionFactory.BasicConstructor(new[] {
                        NameFactory.RegexPatternFieldName,
                    },
                    new[] {
                        NameFactory.StringPointerTypeReference(MutabilityFlag.ForceConst),
                    }))
                .With(contains)
                .With(match)
                    ;
        }

        private TypeDefinition createString(out FunctionDefinition countGetter)
        {
            Property count_property = PropertyBuilder.Create(NameFactory.IterableCount, NameFactory.SizeTypeReference())
                .With(PropertyMemberBuilder.CreateGetter(Block.CreateStatement())
                    .Modifier(EntityModifier.Native));

            countGetter = count_property.Getter;

            return TypeBuilder.Create(NameFactory.StringTypeName)
                                .Modifier(EntityModifier.HeapOnly | EntityModifier.Native | EntityModifier.Mutable)
                                .Parents(NameFactory.ObjectTypeReference())
                                .With(count_property);
        }

        private TypeDefinition createFile(out FunctionDefinition readLines, out FunctionDefinition exists)
        {
            readLines = FunctionBuilder.Create(NameFactory.FileReadLines,
                    NameFactory.OptionTypeReference(NameFactory.PointerTypeReference(NameFactory.IIterableTypeReference(NameFactory.StringPointerTypeReference()))),
                    Block.CreateStatement())
                      .Parameters(FunctionParameter.Create(NameFactory.FileFilePathParameter,
                            NameFactory.StringPointerTypeReference(), ExpressionReadMode.CannotBeRead))
                      .Modifier(EntityModifier.Native);
            exists = FunctionBuilder.Create(NameFactory.FileExists, NameFactory.BoolTypeReference(),
                    Block.CreateStatement())
                      .Parameters(FunctionParameter.Create(NameFactory.FileFilePathParameter,
                            NameFactory.StringPointerTypeReference(), ExpressionReadMode.CannotBeRead))
                      .Modifier(EntityModifier.Native);

            TypeBuilder builder = TypeBuilder.Create(NameFactory.FileTypeName)
                .Modifier(EntityModifier.Static)
                .With(readLines)
                .With(exists)
                ;

            return builder;
        }

        private static FunctionDefinition createConcat1()
        {
            // todo: after adding parser rewrite it as creating Concat type holding fragments and iterating over their elements
            // this (below) has too many allocations

            const string elem_type = "CCT";
            const string buffer_name = "buffer";
            const string coll1_name = "coll1";
            const string coll2_name = "coll2";
            const string elem_name = "cat1_elem";
            return FunctionBuilder.Create(NameDefinition.Create(NameFactory.ConcatFunctionName, elem_type, VarianceMode.None),
                NameFactory.PointerTypeReference(NameFactory.IIterableTypeReference(elem_type, MutabilityFlag.Neutral)),
                Block.CreateStatement(
                    VariableDeclaration.CreateStatement(buffer_name, null,
                        ExpressionFactory.HeapConstructor(NameFactory.ArrayTypeReference(elem_type), NameReference.Create(coll1_name))),
                    Loop.CreateForEach(elem_name, NameReference.Create(elem_type), NameReference.Create(coll2_name), new[] {
                        FunctionCall.Create(NameReference.Create(buffer_name,NameFactory.AppendFunctionName),NameReference.Create(elem_name))
                    }),
                    Return.Create(NameReference.Create(buffer_name))
                    ))
                    .Parameters(FunctionParameter.Create(coll1_name,
                            NameFactory.ReferenceTypeReference(NameFactory.IIterableTypeReference(elem_type, MutabilityFlag.Neutral))),
                        FunctionParameter.Create(coll2_name,
                            NameFactory.ReferenceTypeReference(NameFactory.IIterableTypeReference(elem_type, MutabilityFlag.Neutral))));
        }
        private static FunctionDefinition createConcat3()
        {
            // todo: after adding parser rewrite it as creating Concat type holding fragments and iterating over their elements
            // this (below) has too many allocations

            const string elem1_type = "CCA";
            const string elem2_type = "CCB";
            const string elem3_type = "CCC";
            const string buffer_name = "buffer";
            const string coll1_name = "coll1";
            const string coll2_name = "coll2";
            const string elem_name = "cat3_elem";
            return FunctionBuilder.Create(NameDefinition.Create(NameFactory.ConcatFunctionName,
    TemplateParametersBuffer.Create(elem1_type, elem2_type, elem3_type).Values),
                NameFactory.PointerTypeReference(NameFactory.IIterableTypeReference(NameFactory.PointerTypeReference(elem3_type),
                    MutabilityFlag.Neutral)),
                Block.CreateStatement(
                    VariableDeclaration.CreateStatement(buffer_name, null,
                        ExpressionFactory.HeapConstructor(NameFactory.ArrayTypeReference(NameFactory.PointerTypeReference(elem3_type)),
                            NameReference.Create(coll1_name))),
                    Loop.CreateForEach(elem_name, null, NameReference.Create(coll2_name), new[] {
                        FunctionCall.Create(NameReference.Create(buffer_name,NameFactory.AppendFunctionName),NameReference.Create(elem_name))
                    }),
                    Return.Create(NameReference.Create(buffer_name))
                    ))
                .Constraints(ConstraintBuilder.Create(elem1_type).Modifier(EntityModifier.HeapOnly),
                    ConstraintBuilder.Create(elem2_type).Modifier(EntityModifier.HeapOnly),
                    ConstraintBuilder.Create(elem3_type).BaseOf(elem1_type, elem2_type).Modifier(EntityModifier.HeapOnly))
                .Parameters(FunctionParameter.Create(coll1_name,
                    NameFactory.ReferenceTypeReference(NameFactory.IIterableTypeReference(NameFactory.PointerTypeReference(elem1_type),
                        MutabilityFlag.Neutral))),
                    FunctionParameter.Create(coll2_name,
                        NameFactory.ReferenceTypeReference(NameFactory.IIterableTypeReference(NameFactory.PointerTypeReference(elem2_type),
                            MutabilityFlag.Neutral))));
        }

        private static TypeDefinition createIIterable()
        {
            const string elem_type = "ITBT";

            FunctionDefinition map_func;
            {
                const string map_type = "MPT";
                const string buffer_name = "buffer";
                const string mapper_name = "mapper";
                const string elem_name = "map_elem";
                map_func = FunctionBuilder.Create(NameDefinition.Create(NameFactory.MapFunctionName, map_type, VarianceMode.None),
                    NameFactory.PointerTypeReference(NameFactory.IIterableTypeReference(map_type, MutabilityFlag.Neutral)),
                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement(buffer_name, null,
                            ExpressionFactory.HeapConstructor(NameFactory.ArrayTypeReference(map_type))),
                        Loop.CreateForEach(elem_name, NameReference.Create(elem_type), NameFactory.ThisReference(), new[] {
                        FunctionCall.Create(NameReference.Create(buffer_name,NameFactory.AppendFunctionName),
                            FunctionCall.Create(NameReference.Create(mapper_name), NameReference.Create(elem_name)))
                        }),
                        Return.Create(NameReference.Create(buffer_name))
                        ))
                        .Parameters(FunctionParameter.Create(mapper_name,
                                NameFactory.ReferenceTypeReference(NameFactory.IFunctionTypeReference(
                                     NameReference.Create(elem_type), NameReference.Create(map_type)))));
            }

            FunctionDefinition filter_func;
            {
                const string buffer_name = "buffer";
                const string pred_name = "pred";
                const string elem_name = "filtered_elem";
                filter_func = FunctionBuilder.Create(NameFactory.FilterFunctionName,
                    NameFactory.PointerTypeReference(NameFactory.IIterableTypeReference(elem_type, MutabilityFlag.Neutral)),
                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement(buffer_name, null,
                            ExpressionFactory.HeapConstructor(NameFactory.ArrayTypeReference(elem_type))),
                        Loop.CreateForEach(elem_name, NameReference.Create(elem_type), NameFactory.ThisReference(), new[] {
                            IfBranch.CreateIf(FunctionCall.Create(NameReference.Create(pred_name),NameReference.Create(elem_name)),new[]{
                                FunctionCall.Create(NameReference.Create(buffer_name,NameFactory.AppendFunctionName),
                                    NameReference.Create(elem_name))
                            })
                        }),
                        Return.Create(NameReference.Create(buffer_name))
                        ))
                        .Parameters(FunctionParameter.Create(pred_name,
                                NameFactory.ReferenceTypeReference(NameFactory.IFunctionTypeReference(
                                     NameReference.Create(elem_type), NameFactory.BoolTypeReference()))));
            }

            return TypeBuilder.CreateInterface(NameDefinition.Create(NameFactory.IIterableTypeName, elem_type, VarianceMode.Out))
                                .Parents(NameFactory.ObjectTypeReference())
                                .With(FunctionBuilder.CreateDeclaration(NameFactory.PropertyIndexerName, ExpressionReadMode.ReadRequired,
                                    NameFactory.ReferenceTypeReference(NameReference.Create(elem_type)))
                                    .Parameters(FunctionParameter.Create(NameFactory.IndexIndexerParameter, NameFactory.SizeTypeReference())))

                .With(FunctionBuilder.CreateDeclaration(NameFactory.IterableGetIterator,
                    NameFactory.ReferenceTypeReference(NameFactory.IIteratorTypeReference(elem_type))))

                                .With(FunctionBuilder.CreateDeclaration(NameFactory.IterableCount, ExpressionReadMode.ReadRequired,
                                    NameFactory.SizeTypeReference()))

                 .With(filter_func)
                 .With(map_func);
        }

        private static TypeDefinition createArray()
        {
            const string elem_type = "ART";
            const string data_field = "data";

            Property count_property = PropertyBuilder.Create(NameFactory.IterableCount, NameFactory.SizeTypeReference())
                        .WithAutoField(NatLiteral.Create("0"), EntityModifier.Reassignable)
                        .WithAutoSetter(EntityModifier.Private)
                        .WithAutoGetter(EntityModifier.Override);

            PropertyMemberBuilder indexer_getter = PropertyMemberBuilder.CreateIndexerGetter(Block.CreateStatement(
                            Return.Create(FunctionCall.Indexer(NameReference.CreateThised(data_field),
                                NameFactory.IndexIndexerReference()))
                            ))
                            .Modifier(EntityModifier.Override);

            FunctionDefinition append = FunctionBuilder.Create(NameFactory.AppendFunctionName, NameFactory.UnitTypeReference(),
                Block.CreateStatement(
                    VariableDeclaration.CreateStatement("pos", null, NameReference.CreateThised(NameFactory.IterableCount)),
                                    // ++this.count;
                                    ExpressionFactory.Inc(() => NameReference.CreateThised(NameFactory.IterableCount)),
                                    // if this.count>this.data.count then
                                    IfBranch.CreateIf(ExpressionFactory.IsGreater(NameReference.CreateThised(NameFactory.IterableCount),
                                        NameReference.CreateThised(data_field, NameFactory.IterableCount)), new[]{
                                            // this.data = new Chunk<ART>(this.count,this.data);
                                            Assignment.CreateStatement(NameReference.CreateThised(data_field),
                                                ExpressionFactory.HeapConstructor(NameFactory.ChunkTypeReference(elem_type),
                                                NameReference.CreateThised(NameFactory.IterableCount),
                                                NameReference.CreateThised(data_field)))
                                        }),
                    Assignment.CreateStatement(FunctionCall.Indexer(NameReference.CreateThised(data_field),
                                NameReference.Create("pos")), NameFactory.PropertySetterValueReference())))
                .Parameters(FunctionParameter.Create(NameFactory.PropertySetterValueParameter,
                    NameFactory.ReferenceTypeReference(elem_type)))
                .Modifier(EntityModifier.Mutable);

            PropertyMemberBuilder indexer_setter = PropertyMemberBuilder.CreateIndexerSetter(Block.CreateStatement(
                            // assert index<=this.count;
                            ExpressionFactory.AssertTrue(ExpressionFactory.IsLessEqual(NameFactory.IndexIndexerReference(),
                                NameReference.CreateThised(NameFactory.IterableCount))),
                            // if index==this.count then
                            IfBranch.CreateIf(ExpressionFactory.IsEqual(NameFactory.IndexIndexerReference(),
                                NameReference.CreateThised(NameFactory.IterableCount)), new IExpression[] {
                                    FunctionCall.Create(NameReference.CreateThised(NameFactory.AppendFunctionName),
                                        NameFactory.PropertySetterValueReference())
                                }, IfBranch.CreateElse(new[] {
                                    // this.data[index] = value;
                                    Assignment.CreateStatement(FunctionCall.Indexer(NameReference.CreateThised(data_field),
                                        NameFactory.IndexIndexerReference()), NameFactory.PropertySetterValueReference())
                                }))
                            ));

            const string elem_name = "arr_cc_elem";
            return TypeBuilder.Create(NameDefinition.Create(NameFactory.ArrayTypeName, elem_type, VarianceMode.None))
                    .Modifier(EntityModifier.Mutable | EntityModifier.HeapOnly)
                    .Parents(NameFactory.IIndexableTypeReference(elem_type))

                    .With(VariableDeclaration.CreateStatement(data_field,
                        NameFactory.PointerTypeReference(NameFactory.ChunkTypeReference(elem_type)), Undef.Create(),
                        EntityModifier.Reassignable))

                    .With(count_property)

                    // default constructor
                    .With(FunctionBuilder.CreateInitConstructor(Block.CreateStatement(
                        // this.data = new Chunk<ART>(1);
                        Assignment.CreateStatement(NameReference.CreateThised(data_field),
                            ExpressionFactory.HeapConstructor(NameFactory.ChunkTypeReference(elem_type),
                                NatLiteral.Create("1")))
                        )))

                    // copy constructor
                    .With(FunctionBuilder.CreateInitConstructor(Block.CreateStatement(
                        Loop.CreateForEach(elem_name, NameReference.Create(elem_type),
                            NameReference.Create(NameFactory.SourceCopyConstructorParameter),
                            new[] {
                            FunctionCall.Create(NameReference.CreateThised(NameFactory.AppendFunctionName),NameReference.Create(elem_name))
                        })), FunctionCall.Constructor(NameReference.CreateThised(NameFactory.InitConstructorName)))
                        .Parameters(FunctionParameter.Create(NameFactory.SourceCopyConstructorParameter,
                            NameFactory.ReferenceTypeReference(NameFactory.IIterableTypeReference(elem_type, MutabilityFlag.Neutral)))))

                     .With(append)

                     .With(PropertyBuilder.CreateIndexer(NameFactory.ReferenceTypeReference(elem_type))
                        .Parameters(FunctionParameter.Create(NameFactory.IndexIndexerParameter, NameFactory.SizeTypeReference()))
                        .With(indexer_setter)
                        .With(indexer_getter));
        }

        private static TypeDefinition createChunk(out FunctionDefinition sizeConstructor,
            out FunctionDefinition resizeConstructor,
            out IMember countGetter, out IMember atGetter, out IMember atSetter)
        {
            // todo: in this form this type is broken, size is runtime info, yet we allow the assignment on the stack
            // however it is not yet decided what we will do, maybe this type will be used only internally, 
            // maybe we will introduce two kinds of it, etc.

            const string elem_type = "CHT";

            sizeConstructor = FunctionDefinition.CreateInitConstructor(EntityModifier.Native, new[] {
                            FunctionParameter.Create(NameFactory.ChunkSizeConstructorParameter,NameFactory.SizeTypeReference(),
                                ExpressionReadMode.CannotBeRead)
                        },
                        Block.CreateStatement());

            resizeConstructor = FunctionDefinition.CreateInitConstructor(EntityModifier.Native, new[] {
                            FunctionParameter.Create(NameFactory.ChunkSizeConstructorParameter,NameFactory.SizeTypeReference(),
                                ExpressionReadMode.CannotBeRead),
                            FunctionParameter.Create(NameFactory.SourceCopyConstructorParameter,
                                NameFactory.ReferenceTypeReference( NameFactory.ChunkTypeReference(elem_type)),
                                ExpressionReadMode.CannotBeRead)
                        },
                        Block.CreateStatement());
            return TypeBuilder.Create(NameDefinition.Create(NameFactory.ChunkTypeName, elem_type, VarianceMode.None))
                    .Modifier(EntityModifier.Mutable)
                    .Parents(NameFactory.IIndexableTypeReference(elem_type))

                    .With(sizeConstructor)

                    .With(resizeConstructor)

                     .With(PropertyBuilder.Create(NameFactory.IterableCount, NameFactory.SizeTypeReference())
                        .With(PropertyMemberBuilder.CreateGetter(Block.CreateStatement())
                            .Modifier(EntityModifier.Native | EntityModifier.Override), out countGetter))

                     .With(PropertyBuilder.CreateIndexer(NameFactory.ReferenceTypeReference(elem_type))
                        .Parameters(FunctionParameter.Create(NameFactory.IndexIndexerParameter, NameFactory.SizeTypeReference(),
                            ExpressionReadMode.CannotBeRead))
                        .With(PropertyMemberBuilder.CreateIndexerSetter(Block.CreateStatement())
                            .Modifier(EntityModifier.Native), out atSetter)
                        .With(PropertyMemberBuilder.CreateIndexerGetter(Block.CreateStatement())
                            .Modifier(EntityModifier.Native | EntityModifier.Override), out atGetter));
        }

        private static TypeDefinition createNumXType(string numTypeName, Literal minValue, Literal maxValue, out FunctionDefinition parseString,
            params IMember[] extras)
        {
            parseString = FunctionBuilder.Create(NameFactory.ParseFunctionName, NameFactory.OptionTypeReference(NameFactory.ItTypeReference()),
                    ExpressionFactory.BodyReturnUndef())
                    .Parameters(FunctionParameter.Create("s", NameFactory.StringPointerTypeReference(), ExpressionReadMode.CannotBeRead))
                    .Modifier(EntityModifier.Native | EntityModifier.Static);

            return TypeBuilder.Create(numTypeName)
                .Modifier(EntityModifier.Native)
                .Parents(NameFactory.ObjectTypeReference(), NameFactory.ComparableTypeReference())
                .With(parseString)
                .With(extras)
                .With(VariableDeclaration.CreateStatement(NameFactory.NumMinValueName, NameFactory.ItTypeReference(),
                    minValue, EntityModifier.Static | EntityModifier.Const | EntityModifier.Public))
                .With(VariableDeclaration.CreateStatement(NameFactory.NumMaxValueName, NameFactory.ItTypeReference(),
                    maxValue, EntityModifier.Static | EntityModifier.Const | EntityModifier.Public))
                .With(FunctionDefinition.CreateInitConstructor(EntityModifier.Native,
                    null, Block.CreateStatement()))

                .With(FunctionDefinition.CreateInitConstructor(EntityModifier.Native,
                    new[] { FunctionParameter.Create(NameFactory.SourceCopyConstructorParameter, NameFactory.ItTypeReference(),
                        ExpressionReadMode.CannotBeRead) },
                    Block.CreateStatement()))

                .With(FunctionBuilder.Create(NameDefinition.Create(NameFactory.AddOperator),
                    ExpressionReadMode.ReadRequired, NameFactory.ItTypeReference(),
                    Block.CreateStatement())
                    .Modifier(EntityModifier.Native)
                    .Parameters(FunctionParameter.Create("x", NameFactory.ItTypeReference(), ExpressionReadMode.CannotBeRead)))
                .With(FunctionBuilder.Create(NameDefinition.Create(NameFactory.AddOverflowOperator),
                    ExpressionReadMode.ReadRequired, NameFactory.ItTypeReference(),
                    Block.CreateStatement())
                    .Modifier(EntityModifier.Native)
                    .Parameters(FunctionParameter.Create("x", NameFactory.ItTypeReference(), ExpressionReadMode.CannotBeRead)))
                .With(FunctionBuilder.Create(NameDefinition.Create(NameFactory.SubOperator),
                    ExpressionReadMode.ReadRequired, NameFactory.ItTypeReference(),
                    Block.CreateStatement())
                    .Modifier(EntityModifier.Native)
                    .Parameters(FunctionParameter.Create("x", NameFactory.ItTypeReference(), ExpressionReadMode.CannotBeRead)))
                .With(FunctionBuilder.Create(NameDefinition.Create(NameFactory.MulOperator),
                    ExpressionReadMode.ReadRequired, NameFactory.ItTypeReference(),
                    Block.CreateStatement())
                    .Modifier(EntityModifier.Native)
                    .Parameters(FunctionParameter.Create("x", NameFactory.ItTypeReference(), ExpressionReadMode.CannotBeRead)))

                .WithComparableCompare()
                .With(FunctionBuilder.Create(NameDefinition.Create(NameFactory.ComparableCompare),
                    ExpressionReadMode.ReadRequired, NameFactory.OrderingTypeReference(),
                    Block.CreateStatement())
                    .Modifier(EntityModifier.Native)
                    .Parameters(FunctionParameter.Create("cmp", NameFactory.ItTypeReference(), ExpressionReadMode.CannotBeRead)))

                .With(FunctionBuilder.Create(NameDefinition.Create(NameFactory.EqualOperator),
                    ExpressionReadMode.ReadRequired, NameFactory.BoolTypeReference(),
                    Block.CreateStatement())
                    .Modifier(EntityModifier.Native)
                    .Parameters(FunctionParameter.Create("cmp", NameFactory.ItTypeReference(), ExpressionReadMode.CannotBeRead)))
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

        private TypeDefinition createOptionType(out FunctionDefinition emptyConstructor, out FunctionDefinition valueConstructor)
        {
            const string value_field = "value";
            const string has_value_field = "hasValue";
            const string elem_type = "OPT";

            valueConstructor = FunctionDefinition.CreateInitConstructor(EntityModifier.Public,
                                new[] { FunctionParameter.Create("value", NameReference.Create(elem_type)) },
                                    Block.CreateStatement(new[] {
                                        Assignment.CreateStatement(NameReference.CreateThised(value_field), NameReference.Create("value")),
                                        Assignment.CreateStatement(NameReference.CreateThised(has_value_field), BoolLiteral.CreateTrue())
                                    }));

            emptyConstructor = FunctionDefinition.CreateInitConstructor(EntityModifier.Public,
                                null,
                                    Block.CreateStatement(
                                        Assignment.CreateStatement(NameReference.CreateThised(has_value_field), BoolLiteral.CreateFalse())
                                    ));

            Property has_value_getter = PropertyBuilder.Create(NameFactory.OptionHasValue, NameFactory.BoolTypeReference())
                                .With(PropertyMemberBuilder.CreateGetter(
                                    Block.CreateStatement(Return.Create(NameReference.CreateThised(has_value_field)))));

            Property value_getter = PropertyBuilder.Create(NameFactory.OptionValue, NameReference.Create(elem_type))
                .With(PropertyMemberBuilder.CreateGetter(Block.CreateStatement(
                                    ExpressionFactory.AssertTrue(NameReference.CreateThised(has_value_field)),
                                    Return.Create(NameReference.Create(NameFactory.ThisVariableName, value_field))
                                )));

            return TypeBuilder.Create(NameDefinition.Create(NameFactory.OptionTypeName,
              TemplateParametersBuffer.Create().Add(elem_type, VarianceMode.Out).Values))
                            .With(has_value_getter)
                            .With(value_getter)
                            .With(VariableDeclaration.CreateStatement(value_field, NameReference.Create(elem_type), Undef.Create()))
                            .With(VariableDeclaration.CreateStatement(has_value_field, NameFactory.BoolTypeReference(), Undef.Create()))
                            .With(emptyConstructor)
                            .With(valueConstructor);
        }

        private static TypeDefinition createComparableType()
        {
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
                .Parents(NameFactory.IEquatableTypeReference())
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
                var type_name = $"FNCT{i}";
                type_parameters.Add(type_name, VarianceMode.In);
                function_parameters.Add(FunctionParameter.Create($"item{i}", NameReference.Create(type_name)));
            }

            const string result_type = "FNCR";
            type_parameters.Add(result_type, VarianceMode.Out);

            TypeDefinition function_def = TypeBuilder.CreateInterface(
                NameDefinition.Create(NameFactory.IFunctionTypeName, type_parameters.Values))
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
                var type_name = $"TPT{i}";
                type_parameters.Add(type_name);
                properties.Add(PropertyBuilder.Create(NameFactory.TupleItemName(i), NameReference.Create(type_name))
                    .WithAutoField(Undef.Create(), EntityModifier.Reassignable)
                    .WithAutoSetter()
                    .WithAutoGetter(EntityModifier.Override));
            }

            IfBranch item_selector = IfBranch.CreateElse(new[] { ExpressionFactory.GenericThrow() });

            for (int i = count - 1; i >= 0; --i)
            {
                item_selector = IfBranch.CreateIf(ExpressionFactory.IsEqual(NameFactory.IndexIndexerReference(),
                    NatLiteral.Create($"{i}")), new[] { Return.Create(NameReference.CreateThised(NameFactory.TupleItemName(i))) },
                    item_selector);
            }

            const string base_type_name = "TPC";

            TypeBuilder builder = TypeBuilder.Create(
                NameDefinition.Create(NameFactory.TupleTypeName,
                    TemplateParametersBuffer.Create(type_parameters.ToArray()).Add(base_type_name, VarianceMode.Out).Values))
                .Modifier(EntityModifier.Mutable)
                .Parents(NameFactory.ITupleTypeReference(type_parameters.Concat(base_type_name).Select(it => NameReference.Create(it)).ToArray()))
                .With(ExpressionFactory.BasicConstructor(properties.Select(it => it.Name.Name).ToArray(),
                     type_parameters.Select(it => NameReference.Create(it)).ToArray()))

                .With(properties)

                .With(PropertyBuilder.CreateIndexer(NameFactory.ReferenceTypeReference(base_type_name))
                    .Parameters(FunctionParameter.Create(NameFactory.IndexIndexerParameter, NameFactory.SizeTypeReference()))
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

        private static TypeDefinition createIIterator()
        {
            const string elem_type = "ITRT";
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
            const string elem_type_name = "XIRT";
            const string coll_name = "coll";
            const string index_name = "index";

            TypeBuilder builder = TypeBuilder.Create(
                NameDefinition.Create(NameFactory.IndexIteratorTypeName,
                    TemplateParametersBuffer.Create(VarianceMode.Out, elem_type_name).Values))

                .Modifier(EntityModifier.Mutable | EntityModifier.AssociatedReference)
                .Parents(NameFactory.IIteratorTypeReference(elem_type_name))

                .With(VariableDeclaration.CreateStatement(index_name,
                     NameFactory.SizeTypeReference(), NameReference.Create(NameFactory.SizeTypeReference(), NameFactory.NumMaxValueName),
                     EntityModifier.Reassignable))
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
                            ExpressionFactory.AddOverflow(NameReference.CreateThised(index_name), Nat8Literal.Create("1"))),
                        Return.Create(ExpressionFactory.IsNotEqual(NameReference.CreateThised(index_name),
                            NameReference.CreateThised(coll_name, NameFactory.IterableCount)))
                        ))
                      .Modifier(EntityModifier.Mutable | EntityModifier.Override))

                  .With(FunctionBuilder.Create(NameFactory.IteratorGet,
                          NameFactory.ReferenceTypeReference(NameReference.Create(elem_type_name)),
                          Block.CreateStatement(
                        Return.Create(FunctionCall.Indexer(NameReference.CreateThised(coll_name), NameReference.CreateThised(index_name)))
                              ))
                      .Modifier(EntityModifier.Override))
            ;

            return builder;

        }
        private static TypeDefinition createIIndexable()
        {
            const string elem_type_name = "IXBT";

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
                    .Parameters(FunctionParameter.Create(NameFactory.IndexIndexerParameter, NameFactory.SizeTypeReference()))
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
                var type_name = $"TIPT{i}";
                type_parameters.Add(type_name);
                properties.Add(PropertyBuilder.Create(NameFactory.TupleItemName(i), NameReference.Create(type_name))
                    .WithGetter(body: null).Build());
            }

            const string base_type_name = "TIPC";

            TypeBuilder builder = TypeBuilder.CreateInterface(
                NameDefinition.Create(NameFactory.ITupleTypeName,
                    TemplateParametersBuffer.Create(VarianceMode.Out, type_parameters.Concat(base_type_name).ToArray()).Values))
                .Parents(NameFactory.IIndexableTypeReference(NameReference.Create(base_type_name)))

                .With(properties)

                .With(PropertyBuilder.Create(NameFactory.IterableCount, NameFactory.SizeTypeReference())
                    .WithGetter(Block.CreateStatement(Return.Create(NatLiteral.Create($"{count}"))), EntityModifier.Override))

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
            return typeInstance.IsSame(this.Int64Type.InstanceOf, jokerMatchesAll: false);
        }
        public bool IsNatType(IEntityInstance typeInstance)
        {
            return typeInstance.IsSame(this.Nat64Type.InstanceOf, jokerMatchesAll: false);
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
        public EntityInstance Reference(IEntityInstance instance, MutabilityFlag mutability, TemplateTranslation translation,
            bool viaPointer)
        {
            return (viaPointer ? this.PointerType : this.ReferenceType).GetInstance(new[] { instance }, mutability, translation);
        }
        public int Dereference(IEntityInstance instance, out IEntityInstance result)
        {
            int count = 0;

            while (true)
            {
                if (!DereferencedOnce(instance, out result, out bool dummy))
                    break;

                instance = result;
                ++count;
            }
            return count;
        }
        public bool Dereferenced(IEntityInstance instance, out IEntityInstance result)
        {
            return Dereference(instance, out result) > 0;
        }

        public bool DereferencedOnce(IEntityInstance instance, out IEntityInstance result, out bool viaPointer)
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
