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
using System.Diagnostics;

namespace Skila.Language
{
    public sealed class Environment
    {
        public static Environment Create(IOptions options)
        {
            return new Environment(options);
        }

        private const int maxSaneArity = 15;

        private Option<FunctionDefinition> mainFunction;
        public FunctionDefinition MainFunction(ComputationContext ctx)
        {
            if (!mainFunction.HasValue)
                mainFunction = new Option<FunctionDefinition>(this.Root.InstanceOf.FindEntities(ctx, NameReference.Create(NameFactory.MainFunctionName))
                    .FirstOrDefault()?.TargetFunction);
            return mainFunction.Value;
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

        public TypeDefinition SizeType => NatType;
        public TypeDefinition NatType => Nat64Type;

        public TypeDefinition Nat64Type { get; }
        public FunctionDefinition Nat64ParseStringFunction { get; }
        public FunctionDefinition Nat64FromNat8Constructor { get; }

        public TypeDefinition Nat8Type { get; }
        public FunctionDefinition Nat8ParseStringFunction { get; }

        //public TypeDefinition EnumType { get; }

        public TypeDefinition Real64Type { get; }
        public FunctionDefinition Real64ParseStringFunction { get; }
        public FunctionDefinition Real64FromNat8Constructor { get; }

        public TypeDefinition CharType { get; }
        public FunctionDefinition CharLengthGetter { get; }
        public FunctionDefinition CharToString { get; }

        public TypeDefinition Utf8StringType { get; }
        public FunctionDefinition Utf8StringCountGetter { get; }
        public FunctionDefinition Utf8StringAtGetter { get; }
        public FunctionDefinition Utf8StringTrimStart { get; }
        public FunctionDefinition Utf8StringTrimEnd { get; }
        public FunctionDefinition Utf8StringIndexOfString { get; }
        public FunctionDefinition Utf8StringLastIndexOfChar { get; }
        public FunctionDefinition Utf8StringReverse { get; }
        public FunctionDefinition Utf8StringSplit { get; }
        public FunctionDefinition Utf8StringSlice { get; }
        public FunctionDefinition Utf8StringConcat { get; }
        public FunctionDefinition Utf8StringCopyConstructor { get; }
        public FunctionDefinition Utf8StringRemove { get; }
        public FunctionDefinition Utf8StringLengthGetter { get; }

        public TypeDefinition Utf8StringIteratorType { get; }


        public TypeDefinition CaptureType { get; }
        public FunctionDefinition CaptureConstructor { get; }
        public TypeDefinition MatchType { get; }
        public Property MatchCapturesProperty { get; }
        public FunctionDefinition MatchConstructor { get; }
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
        public TypeDefinition IComparableType { get; }
        public TypeDefinition IObjectType { get; }
        public FunctionDefinition IObjectGetTypeFunction { get; }

        public TypeDefinition ChunkType { get; }
        public FunctionDefinition ChunkSizeConstructor { get; }
        public FunctionDefinition ChunkResizeConstructor { get; }
        public FunctionDefinition ChunkCount { get; }
        public FunctionDefinition ChunkAtGet { get; }
        public FunctionDefinition ChunkAtSet { get; }

        public TypeDefinition ArrayType { get; }
        public FunctionDefinition ArrayDefaultConstructor { get; }
        public FunctionDefinition ArrayAppendFunction { get; }

        public TypeDefinition ISequenceType { get; }
        public TypeDefinition ICountedType { get; }
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

        public static TypeDefinition JokerType { get; private set; }
        public static EntityInstance JokerInstance { get; private set; }
        public static EvaluationInfo JokerEval { get; private set; }

        private Environment(IOptions options)
        {
            this.Options = options ?? new Options();

            // we have to recreate them on each run, otherwise old joker will have the references
            // to previous test, this would prevent GC from reclaiming the memory, and after few hundred
            // tests we will be out of memory
            JokerType = null;
            JokerInstance = null;
            JokerEval = null;
            JokerType = TypeDefinition.Create(EntityModifier.None,
                NameDefinition.Create(NameFactory.JokerTypeName), null, allowSlicing: true);
            JokerInstance = JokerType.GetInstance(TypeMutability.None, TemplateTranslation.Empty, Lifetime.Timeless);
            JokerEval = EvaluationInfo.Create(JokerInstance);

            this.Root = Namespace.Create(NameFactory.RootNamespace);
            this.SystemNamespace = this.Root.AddNode(Namespace.Create(NameFactory.SystemNamespace));
            this.ConcurrencyNamespace = this.SystemNamespace.AddNode(Namespace.Create(NameFactory.ConcurrencyNamespace));
            this.CollectionsNamespace = this.SystemNamespace.AddNode(Namespace.Create(NameFactory.CollectionsNamespace));
            this.IoNamespace = this.SystemNamespace.AddNode(Namespace.Create(NameFactory.IoNamespace));
            this.TextNamespace = this.SystemNamespace.AddNode(Namespace.Create(NameFactory.TextNamespace));

            this.IObjectGetTypeFunction = FunctionBuilder.Create(NameFactory.GetTypeFunctionName, NameFactory.TypeInfoPointerNameReference(),
                    Block.CreateStatement())
                        .SetModifier(EntityModifier.Native);
            this.IObjectType = this.Root.AddBuilder(TypeBuilder.CreateInterface(NameFactory.IObjectTypeName)
                .With(IObjectGetTypeFunction));

            this.UnitType = Root.AddNode(createUnitType());

            this.UnitEvaluation = new EvaluationInfo(this.UnitType.InstanceOf);

            // pointer and reference are not of Object type (otherwise we could have common root for String and pointer to Int)
            this.ReferenceType = Root.AddBuilder(TypeBuilder.Create(NameDefinition.Create(NameFactory.ReferenceTypeName, "RFT", VarianceMode.Out))
                // todo: uncomment this when we have traits and IReplicable interface
                // .Modifier(EntityModifier.Native | EntityModifier.Base)
                .SetModifier(EntityModifier.Native)
                .Slicing(true));

            this.PointerType = Root.AddBuilder(TypeBuilder.Create(NameDefinition.Create(NameFactory.PointerTypeName, "PTT", VarianceMode.Out))
                .SetModifier(EntityModifier.Native)
                // todo: uncomment this when we have traits and IReplicable interface
                // this allows to make such override of the method
                // Parent::foo() -> Ref<T>
                // Child::foo() -> Ptr<T> 
                //.Parents(NameFactory.ReferenceNameReference("PTT")) 
                .Slicing(true));

            this.TypeInfoType = this.SystemNamespace.AddNode(createTypeInfo());

            if (this.Options.MiniEnvironment)
                return;






            {
                this.Int16Type = this.Root.AddBuilder(createNumFullBuilder(options, NameFactory.Int16TypeName,
                    Int16Literal.Create($"{Int16.MinValue}"),
                    Int16Literal.Create($"{Int16.MaxValue}"),
                    out FunctionDefinition parse_string));
                this.Int16ParseStringFunction = parse_string;
            }
            {
                this.Int64FromNat8Constructor = FunctionDefinition.CreateInitConstructor(EntityModifier.Native | EntityModifier.Implicit,
                    new[] { FunctionParameter.Create(NameFactory.SourceCopyConstructorParameter, NameFactory.Nat8NameReference(),
                        ExpressionReadMode.CannotBeRead) },
                    Block.CreateStatement());

                this.Int64Type = this.Root.AddBuilder(createNumFullBuilder(options, NameFactory.Int64TypeName,
                    Int64Literal.Create($"{Int64.MinValue}"),
                    Int64Literal.Create($"{Int64.MaxValue}"),
                    out FunctionDefinition parse_string,
                    this.Int64FromNat8Constructor));
                // todo: make it platform-dependant
                this.Root.AddNode(Alias.Create(NameFactory.IntTypeName, NameFactory.Int64NameReference(), EntityModifier.Public));
                this.Int64ParseStringFunction = parse_string;
            }
            {
                this.Nat8Type = this.Root.AddBuilder(createNumFullBuilder(options, NameFactory.Nat8TypeName,
                    Nat8Literal.Create($"{byte.MinValue}"),
                    Nat8Literal.Create($"{byte.MaxValue}"),
                    out FunctionDefinition parse_string));
                this.Nat8ParseStringFunction = parse_string;
            }
            {
                this.Nat64FromNat8Constructor = FunctionDefinition.CreateInitConstructor(EntityModifier.Native | EntityModifier.Implicit,
                    new[] { FunctionParameter.Create(NameFactory.SourceCopyConstructorParameter, NameFactory.Nat8NameReference(),
                        ExpressionReadMode.CannotBeRead) },
                    Block.CreateStatement());

                this.Nat64Type = this.Root.AddBuilder(createNumFullBuilder(options, NameFactory.Nat64TypeName,
                   Nat64Literal.Create($"{UInt64.MinValue}"),
                   Nat64Literal.Create($"{UInt64.MaxValue}"),
                   out FunctionDefinition parse_string,
                   this.Nat64FromNat8Constructor));
                // todo: make it platform-dependant
                this.Root.AddNode(Alias.Create(NameFactory.NatTypeName, NameFactory.Nat64NameReference(), EntityModifier.Public));
                this.Root.AddNode(Alias.Create(NameFactory.SizeTypeName, NameFactory.NatNameReference(), EntityModifier.Public));
                this.Nat64ParseStringFunction = parse_string;
            }

            /*this.EnumType = this.Root.AddBuilder(TypeBuilder.CreateInterface(NameFactory.EnumTypeName,EntityModifier.Native)
                            .Parents(NameFactory.ObjectNameReference(), NameFactory.EquatableNameReference()));
                */

            {
                this.Real64FromNat8Constructor = FunctionDefinition.CreateInitConstructor(EntityModifier.Native | EntityModifier.Implicit,
                    new[] { FunctionParameter.Create(NameFactory.SourceCopyConstructorParameter, NameFactory.Nat8NameReference(),
                        ExpressionReadMode.CannotBeRead) },
                    Block.CreateStatement());

                this.Real64Type = this.Root.AddNode(createRealType(options,
                    NameFactory.Real64TypeName,
                   Real64Literal.Create(Double.MinValue),
                   Real64Literal.Create(Double.MaxValue),
                   out FunctionDefinition parse_string,
                   this.Real64FromNat8Constructor));
                // todo: make it platform-dependant
                this.Root.AddNode(Alias.Create(NameFactory.RealTypeName, NameFactory.Real64NameReference(), EntityModifier.Public));
                this.Real64ParseStringFunction = parse_string;
            }
            {
                createSpreads(out FunctionDefinition spread_min, out FunctionDefinition spread_min_max);
                this.SystemNamespace.AddNode(spread_min);
                this.SystemNamespace.AddNode(spread_min_max);
            }

            this.SystemNamespace.AddNode(createStore());

            {
                FunctionDefinition read_lines;
                FunctionDefinition exists;
                this.FileType = this.IoNamespace.AddNode(createFile(readLines: out read_lines, exists: out exists));
                this.FileReadLines = read_lines;
                this.FileExists = exists;
            }

            {
                this.CaptureType = this.TextNamespace.AddNode(createCapture(options, out FunctionDefinition cap_cons));
                this.CaptureConstructor = cap_cons;
            }
            {
                this.MatchType = this.TextNamespace.AddNode(createMatch(options, out Property match_captures_prop, out FunctionDefinition match_cons));
                this.MatchCapturesProperty = match_captures_prop;
                this.MatchConstructor = match_cons;
            }
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

            this.CollectionsNamespace.AddNode(createLinq(this.Options));

            this.IIteratorType = this.CollectionsNamespace.AddNode(createIIterator());

            this.IndexIteratorType = this.CollectionsNamespace.AddNode(createIndexIterator(this.Options));

            this.ISequenceType = this.CollectionsNamespace.AddNode(createISequence());

            this.ICountedType = this.CollectionsNamespace.AddNode(createICounted(options));

            this.IIndexableType = this.CollectionsNamespace.AddNode(createIIndexable(options));

            {
                this.ChunkType = this.CollectionsNamespace.AddNode(createChunk(options,
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

            {
                this.ArrayType = this.CollectionsNamespace.AddNode(createArray(this.Options, out FunctionDefinition array_def_cons,
                    out FunctionDefinition append));
                this.ArrayDefaultConstructor = array_def_cons;
                this.ArrayAppendFunction = append;
            }

            this.IEquatableType = this.SystemNamespace.AddNode(createIEquatableType());

            this.DayOfWeek = this.SystemNamespace.AddBuilder(TypeBuilder.CreateEnum(NameFactory.DayOfWeekTypeName)
                .With(EnumCaseBuilder.Create(
                    NameFactory.SundayDayOfWeekTypeName,
                    NameFactory.MondayDayOfWeekTypeName,
                    NameFactory.TuesdayDayOfWeekTypeName,
                    NameFactory.WednesdayDayOfWeekTypeName,
                    NameFactory.ThursdayDayOfWeekTypeName,
                    NameFactory.FridayDayOfWeekTypeName,
                    NameFactory.SaturdayDayOfWeekTypeName
                    )));

            {
                this.DateType = this.SystemNamespace.AddBuilder(TypeBuilder.Create(NameFactory.DateTypeName)
                    .SetModifier(EntityModifier.Mutable)
                    .With(PropertyBuilder.CreateAutoFull(this.Options, "year", NameFactory.Int16NameReference(), out PropertyMembers year))
                    .With(PropertyBuilder.CreateAutoFull(this.Options, "month", NameFactory.Nat8NameReference(), out PropertyMembers month,
                        Nat8Literal.Create("1")))
                    .With(PropertyBuilder.CreateAutoFull(this.Options, "day", NameFactory.Nat8NameReference(), out PropertyMembers day,
                        Nat8Literal.Create("1")))
                    .With(ExpressionFactory.BasicConstructor(new[] { "year", "month", "day" },
                        new[] { NameFactory.Int16NameReference(), NameFactory.Nat8NameReference(), NameFactory.Nat8NameReference() }))
                    .With(PropertyBuilder.Create(options, NameFactory.DateDayOfWeekProperty, () => NameFactory.DayOfWeekNameReference())
                        .WithGetter(ExpressionFactory.BodyReturnUndef(), out FunctionDefinition day_of_week_getter, EntityModifier.Native))
                    );

                this.DateDayOfWeekGetter = day_of_week_getter;
                this.DateYearField = year.Field;
                this.DateMonthField = month.Field;
                this.DateDayField = day.Field;
            }

            this.CollectionsNamespace.AddNode(createConcat1());
            this.CollectionsNamespace.AddNode(createConcat3());

            this.BoolType = Root.AddNode(createBool(options));

            {
                this.CharType = Root.AddNode(createChar(options,
                    out FunctionDefinition length_getter,
                    toString: out FunctionDefinition to_string));
                this.CharLengthGetter = length_getter;
                this.CharToString = to_string;
            }
            {
                this.Utf8StringType = this.SystemNamespace.AddNode(createUtf8String(options,
                    out FunctionDefinition count_getter,
                    out FunctionDefinition length_getter,
                    out IMember at_getter,
                    out FunctionDefinition trim_start,
                    out FunctionDefinition trim_end,
                    out FunctionDefinition index_of_string,
                    out FunctionDefinition last_index_of_char,
                    out FunctionDefinition reverse,
                    slice: out FunctionDefinition slice,
                    concat: out FunctionDefinition concat,
                    copyConstructor: out FunctionDefinition copy_cons,
                    remove: out FunctionDefinition remove));
                this.Utf8StringCountGetter = count_getter;
                this.Utf8StringLengthGetter = length_getter;
                this.Utf8StringAtGetter = at_getter.Cast<FunctionDefinition>();
                this.Utf8StringTrimStart = trim_start;
                this.Utf8StringTrimEnd = trim_end;
                this.Utf8StringIndexOfString = index_of_string;
                this.Utf8StringLastIndexOfChar = last_index_of_char;
                this.Utf8StringReverse = reverse;
                this.Utf8StringSlice = slice;
                this.Utf8StringConcat = concat;
                this.Utf8StringCopyConstructor = copy_cons;
                this.Utf8StringRemove = remove;
                this.SystemNamespace.AddNode(Alias.Create(NameFactory.StringTypeName, NameFactory.Utf8StringNameReference(),
                    EntityModifier.Public));
            }

            this.Utf8StringIteratorType = this.SystemNamespace.AddNode(createUtf8StringIterator(this.Options));

            this.OrderingType = this.SystemNamespace.AddBuilder(TypeBuilder.CreateEnum(NameFactory.OrderingTypeName)
                .With(EnumCaseBuilder.Create(NameFactory.OrderingLess, NameFactory.OrderingEqual, NameFactory.OrderingGreater)));

            this.OrderingLess = this.OrderingType.NestedFields.Single(it => it.Name.Name == NameFactory.OrderingLess);
            this.OrderingEqual = this.OrderingType.NestedFields.Single(it => it.Name.Name == NameFactory.OrderingEqual);
            this.OrderingGreater = this.OrderingType.NestedFields.Single(it => it.Name.Name == NameFactory.OrderingGreater);

            this.IComparableType = this.SystemNamespace.AddNode(createIComparableType());


            this.ChannelType = this.ConcurrencyNamespace.AddNode(createChannelType());

            this.ExceptionType = this.SystemNamespace.AddBuilder(TypeBuilder.Create(NameFactory.ExceptionTypeName)
                .SetModifier(EntityModifier.HeapOnly)
                .Parents(NameFactory.IObjectNameReference()));

            {
                FunctionDefinition empty, value;
                this.OptionType = this.SystemNamespace.AddNode(createOptionType(out empty, out value));
                this.OptionEmptyConstructor = empty;
                this.OptionValueConstructor = value;
            }

            this.functionTypes = new List<TypeDefinition>();
            foreach (int param_count in Enumerable.Range(0, maxSaneArity))
                this.functionTypes.Add(createFunction(param_count));
            this.functionTypes.ForEach(it => this.Root.AddNode(it));

            {

                TypeBuilder factory_builder = TypeBuilder.Create(NameFactory.TupleTypeName)
                    .SetModifier(EntityModifier.Static);
                this.tupleTypes = new List<TypeDefinition>();
                this.iTupleTypes = new List<TypeDefinition>();
                foreach (int count in Enumerable.Range(2, maxSaneArity))
                {
                    this.tupleTypes.Add(createTuple(this.Options, count, out FunctionDefinition factory));
                    this.iTupleTypes.Add(createITuple(options, count));

                    factory_builder.With(factory);
                }
                this.tupleTypes.ForEach(it => this.CollectionsNamespace.AddNode(it));
                this.iTupleTypes.ForEach(it => this.CollectionsNamespace.AddNode(it));
                this.CollectionsNamespace.AddBuilder(factory_builder);
            }
        }

        private static TypeDefinition createICounted(IOptions options)
        {
            return TypeBuilder.CreateInterface(NameDefinition.Create(NameFactory.ICountedTypeName))

                                .With(PropertyBuilder.Create(options, NameFactory.IIterableCount, () => NameFactory.SizeNameReference())
                                    .WithGetter(body: null));
        }

        private static TypeDefinition createISequence()
        {
            return TypeBuilder.CreateInterface(NameDefinition.Create(NameFactory.ISequenceTypeName, "SQT", VarianceMode.Out))
                                .Parents(NameFactory.IIterableNameReference("SQT"))

                                    ;
        }

        private static TypeDefinition createUnitType()
        {
            return TypeBuilder.Create(NameFactory.UnitTypeName)
                .SetModifier(EntityModifier.Native)
                .With(FunctionBuilder.CreateInitConstructor(Block.CreateStatement())
                    .SetModifier(EntityModifier.Native))
                .Parents(NameFactory.IObjectNameReference())
                ;
        }

        private static TypeDefinition createIEquatableType()
        {
            return TypeBuilder.Create(NameDefinition.Create(NameFactory.IEquatableTypeName))
                                .SetModifier(EntityModifier.Interface)
                                .Parents(NameFactory.IObjectNameReference())
                                .With(FunctionBuilder.Create(NameFactory.NotEqualOperator, ExpressionReadMode.ReadRequired, NameFactory.BoolNameReference(),
                                    Block.CreateStatement(new[] {
                            Return.Create( ExpressionFactory.Not(
                                 ExpressionFactory.IsEqual(NameFactory.ThisReference(),NameReference.Create("cmp"))))
                                    }))
                                        .Parameters(FunctionParameter.Create("cmp",
                                            NameFactory.ReferenceNameReference(NameFactory.ShouldBeThisNameReference(NameFactory.IEquatableTypeName, TypeMutability.ReadOnly)))))
                                .With(FunctionBuilder.CreateDeclaration(NameFactory.EqualOperator, NameFactory.BoolNameReference())
                                    .SetModifier(EntityModifier.Pinned)
                                        .Parameters(FunctionParameter.Create("cmp",
                                            NameFactory.ReferenceNameReference(NameFactory.ShouldBeThisNameReference(NameFactory.IEquatableTypeName, TypeMutability.ReadOnly)))));
        }

        private FunctionDefinition createStore()
        {
            return FunctionBuilder.Create(NameFactory.StoreFunctionName, "T", VarianceMode.None,
               NameFactory.ReferenceNameReference(NameFactory.ISequenceNameReference("T", mutability: TypeMutability.ReadOnly)),
               Block.CreateStatement(
                    VariableDeclaration.CreateStatement("opt_coll", null, ExpressionFactory.DownCast(NameReference.Create("coll"),
                        NameFactory.ReferenceNameReference(NameFactory.ISequenceNameReference("T", TypeMutability.ReadOnly)))),

                    VariableDeclaration.CreateStatement("seq_coll", null,
                         ExpressionFactory.OptionCoalesce(NameReference.Create("opt_coll"),
                             ExpressionFactory.HeapConstructor(NameFactory.ArrayNameReference("T", TypeMutability.ReadOnly),
                                NameReference.Create("coll")))),

                    Return.Create(NameReference.Create("seq_coll"))))
              .Parameters(FunctionParameter.Create("coll", NameFactory.ReferenceNameReference(NameFactory.IIterableNameReference("T",
                        overrideMutability: TypeMutability.ReadOnly))));

        }

        private void createSpreads(out FunctionDefinition spreadMin, out FunctionDefinition spreadMinMax)
        {
            // with min limit
            spreadMin = FunctionBuilder.Create(NameFactory.SpreadFunctionName, "T", VarianceMode.None,
               NameFactory.ReferenceNameReference(NameFactory.ISequenceNameReference("T", mutability: TypeMutability.ReadOnly)),
               Block.CreateStatement(

                   IfBranch.CreateIf(ExpressionFactory.IsLess(FunctionCall.Create(NameReference.Create("coll", NameFactory.IIterableCount)),
                        NameReference.Create("min")), new[] { ExpressionFactory.GenericThrow() }),
                        Return.Create(NameReference.Create("coll"))
                ))
                .Parameters(FunctionParameter.Create("coll", NameFactory.ReferenceNameReference(NameFactory.ISequenceNameReference("T",
                            mutability: TypeMutability.ReadOnly))),
                        FunctionParameter.Create("min", NameFactory.SizeNameReference()))
                .Include(NameFactory.LinqExtensionReference());

            // with min+max limit
            spreadMinMax = FunctionBuilder.Create(NameFactory.SpreadFunctionName, "T", VarianceMode.None,
                NameFactory.ReferenceNameReference(NameFactory.ISequenceNameReference("T", mutability: TypeMutability.ReadOnly)),
                Block.CreateStatement(
                   VariableDeclaration.CreateStatement("count", null,
                     FunctionCall.Create(NameReference.Create("coll", NameFactory.IIterableCount))),
                   IfBranch.CreateIf(ExpressionFactory.IsLess(NameReference.Create("count"),
                        NameReference.Create("min")), new[] { ExpressionFactory.GenericThrow() }),
                   IfBranch.CreateIf(ExpressionFactory.IsGreaterEqual(NameReference.Create("count"),
                        NameReference.Create("max")), new[] { ExpressionFactory.GenericThrow() }),
                                      Return.Create(NameReference.Create("coll"))
                ))
                .Parameters(FunctionParameter.Create("coll", NameFactory.ReferenceNameReference(NameFactory.ISequenceNameReference("T",
                            mutability: TypeMutability.ReadOnly))),
                        FunctionParameter.Create("min", NameFactory.SizeNameReference()),
                        FunctionParameter.Create("max", NameFactory.SizeNameReference()))
                .Include(NameFactory.LinqExtensionReference());
        }

        private TypeDefinition createTypeInfo()
        {
            return TypeBuilder.Create(NameFactory.TypeInfoTypeName)
                .SetModifier(EntityModifier.HeapOnly);
        }

        private TypeDefinition createCapture(IOptions options, out FunctionDefinition captureConstructor)
        {
            TypeMutability mutability_override = this.Options.AtomicPrimitivesMutable ? TypeMutability.ForceConst : TypeMutability.None;

            captureConstructor = ExpressionFactory.BasicConstructor(new[] {
                        NameFactory.CaptureStartFieldName,
                        NameFactory.CaptureEndFieldName,
                  //      NameFactory.CaptureIdFieldName,
                        NameFactory.CaptureNameFieldName
                    },
                    new[] {
                        NameFactory.SizeNameReference(mutability_override),
                        NameFactory.SizeNameReference(mutability_override),
                    //    NameFactory.SizeNameReference(),
                        NameFactory.OptionNameReference(NameFactory.StringPointerNameReference(TypeMutability.ForceConst))
                    });


            return TypeBuilder.Create(NameFactory.CaptureTypeName)
                .With(PropertyBuilder.CreateAutoGetter(options, NameFactory.CaptureStartFieldName, NameFactory.SizeNameReference(mutability_override), Undef.Create()))
                .With(PropertyBuilder.CreateAutoGetter(options, NameFactory.CaptureEndFieldName, NameFactory.SizeNameReference(mutability_override), Undef.Create()))
                //.With(PropertyBuilder.CreateAutoGetter(NameFactory.CaptureIdFieldName, NameFactory.SizeNameReference(), Undef.Create()))
                .With(PropertyBuilder.CreateAutoGetter(options, NameFactory.CaptureNameFieldName,
                    NameFactory.OptionNameReference(NameFactory.StringPointerNameReference(TypeMutability.ForceConst)), Undef.Create()))
                .With(captureConstructor)
                    ;
        }

        private TypeDefinition createMatch(IOptions options, out Property matchCapturesProp, out FunctionDefinition matchConstructor)
        {
            TypeMutability mutability_override = this.Options.AtomicPrimitivesMutable ? TypeMutability.ForceConst : TypeMutability.None;

            Property index_prop = PropertyBuilder.CreateAutoGetter(options, NameFactory.MatchStartFieldName, NameFactory.SizeNameReference(mutability_override),
                Undef.Create());
            Property count_prop = PropertyBuilder.CreateAutoGetter(options, NameFactory.MatchEndFieldName, NameFactory.SizeNameReference(mutability_override),
                Undef.Create());
            matchCapturesProp = PropertyBuilder.CreateAutoGetter(options, NameFactory.MatchCapturesFieldName,
                    NameFactory.PointerNameReference(NameFactory.ArrayNameReference(NameFactory.CaptureNameReference(), TypeMutability.ForceConst)),
                        Undef.Create());

            // using constructor with direct initialization of the fields, not properties (no setters anyway)
            matchConstructor = ExpressionFactory.BasicConstructor(
                new[] {
                        NameFactory.MatchStartFieldName,
                        NameFactory.MatchEndFieldName,
                        NameFactory.MatchCapturesFieldName
                    },
                    new[] {
                        NameFactory.SizeNameReference(mutability_override),
                        NameFactory.SizeNameReference(mutability_override),
                        NameFactory.PointerNameReference( NameFactory.ArrayNameReference(NameFactory.CaptureNameReference(),
                            TypeMutability.ForceConst))
                    });
            return TypeBuilder.Create(NameFactory.MatchTypeName)
                .With(index_prop)
                .With(count_prop)
                .With(matchCapturesProp)
                .With(matchConstructor)
                    ;
        }

        private TypeDefinition createRegex(out VariableDeclaration pattern, out FunctionDefinition contains, out FunctionDefinition match)
        {
            pattern = VariableDeclaration.CreateStatement(NameFactory.RegexPatternFieldName,
                    NameFactory.StringPointerNameReference(TypeMutability.ForceConst),
                    Undef.Create(), EntityModifier.Native);

            contains = FunctionBuilder.Create(NameFactory.RegexContainsFunctionName, NameFactory.BoolNameReference(), Block.CreateStatement())
                    .SetModifier(EntityModifier.Native)
                    .Parameters(FunctionParameter.Create("input", NameFactory.StringPointerNameReference(TypeMutability.ReadOnly),
                        ExpressionReadMode.CannotBeRead));

            match = FunctionBuilder.Create(NameFactory.RegexMatchFunctionName,
                NameFactory.PointerNameReference(NameFactory.IIterableNameReference(NameFactory.MatchNameReference())),
                Block.CreateStatement())
                    .SetModifier(EntityModifier.Native)
                    .Parameters(FunctionParameter.Create("input", NameFactory.StringPointerNameReference(TypeMutability.ReadOnly),
                        ExpressionReadMode.CannotBeRead));

            return TypeBuilder.Create(NameFactory.RegexTypeName)
                .With(pattern)
                .With(ExpressionFactory.BasicConstructor(new[] {
                        NameFactory.RegexPatternFieldName,
                    },
                    new[] {
                        NameFactory.StringPointerNameReference(TypeMutability.ForceConst),
                    }))
                .With(contains)
                .With(match)
                    ;
        }

        private static TypeDefinition createChar(IOptions options, out FunctionDefinition lengthGetter, out FunctionDefinition toString)
        {
            Property length_property = PropertyBuilder.Create(options, NameFactory.CharLength, () => NameFactory.Nat8NameReference())
                .With(PropertyMemberBuilder.CreateGetter(Block.CreateStatement())
                    .Modifier(EntityModifier.Native));

            lengthGetter = length_property.Getter;

            toString = FunctionBuilder.Create(NameFactory.ConvertFunctionName, NameFactory.Utf8StringPointerNameReference(),
                Block.CreateStatement(Return.Create(Undef.Create())))
                .SetModifier(EntityModifier.Native);

            EntityModifier modifier = EntityModifier.Native;
            if (options.AtomicPrimitivesMutable)
                modifier |= EntityModifier.Mutable;
            else
                modifier |= EntityModifier.Const;

            return TypeBuilder.Create(NameFactory.CharTypeName)
                            .Parents(NameFactory.IComparableNameReference())
                            .SetModifier(modifier)
                            .With(FunctionDefinition.CreateInitConstructor(EntityModifier.Native, null, Block.CreateStatement()))
                            .With(FunctionDefinition.CreateInitConstructor(EntityModifier.Native,
                                new[] { FunctionParameter.Create(NameFactory.SourceCopyConstructorParameter, NameFactory.ItNameReference(),
                                    ExpressionReadMode.CannotBeRead) },
                                Block.CreateStatement()))

                                .With(length_property)

                                .With(toString)

                                .WithComparableCompare()
                            .With(FunctionBuilder.Create(NameFactory.ComparableCompare,
                                ExpressionReadMode.ReadRequired, NameFactory.OrderingNameReference(),
                                Block.CreateStatement())
                                .SetModifier(EntityModifier.Native)
                                .Parameters(FunctionParameter.Create("cmp", NameFactory.ItNameReference(TypeMutability.ReadOnly),
                                    ExpressionReadMode.CannotBeRead)))
                            ;
            ;
        }

        private TypeDefinition createUtf8String(IOptions options, out FunctionDefinition countGetter, out FunctionDefinition lengthGetter,
            out IMember atGetter, out FunctionDefinition trimStart, out FunctionDefinition trimEnd,
            out FunctionDefinition indexOfString, out FunctionDefinition lastIndexOfChar, out FunctionDefinition reverse,
            out FunctionDefinition slice, out FunctionDefinition concat, out FunctionDefinition copyConstructor,
            out FunctionDefinition remove)
        {
            Property count_property = PropertyBuilder.Create(options, NameFactory.IIterableCount, () => NameFactory.SizeNameReference())
                .With(PropertyMemberBuilder.CreateGetter(Block.CreateStatement())
                    .Modifier(EntityModifier.Native | EntityModifier.Override));
            Property length_property = PropertyBuilder.Create(options, NameFactory.StringLength, () => NameFactory.SizeNameReference())
                .With(PropertyMemberBuilder.CreateGetter(Block.CreateStatement())
                    .Modifier(EntityModifier.Native));

            countGetter = count_property.Getter;
            lengthGetter = length_property.Getter;

            copyConstructor = FunctionBuilder.CreateInitConstructor(Block.CreateStatement())
                .SetModifier(EntityModifier.Native)
                .Parameters(FunctionParameter.Create("source", NameFactory.Utf8StringPointerNameReference(TypeMutability.ReadOnly),
                    ExpressionReadMode.CannotBeRead));

            trimStart = FunctionBuilder.Create(NameFactory.StringTrimStart,
                NameFactory.UnitNameReference(),
                Block.CreateStatement())
                .SetModifier(EntityModifier.Native | EntityModifier.Mutable);
            trimEnd = FunctionBuilder.Create(NameFactory.StringTrimEnd,
                NameFactory.UnitNameReference(),
                Block.CreateStatement())
                .SetModifier(EntityModifier.Native | EntityModifier.Mutable);
            concat = FunctionBuilder.Create(NameFactory.StringConcat,
                NameFactory.UnitNameReference(),
                Block.CreateStatement())
            .Parameters(FunctionParameter.Create("str", NameFactory.StringPointerNameReference(TypeMutability.ReadOnly),
                ExpressionReadMode.CannotBeRead))
                .SetModifier(EntityModifier.Native | EntityModifier.Mutable);

            indexOfString = FunctionBuilder.Create(NameFactory.StringIndexOf, NameFactory.OptionNameReference(NameFactory.SizeNameReference()),
                Block.CreateStatement(Return.Create(Undef.Create())))
                .Parameters(FunctionParameter.Create("str", NameFactory.StringPointerNameReference(TypeMutability.ReadOnly), ExpressionReadMode.CannotBeRead),
                    FunctionParameter.Create("index", NameFactory.SizeNameReference(), Variadic.None, NatLiteral.Create("0"), false, ExpressionReadMode.CannotBeRead))
                .SetModifier(EntityModifier.Native);
            // todo: change it to work on string instead, as indexOf
            lastIndexOfChar = FunctionBuilder.Create(NameFactory.StringLastIndexOf, NameFactory.OptionNameReference(NameFactory.SizeNameReference()),
                Block.CreateStatement(Return.Create(Undef.Create())))
                .Parameters(FunctionParameter.Create("ch", NameFactory.CharNameReference(), ExpressionReadMode.CannotBeRead),
                    FunctionParameter.Create("index1", NameFactory.SizeNameReference(), ExpressionReadMode.CannotBeRead))
                .SetModifier(EntityModifier.Native);

            reverse = FunctionBuilder.Create(NameFactory.StringReverse,
                NameFactory.UnitNameReference(),
                Block.CreateStatement())
                .SetModifier(EntityModifier.Native | EntityModifier.Mutable);
            slice = FunctionBuilder.Create(NameFactory.StringSlice,
                NameFactory.Utf8StringPointerNameReference(),
                Block.CreateStatement(Return.Create(Undef.Create())))
                .Parameters(FunctionParameter.Create("start", NameFactory.SizeNameReference(), ExpressionReadMode.CannotBeRead),
                    FunctionParameter.Create("end", NameFactory.SizeNameReference(), ExpressionReadMode.CannotBeRead))
                .SetModifier(EntityModifier.Native);

            FunctionDefinition split = FunctionBuilder.Create(NameFactory.StringSplit,
                                NameFactory.PointerNameReference(NameFactory.IIterableNameReference(NameFactory.Utf8StringPointerNameReference())),
                            Block.CreateStatement(
                                VariableDeclaration.CreateStatement("buffer", null,
                                     ExpressionFactory.HeapConstructor(NameFactory.ArrayNameReference(NameFactory.StringPointerNameReference()))),
                                VariableDeclaration.CreateStatement("start", null, NatLiteral.Create("0"), this.Options.ReassignableModifier()),

                                Loop.CreateWhile(ExpressionFactory.And(
                                     ExpressionFactory.IsNotEqual(NatLiteral.Create("0"), NameReference.Create("limit"))
                                    ,
                                     ExpressionFactory.OptionalDeclaration("idx", null,
                                        FunctionCall.Create(NameReference.CreateThised(NameFactory.StringIndexOf),
                                            NameReference.Create("separator"), NameReference.Create("start")))),

                                        new IExpression[] {
                                             ExpressionFactory.Dec("limit"),

                                        FunctionCall.Create(NameReference.Create( "buffer",NameFactory.AppendFunctionName),
                                            FunctionCall.Create(NameReference.CreateThised(NameFactory.StringSlice),
                                            NameReference.Create("start"),
                                            NameReference.Create( "idx"))),

                                        Assignment.CreateStatement(NameReference.Create("start"),
                                             ExpressionFactory.Add(NameReference.Create("idx"),
                                            NameReference.Create("separator",NameFactory.StringLength)))
                                    }),

                                    FunctionCall.Create(NameReference.Create("buffer", NameFactory.AppendFunctionName),
                                        FunctionCall.Create(NameReference.CreateThised(NameFactory.StringSlice),
                                            NameReference.Create("start"))),

                                    Return.Create(NameReference.Create("buffer"))

                                ))
                            .Parameters(FunctionParameter.Create("separator",
                                NameFactory.StringPointerNameReference(TypeMutability.ReadOnly)),
                                // limit of the splits, not parts!
                                FunctionParameter.Create("limit", NameFactory.SizeNameReference(), Variadic.None,
                                    NameReference.Create(NameFactory.SizeNameReference(), NameFactory.NumMaxValueName),
                                        isNameRequired: false, modifier: this.Options.ReassignableModifier()));

            remove = FunctionBuilder.Create(NameFactory.StringRemove,
                NameFactory.UnitNameReference(),
                            Block.CreateStatement())
                            .Parameters(FunctionParameter.Create("start", NameFactory.SizeNameReference(), ExpressionReadMode.CannotBeRead),
                                FunctionParameter.Create("end", NameFactory.SizeNameReference(), ExpressionReadMode.CannotBeRead))
                             .SetModifier(EntityModifier.Mutable | EntityModifier.Native);



            TypeBuilder builder = TypeBuilder.Create(NameFactory.Utf8StringTypeName)
                                .SetModifier(EntityModifier.HeapOnly | EntityModifier.Native | EntityModifier.Mutable)
                                .Parents(NameFactory.IComparableNameReference(),
                                    NameFactory.ICountedNameReference(),
                                    NameFactory.ISequenceNameReference(NameFactory.CharNameReference()))

                                .With(copyConstructor)

                                .With(count_property)
                                .With(length_property)

                     .With(PropertyBuilder.CreateIndexer(options, NameFactory.ReferenceNameReference(NameFactory.CharNameReference()))
                        .Parameters(FunctionParameter.Create(NameFactory.IndexIndexerParameter, NameFactory.SizeNameReference(),
                            ExpressionReadMode.CannotBeRead))
                        .With(PropertyMemberBuilder.CreateIndexerGetter(Block.CreateStatement())
                            .Modifier(EntityModifier.Native | EntityModifier.Override), out atGetter))

                .With(FunctionBuilder.Create(NameFactory.IterableGetIterator,
                    NameFactory.ReferenceNameReference(NameFactory.IIteratorNameReference(NameFactory.CharNameReference())),
                    Block.CreateStatement(
                        Return.Create(ExpressionFactory.HeapConstructor(NameFactory.Utf8StringIteratorNameReference(),
                            NameFactory.ThisReference()))))
                    .SetModifier(EntityModifier.Override))


                                .WithComparableCompare()
                            .With(FunctionBuilder.Create(NameFactory.ComparableCompare,
                                ExpressionReadMode.ReadRequired, NameFactory.OrderingNameReference(),
                                Block.CreateStatement())
                                .SetModifier(EntityModifier.Native)
                                .Parameters(FunctionParameter.Create("cmp",
                                    NameFactory.ReferenceNameReference(NameFactory.ItNameReference(TypeMutability.ReadOnly)),
                                    ExpressionReadMode.CannotBeRead)))

                                    .With(concat)

                                    .With(indexOfString)
                                    .With(FunctionBuilder.Create(NameFactory.StringIndexOf, NameFactory.OptionNameReference(NameFactory.SizeNameReference()),
                Block.CreateStatement(Return.Create(FunctionCall.Create(NameReference.CreateThised(NameFactory.StringIndexOf),
                    FunctionCall.ConvCall(NameReference.Create("ch"), NameFactory.StringPointerNameReference()),
                    NameReference.Create("index")
                ))))
                .Parameters(FunctionParameter.Create("ch", NameFactory.CharNameReference()),
                    FunctionParameter.Create("index", NameFactory.SizeNameReference(), Variadic.None, NatLiteral.Create("0"))))

                    .With(lastIndexOfChar)

                    .With(FunctionBuilder.Create(NameFactory.StringLastIndexOf, NameFactory.OptionNameReference(NameFactory.SizeNameReference()),
                                        Block.CreateStatement(Return.Create(
                                            FunctionCall.Create(NameReference.CreateThised(NameFactory.StringLastIndexOf),
                                        NameReference.Create("ch"),
                                        NameReference.CreateThised(NameFactory.StringLength)))))
                            .Parameters(FunctionParameter.Create("ch", NameFactory.CharNameReference())))

                            .With(reverse)
                            .With(split)

                            .With(remove)
                                    .With(FunctionBuilder.Create(NameFactory.StringRemove,
                                    NameFactory.UnitNameReference(),
                Block.CreateStatement(
                        FunctionCall.Create(NameReference.CreateThised(NameFactory.MutableName(NameFactory.StringRemove)),
                    NameReference.Create("start"),
                    NameReference.CreateThised(NameFactory.StringLength))))
                .Parameters(FunctionParameter.Create("start", NameFactory.SizeNameReference()))
                .SetModifier(EntityModifier.Mutable))

                            .With(slice)
                                    .With(FunctionBuilder.Create(NameFactory.StringSlice,
                                    NameFactory.Utf8StringPointerNameReference(),
                Block.CreateStatement(Return.Create(FunctionCall.Create(NameReference.CreateThised(NameFactory.StringSlice),
                    NameReference.Create("start"),
                    NameReference.CreateThised(NameFactory.StringLength)))))
                .Parameters(FunctionParameter.Create("start", NameFactory.SizeNameReference())))

                                    .With(trimStart)
                                    .With(trimEnd)
                            .With(FunctionBuilder.Create(NameFactory.StringTrim,
                                NameFactory.UnitNameReference(),
                                Block.CreateStatement(
                                        FunctionCall.Create(NameReference.CreateThised(NameFactory.MutableName(NameFactory.StringTrimStart))),

                                        FunctionCall.Create(NameReference.CreateThised(NameFactory.MutableName(NameFactory.StringTrimEnd)))

                                    )).SetModifier(EntityModifier.Mutable))
                                ;

            withComparableFunctions(builder);

            return builder;
        }

        private TypeDefinition createFile(out FunctionDefinition readLines, out FunctionDefinition exists)
        {
            readLines = FunctionBuilder.Create(NameFactory.FileReadLines,
                    NameFactory.OptionNameReference(NameFactory.PointerNameReference(NameFactory.IIterableNameReference(NameFactory.StringPointerNameReference()))),
                    Block.CreateStatement())
                      .Parameters(FunctionParameter.Create(NameFactory.FileFilePathParameter,
                            NameFactory.StringPointerNameReference(), ExpressionReadMode.CannotBeRead))
                      .SetModifier(EntityModifier.Native);
            exists = FunctionBuilder.Create(NameFactory.FileExists, NameFactory.BoolNameReference(),
                    Block.CreateStatement())
                      .Parameters(FunctionParameter.Create(NameFactory.FileFilePathParameter,
                            NameFactory.StringPointerNameReference(), ExpressionReadMode.CannotBeRead))
                      .SetModifier(EntityModifier.Native);

            TypeBuilder builder = TypeBuilder.Create(NameFactory.FileTypeName)
                .SetModifier(EntityModifier.Static)
                .With(readLines)
                .With(exists)
                ;

            return builder;
        }

        private static FunctionDefinition createConcat1()
        {
            // todo: after adding parser rewrite it as creating Concat type holding fragments and iterating over their elements
            // this (below) has too many allocations

            // value based version, since we don't need any common type here
            const string elem_type = "CCT";
            const string buffer_name = "buffer";
            const string coll1_name = "coll1";
            const string coll2_name = "coll2";
            const string elem_name = "cat1_elem";
            return FunctionBuilder.Create(NameFactory.ConcatFunctionName, elem_type, VarianceMode.None,
                NameFactory.PointerNameReference(NameFactory.IIterableNameReference(elem_type, TypeMutability.ReadOnly)),
                Block.CreateStatement(
                    VariableDeclaration.CreateStatement(buffer_name, null,
                         ExpressionFactory.HeapConstructor(NameFactory.ArrayNameReference(elem_type), NameReference.Create(coll1_name))),
                    Loop.CreateForEach(elem_name, NameReference.Create(elem_type), NameReference.Create(coll2_name), new[] {
                        FunctionCall.Create(NameReference.Create(buffer_name,NameFactory.AppendFunctionName),NameReference.Create(elem_name))
                    }),
                    Return.Create(NameReference.Create(buffer_name))
                    ))
                    .Parameters(FunctionParameter.Create(coll1_name,
                            NameFactory.ReferenceNameReference(NameFactory.IIterableNameReference(elem_type, TypeMutability.ReadOnly))),
                        FunctionParameter.Create(coll2_name,
                            NameFactory.ReferenceNameReference(NameFactory.IIterableNameReference(elem_type, TypeMutability.ReadOnly))));
        }
        private static FunctionDefinition createConcat3()
        {
            // todo: after adding parser rewrite it as creating Concat type holding fragments and iterating over their elements
            // this (below) has too many allocations

            // since we need to compute and use common type of arguments we work on pointers
            const string elem1_type = "CCA";
            const string elem2_type = "CCB";
            const string elem3_type = "CCC";
            const string buffer_name = "buffer";
            const string coll1_name = "coll1";
            const string coll2_name = "coll2";
            const string elem_name = "cat3_elem";
            return FunctionBuilder.Create(NameFactory.ConcatFunctionName,
                TemplateParametersBuffer.Create(elem1_type, elem2_type, elem3_type).Values,
                NameFactory.PointerNameReference(NameFactory.IIterableNameReference(NameFactory.PointerNameReference(elem3_type),
                    TypeMutability.ReadOnly)),
                Block.CreateStatement(
                    VariableDeclaration.CreateStatement(buffer_name, null,
                         ExpressionFactory.HeapConstructor(NameFactory.ArrayNameReference(NameFactory.PointerNameReference(elem3_type)),
                            NameReference.Create(coll1_name))),

                    Loop.CreateForEach(elem_name,
                        null,
                        NameReference.Create(coll2_name), new[] {
                        FunctionCall.Create(NameReference.Create(buffer_name,NameFactory.AppendFunctionName),NameReference.Create(elem_name))
                    }),
                    Return.Create(NameReference.Create(buffer_name))
                    ))
                .Constraints(ConstraintBuilder.Create(elem1_type).SetModifier(EntityModifier.HeapOnly),
                    ConstraintBuilder.Create(elem2_type).SetModifier(EntityModifier.HeapOnly),
                    ConstraintBuilder.Create(elem3_type).BaseOf(elem1_type, elem2_type).SetModifier(EntityModifier.HeapOnly))
                .Parameters(FunctionParameter.Create(coll1_name,
                    NameFactory.ReferenceNameReference(NameFactory.IIterableNameReference(NameFactory.PointerNameReference(elem1_type),
                        TypeMutability.ReadOnly))),
                    FunctionParameter.Create(coll2_name,
                        NameFactory.ReferenceNameReference(NameFactory.IIterableNameReference(NameFactory.PointerNameReference(elem2_type),
                            TypeMutability.ReadOnly))));
        }

        private static TypeDefinition createIIterable()
        {
            const string elem_type = "ITBT";

            return TypeBuilder.CreateInterface(NameDefinition.Create(NameFactory.IIterableTypeName, elem_type, VarianceMode.Out))
                                .Parents(NameFactory.IObjectNameReference())
                                .With(FunctionBuilder.CreateDeclaration(NameFactory.PropertyIndexerName, ExpressionReadMode.ReadRequired,
                                    NameFactory.ReferenceNameReference(NameReference.Create(elem_type)))
                                    .Parameters(FunctionParameter.Create(NameFactory.IndexIndexerParameter, NameFactory.SizeNameReference())))

                .With(FunctionBuilder.CreateDeclaration(NameFactory.IterableGetIterator,
                    NameFactory.ReferenceNameReference(NameFactory.IIteratorNameReference(elem_type))))
                 ;
        }

        private static Extension createLinq(IOptions options)
        {
            // instead of having the same type name we use different one per each function for easier debugging of compiler
            Func<string, string> elem_type = func_id => $"{func_id}LQT";
            const string this_name = "_this_";
            Func<string,FunctionParameter> this_param = func_id => FunctionParameter.Create(this_name,
                NameFactory.ReferenceNameReference(NameFactory.IIterableNameReference(elem_type(func_id), TypeMutability.ReadOnly)),
                EntityModifier.This);
            Func<NameReference> this_ref = () => NameReference.Create(this_name);

            FunctionDefinition map_func;
            {
                const string func_id = "M";
                const string map_type = "MPT";
                const string buffer_name = "buffer";
                const string mapper_name = "mapper";
                const string elem_name = "map_elem";
                map_func = FunctionBuilder.Create(
                    NameFactory.MapFunctionName, TemplateParametersBuffer.Create(elem_type(func_id), map_type).Values,
                    NameFactory.PointerNameReference(NameFactory.IIterableNameReference(map_type, TypeMutability.ReadOnly)),
                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement(buffer_name, null,
                             ExpressionFactory.HeapConstructor(NameFactory.ArrayNameReference(map_type))),
                        Loop.CreateForEach(elem_name, NameReference.Create(elem_type(func_id)), this_ref(), new[] {
                        FunctionCall.Create(NameReference.Create(buffer_name,NameFactory.AppendFunctionName),
                            FunctionCall.Create(NameReference.Create(mapper_name), NameReference.Create(elem_name)))
                        }),
                        Return.Create(NameReference.Create(buffer_name))
                        ))
                        .Parameters(this_param(func_id),
                            FunctionParameter.Create(mapper_name,
                                NameFactory.ReferenceNameReference(NameFactory.IFunctionNameReference(
                                     NameReference.Create(elem_type(func_id)), NameReference.Create(map_type)))));
            }

            FunctionDefinition reverse_func;
            {
                const string func_id = "R";
                // this is ineffective, but until having regular syntax it makes no sense to write sth for speed here
                const string buffer_name = "buffer";
                const string elem_name = "map_elem";
                reverse_func = FunctionBuilder.Create(
                    NameFactory.ReverseFunctionName, TemplateParametersBuffer.Create(elem_type(func_id)).Values,
                    NameFactory.PointerNameReference(NameFactory.IIterableNameReference(elem_type(func_id), TypeMutability.ReadOnly)),
                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("cnt", null,
                            FunctionCall.Create(NameReference.Create(this_ref(), NameFactory.IIterableCount)),
                            options.ReassignableModifier()),
                        VariableDeclaration.CreateStatement(buffer_name, null,
                             ExpressionFactory.HeapConstructor(NameFactory.ArrayNameReference(elem_type(func_id)),
                            NameReference.Create("cnt"))),
                        Loop.CreateForEach(elem_name, NameReference.Create(elem_type(func_id)), this_ref(), new[] {
                             ExpressionFactory.Dec("cnt"),
                            Assignment.CreateStatement( FunctionCall.Indexer(NameReference.Create(buffer_name),NameReference.Create("cnt")),
                                NameReference.Create(elem_name))
                        }),
                        Return.Create(NameReference.Create(buffer_name))
                        ))
                        .Parameters(this_param(func_id));
            }

            FunctionDefinition filter_func;
            {
                const string func_id = "F";
                const string buffer_name = "buffer";
                const string pred_name = "pred";
                const string elem_name = "filtered_elem";
                filter_func = FunctionBuilder.Create(
                    NameFactory.FilterFunctionName, elem_type(func_id), VarianceMode.None,
                    NameFactory.PointerNameReference(NameFactory.IIterableNameReference(elem_type(func_id), TypeMutability.ReadOnly)),
                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement(buffer_name, null,
                             ExpressionFactory.HeapConstructor(NameFactory.ArrayNameReference(elem_type(func_id)))),
                        Loop.CreateForEach(elem_name, NameReference.Create(elem_type(func_id)), this_ref(), new[] {
                            IfBranch.CreateIf(FunctionCall.Create(NameReference.Create(pred_name),NameReference.Create(elem_name)),new[]{
                                FunctionCall.Create(NameReference.Create(buffer_name,NameFactory.AppendFunctionName),
                                    NameReference.Create(elem_name))
                            })
                        }),
                        Return.Create(NameReference.Create(buffer_name))
                        ))
                        .Parameters(this_param(func_id),
                            FunctionParameter.Create(pred_name,
                                NameFactory.ReferenceNameReference(NameFactory.IFunctionNameReference(
                                     NameReference.Create(elem_type(func_id)), NameFactory.BoolNameReference()))));
            }
            FunctionDefinition any_func;
            {
                const string func_id = "AN";
                const string pred_name = "pred";
                const string elem_name = "any_elem";
                any_func = FunctionBuilder.Create(
                    NameFactory.AnyFunctionName, elem_type(func_id), VarianceMode.None,
                    NameFactory.BoolNameReference(),
                    Block.CreateStatement(
                        Loop.CreateForEach(elem_name, NameReference.Create(elem_type(func_id)), this_ref(), new[] {
                            IfBranch.CreateIf(FunctionCall.Create(NameReference.Create(pred_name),NameReference.Create(elem_name)),new[]{
                                Return.Create(BoolLiteral.CreateTrue())
                            })
                        }),
                                Return.Create(BoolLiteral.CreateFalse())
                        ))
                        .Parameters(this_param(func_id),
                            FunctionParameter.Create(pred_name,
                                NameFactory.ReferenceNameReference(NameFactory.IFunctionNameReference(
                                     NameReference.Create(elem_type(func_id)), NameFactory.BoolNameReference()))));
            }
            FunctionDefinition all_func;
            {
                const string func_id = "AL";
                const string pred_name = "pred";
                const string elem_name = "all_elem";
                all_func = FunctionBuilder.Create(
                    NameFactory.AllFunctionName, elem_type(func_id), VarianceMode.None,
                    NameFactory.BoolNameReference(),
                    Block.CreateStatement(
                        Loop.CreateForEach(elem_name, NameReference.Create(elem_type(func_id)), this_ref(), new[] {
                            IfBranch.CreateIf( ExpressionFactory.Not( FunctionCall.Create(NameReference.Create(pred_name),NameReference.Create(elem_name))),new[]{
                                Return.Create(BoolLiteral.CreateFalse())
                            })
                        }),
                                Return.Create(BoolLiteral.CreateTrue())
                        ))
                        .Parameters(this_param(func_id),
                            FunctionParameter.Create(pred_name,
                                NameFactory.ReferenceNameReference(NameFactory.IFunctionNameReference(
                                     NameReference.Create(elem_type(func_id)), NameFactory.BoolNameReference()))));
            }

            FunctionDefinition count_func;
            {
                const string func_id = "C";
                const string count_name = "cnt";
                string elem_name = NameFactory.Sink;

                count_func = FunctionBuilder.Create(
                    NameFactory.IIterableCount, elem_type(func_id), VarianceMode.None,
                    NameFactory.SizeNameReference(),
                    Block.CreateStatement(

                        IfBranch.CreateIf(ExpressionFactory.OptionalDeclaration("counted", null,
                             ExpressionFactory.DownCast(this_ref(),
                                rhsTypeName: NameFactory.ReferenceNameReference(NameFactory.ICountedNameReference(TypeMutability.ReadOnly)))),
                            Return.Create(NameReference.Create("counted", NameFactory.IIterableCount))),

                        VariableDeclaration.CreateStatement(count_name, null, NatLiteral.Create("0"),
                            options.ReassignableModifier()),
                        Loop.CreateForEach(elem_name, NameReference.Create(elem_type(func_id)), this_ref(), new[] {
                             ExpressionFactory.Inc(count_name)
                        }),
                        Return.Create(NameReference.Create(count_name))
                        ))
                        .Parameters(this_param(func_id))
                        ;
            }

            Extension ext = Extension.Create(NameFactory.LinqExtensionName);
            ext.AddNode(filter_func);
            ext.AddNode(map_func);
            ext.AddNode(count_func);
            ext.AddNode(reverse_func);
            ext.AddNode(all_func);
            ext.AddNode(any_func);

            return ext;
        }

        private static TypeDefinition createArray(IOptions options, out FunctionDefinition defaultConstructor, out FunctionDefinition append)
        {
            const string elem_type = "ART";
            const string data_field = "data";

            Property count_property = PropertyBuilder.Create(options, NameFactory.IIterableCount, () => NameFactory.SizeNameReference())
                        .WithAutoField(NatLiteral.Create("0"), options.ReassignableModifier())
                        .WithAutoSetter(EntityModifier.Private)
                        .WithAutoGetter(EntityModifier.Override);

            PropertyMemberBuilder indexer_getter_builder = PropertyMemberBuilder.CreateIndexerGetter(Block.CreateStatement(
                            Return.Create(FunctionCall.Indexer(NameReference.CreateThised(data_field),
                                NameFactory.IndexIndexerReference()))
                            ))
                            .Modifier(EntityModifier.Override);

            append = FunctionBuilder.Create(NameFactory.AppendFunctionName, NameFactory.UnitNameReference(),
                Block.CreateStatement(
                    VariableDeclaration.CreateStatement("pos", null, NameReference.CreateThised(NameFactory.IIterableCount)),
                                     // ++this.count;
                                     ExpressionFactory.Inc(() => NameReference.CreateThised(NameFactory.IIterableCount)),
                                    // if this.count>this.data.count then
                                    IfBranch.CreateIf(ExpressionFactory.IsGreater(NameReference.CreateThised(NameFactory.IIterableCount),
                                        NameReference.CreateThised(data_field, NameFactory.IIterableCount)), new[]{
                                            // this.data = new Chunk<ART>(this.count,this.data);
                                            Assignment.CreateStatement(NameReference.CreateThised(data_field),
                                                 ExpressionFactory.HeapConstructor(NameFactory.ChunkNameReference(elem_type),
                                                NameReference.CreateThised(NameFactory.IIterableCount),
                                                NameReference.CreateThised(data_field)))
                                        }),
                    Assignment.CreateStatement(FunctionCall.Indexer(NameReference.CreateThised(data_field),
                                NameReference.Create("pos")), NameFactory.PropertySetterValueReference())))
                .Parameters(FunctionParameter.Create(NameFactory.PropertySetterValueParameter,
                    NameFactory.ReferenceNameReference(elem_type)))
                .SetModifier(EntityModifier.Mutable);

            PropertyMemberBuilder indexer_setter_builder = PropertyMemberBuilder.CreateIndexerSetter(Block.CreateStatement(
                             // assert index<=this.count;
                             ExpressionFactory.AssertTrue(ExpressionFactory.IsLessEqual(NameFactory.IndexIndexerReference(),
                                NameReference.CreateThised(NameFactory.IIterableCount))),
                            // if index==this.count then
                            IfBranch.CreateIf(ExpressionFactory.IsEqual(NameFactory.IndexIndexerReference(),
                                NameReference.CreateThised(NameFactory.IIterableCount)), new IExpression[] {
                                    FunctionCall.Create(NameReference.CreateThised(NameFactory.AppendFunctionName),
                                        NameFactory.PropertySetterValueReference())
                                }, IfBranch.CreateElse(new[] {
                                    // this.data[index] = value;
                                    Assignment.CreateStatement(FunctionCall.Indexer(NameReference.CreateThised(data_field),
                                        NameFactory.IndexIndexerReference()), NameFactory.PropertySetterValueReference())
                                }))
                            ));

            const string elem_name = "arr_cc_elem";
            defaultConstructor = FunctionBuilder.CreateInitConstructor(Block.CreateStatement(
                        // this.data = new Chunk<ART>(1);
                        Assignment.CreateStatement(NameReference.CreateThised(data_field),
                             ExpressionFactory.HeapConstructor(NameFactory.ChunkNameReference(elem_type),
                                NatLiteral.Create("1")))
                        ));

            // todo: this is not safe, add initialization with values or repeated value (when we have regular parser)
            // this is NOT capacity constructor, just hacky way for setting values
            var sized_constructor = FunctionBuilder.CreateInitConstructor(Block.CreateStatement(
                        // this.data = new Chunk<ART>(n);
                        Assignment.CreateStatement(NameReference.CreateThised(data_field),
                             ExpressionFactory.HeapConstructor(NameFactory.ChunkNameReference(elem_type),
                                NameReference.Create("n"))),
                        Assignment.CreateStatement(NameReference.CreateThised(NameFactory.IIterableCount), NameReference.Create("n"))
                        ))
                        .Parameters(FunctionParameter.Create("n", NameFactory.SizeNameReference()));

            TypeBuilder builder = TypeBuilder.Create(NameDefinition.Create(NameFactory.ArrayTypeName, elem_type, VarianceMode.None))
                    .SetModifier(EntityModifier.Mutable | EntityModifier.HeapOnly)
                    .Parents(NameFactory.IIndexableNameReference(elem_type))

                    .With(VariableDeclaration.CreateStatement(data_field,
                        NameFactory.PointerNameReference(NameFactory.ChunkNameReference(elem_type)), Undef.Create(),
                        options.ReassignableModifier()))

                    .With(count_property)

                    // default constructor
                    .With(defaultConstructor)

                    .With(sized_constructor)

                    // copy constructor
                    .With(FunctionBuilder.CreateInitConstructor(Block.CreateStatement(
                        Loop.CreateForEach(elem_name, NameReference.Create(elem_type),
                            NameReference.Create(NameFactory.SourceCopyConstructorParameter),
                            new[] {
                            FunctionCall.Create(NameReference.CreateThised(NameFactory.AppendFunctionName),NameReference.Create(elem_name))
                        })), FunctionCall.Constructor(NameReference.CreateThised(NameFactory.InitConstructorName)))
                        .Parameters(FunctionParameter.Create(NameFactory.SourceCopyConstructorParameter,
                            NameFactory.ReferenceNameReference(NameFactory.IIterableNameReference(elem_type, TypeMutability.ReadOnly)))))

                     .With(append)

                     .With(PropertyBuilder.CreateIndexer(options, NameFactory.ReferenceNameReference(elem_type))
                        .Parameters(FunctionParameter.Create(NameFactory.IndexIndexerParameter, NameFactory.SizeNameReference()))
                        .With(indexer_setter_builder, out IMember indexer_setter_func)
                        .With(indexer_getter_builder));

            return builder;
        }

        private static TypeDefinition createChunk(IOptions options, out FunctionDefinition sizeConstructor,
            out FunctionDefinition resizeConstructor,
            out IMember countGetter, out IMember atGetter, out IMember atSetter)
        {
            // todo: in this form this type is broken, size is runtime info, yet we allow the assignment on the stack
            // however it is not yet decided what we will do, maybe this type will be used only internally, 
            // maybe we will introduce two kinds of it, etc.

            const string elem_type = "CHT";

            sizeConstructor = FunctionDefinition.CreateInitConstructor(EntityModifier.Native, new[] {
                            FunctionParameter.Create(NameFactory.ChunkSizeConstructorParameter,NameFactory.SizeNameReference(),
                                ExpressionReadMode.CannotBeRead)
                        },
                        Block.CreateStatement());

            resizeConstructor = FunctionDefinition.CreateInitConstructor(EntityModifier.Native, new[] {
                            FunctionParameter.Create(NameFactory.ChunkSizeConstructorParameter,NameFactory.SizeNameReference(),
                                ExpressionReadMode.CannotBeRead),
                            FunctionParameter.Create(NameFactory.SourceCopyConstructorParameter,
                                NameFactory.ReferenceNameReference( NameFactory.ChunkNameReference(elem_type)),
                                ExpressionReadMode.CannotBeRead)
                        },
                        Block.CreateStatement());
            return TypeBuilder.Create(NameDefinition.Create(NameFactory.ChunkTypeName, elem_type, VarianceMode.None))
                    .SetModifier(EntityModifier.Mutable)
                    .Parents(NameFactory.IIndexableNameReference(elem_type))

                    .With(sizeConstructor)

                    .With(resizeConstructor)

                     .With(PropertyBuilder.Create(options, NameFactory.IIterableCount, () => NameFactory.SizeNameReference())
                        .With(PropertyMemberBuilder.CreateGetter(Block.CreateStatement())
                            .Modifier(EntityModifier.Native | EntityModifier.Override), out countGetter))

                     .With(PropertyBuilder.CreateIndexer(options, NameFactory.ReferenceNameReference(elem_type))
                        .Parameters(FunctionParameter.Create(NameFactory.IndexIndexerParameter, NameFactory.SizeNameReference(),
                            ExpressionReadMode.CannotBeRead))
                        .With(PropertyMemberBuilder.CreateIndexerSetter(Block.CreateStatement())
                            .Modifier(EntityModifier.Native), out atSetter)
                        .With(PropertyMemberBuilder.CreateIndexerGetter(Block.CreateStatement())
                            .Modifier(EntityModifier.Native | EntityModifier.Override), out atGetter));
        }

        private static TypeDefinition createRealType(IOptions options, string numTypeName, Literal minValue, Literal maxValue, out FunctionDefinition parseString,
            params IMember[] extras)
        {
            TypeBuilder builder;
            if (options.AllowRealMagic)
            {
                VariableDeclaration nan = VariableDeclaration.CreateStatement(NameFactory.RealNanName, NameFactory.Real64NameReference(), Real64Literal.Create(double.NaN),
                    EntityModifier.Static | EntityModifier.Public);

                // because of NaNs we cannot use IEquatable/IComparable, 
                // x ==x for NaN gives false despite both sides not only are equal but they are identical
                // https://stackoverflow.com/questions/1565164/what-is-the-rationale-for-all-comparisons-returning-false-for-ieee754-nan-values
                builder = createNumCoreBuilder(options, numTypeName, minValue, maxValue, out parseString, extras.Concat(nan).ToArray())
                    ;
            }
            else
                builder = createNumFullBuilder(options, numTypeName, minValue, maxValue, out parseString, extras);

            return builder
               .With(FunctionBuilder.Create(NameFactory.DivideOperator,
                   ExpressionReadMode.ReadRequired, NameFactory.ItNameReference(),
                   Block.CreateStatement())
                   .SetModifier(EntityModifier.Native)
                   .Parameters(FunctionParameter.Create("x", NameFactory.ItNameReference(), ExpressionReadMode.CannotBeRead)));
        }

        private static TypeDefinition createBool(IOptions options)
        {
            EntityModifier modifier = EntityModifier.Native;
            if (options.AtomicPrimitivesMutable)
                modifier |= EntityModifier.Mutable;
            else
                modifier |= EntityModifier.Const;

            return TypeBuilder.Create(NameFactory.BoolTypeName)
                            .Parents(NameFactory.IEquatableNameReference())
                            .SetModifier(modifier)
                            .With(FunctionDefinition.CreateInitConstructor(EntityModifier.Native, null, Block.CreateStatement()))
                            .With(FunctionDefinition.CreateInitConstructor(EntityModifier.Native,
                                new[] {
                                    FunctionParameter.Create(NameFactory.SourceCopyConstructorParameter, NameFactory.BoolNameReference(),
                                        ExpressionReadMode.CannotBeRead)
                                },
                                Block.CreateStatement()))
                            .With(FunctionBuilder.Create(NameFactory.NotOperator,
                                ExpressionReadMode.ReadRequired, NameFactory.BoolNameReference(),
                                Block.CreateStatement())
                                .SetModifier(EntityModifier.Native))

                                .WithEquatableEquals()
                                .With(FunctionBuilder.Create(NameFactory.EqualOperator,
                                    ExpressionReadMode.ReadRequired, NameFactory.BoolNameReference(),
                                        Block.CreateStatement())
                                    .SetModifier(EntityModifier.Native)
                                .Parameters(FunctionParameter.Create("cmp", NameFactory.ItNameReference(TypeMutability.ReadOnly),
                                    ExpressionReadMode.CannotBeRead)))
                            ;
        }

        private static TypeBuilder createNumFullBuilder(IOptions options, string numTypeName, Literal minValue, Literal maxValue,
            out FunctionDefinition parseString, params IMember[] extras)
        {
            return createNumCoreBuilder(options, numTypeName, minValue, maxValue, out parseString, extras)
                            .Parents(NameFactory.IComparableNameReference())
                            .WithComparableCompare()
                            .With(FunctionBuilder.Create(NameFactory.ComparableCompare,
                                ExpressionReadMode.ReadRequired, NameFactory.OrderingNameReference(),
                                Block.CreateStatement())
                                .SetModifier(EntityModifier.Native)
                                .Parameters(FunctionParameter.Create("cmp", NameFactory.ItNameReference(TypeMutability.ReadOnly), ExpressionReadMode.CannotBeRead)))
                            ;
        }

        private static TypeBuilder createNumCoreBuilder(IOptions options, string numTypeName, Literal minValue, Literal maxValue,
            out FunctionDefinition parseString,
            params IMember[] extras)
        {
            TypeMutability mutability_override = options.AtomicPrimitivesMutable ? TypeMutability.ForceConst : TypeMutability.None;

            parseString = FunctionBuilder.Create(NameFactory.ParseFunctionName,
                        NameFactory.OptionNameReference(NameFactory.ItNameReference()),
                     ExpressionFactory.BodyReturnUndef())
                    .Parameters(FunctionParameter.Create("s", NameFactory.StringPointerNameReference(TypeMutability.ReadOnly),
                        ExpressionReadMode.CannotBeRead))
                    .SetModifier(EntityModifier.Native | EntityModifier.Static);

            EntityModifier modifier = EntityModifier.Native;
            if (options.AtomicPrimitivesMutable)
                modifier |= EntityModifier.Mutable;
            else
                modifier |= EntityModifier.Const;

            TypeBuilder builder = TypeBuilder.Create(numTypeName)
                      .SetModifier(modifier)
                      .With(parseString)
                      .With(extras)
                      .With(VariableDeclaration.CreateStatement(NameFactory.NumMinValueName, NameFactory.ItNameReference(mutability_override),
                          minValue, EntityModifier.Static | EntityModifier.Const | EntityModifier.Public))
                      .With(VariableDeclaration.CreateStatement(NameFactory.NumMaxValueName, NameFactory.ItNameReference(mutability_override),
                          maxValue, EntityModifier.Static | EntityModifier.Const | EntityModifier.Public))
                      .With(FunctionDefinition.CreateInitConstructor(EntityModifier.Native,
                          null, Block.CreateStatement()))

                      .With(FunctionDefinition.CreateInitConstructor(EntityModifier.Native,
                          new[] { FunctionParameter.Create(NameFactory.SourceCopyConstructorParameter, NameFactory.ItNameReference(),
                        ExpressionReadMode.CannotBeRead) },
                          Block.CreateStatement()))

                      .With(FunctionBuilder.Create(NameFactory.AddOperator,
                          ExpressionReadMode.ReadRequired, NameFactory.ItNameReference(),
                          Block.CreateStatement())
                          .SetModifier(EntityModifier.Native)
                          .Parameters(FunctionParameter.Create("x", NameFactory.ItNameReference(), ExpressionReadMode.CannotBeRead)))
                      .With(FunctionBuilder.Create(NameFactory.AddOverflowOperator,
                          ExpressionReadMode.ReadRequired, NameFactory.ItNameReference(),
                          Block.CreateStatement())
                          .SetModifier(EntityModifier.Native)
                          .Parameters(FunctionParameter.Create("x", NameFactory.ItNameReference(), ExpressionReadMode.CannotBeRead)))
                      .With(FunctionBuilder.Create(NameFactory.SubOperator,
                          ExpressionReadMode.ReadRequired, NameFactory.ItNameReference(),
                          Block.CreateStatement())
                          .SetModifier(EntityModifier.Native)
                          .Parameters(FunctionParameter.Create("x", NameFactory.ItNameReference(), ExpressionReadMode.CannotBeRead)))
                      .With(FunctionBuilder.Create(NameFactory.MulOperator,
                          ExpressionReadMode.ReadRequired, NameFactory.ItNameReference(),
                          Block.CreateStatement())
                          .SetModifier(EntityModifier.Native)
                          .Parameters(FunctionParameter.Create("x", NameFactory.ItNameReference(), ExpressionReadMode.CannotBeRead)))

                          ;

            builder = withComparableFunctions(builder);

            return builder;
        }

        private static TypeBuilder withComparableFunctions(TypeBuilder builder)
        {
            bool via_pointer = builder.Modifier.HasHeapOnly;

            Func<NameReference> type_ref;
            if (via_pointer)
                type_ref = () => NameFactory.PointerNameReference(NameFactory.ItNameReference(TypeMutability.ReadOnly));
            else
                type_ref = () => NameFactory.ItNameReference(TypeMutability.ReadOnly);

            return builder
                      .With(FunctionBuilder.Create(NameFactory.LessOperator,
                          ExpressionReadMode.ReadRequired, NameFactory.BoolNameReference(),
                          Block.CreateStatement())
                          .SetModifier(EntityModifier.Native)
                          .Parameters(FunctionParameter.Create("cmp", type_ref(), ExpressionReadMode.CannotBeRead)))
                      .With(FunctionBuilder.Create(NameFactory.LessEqualOperator,
                          ExpressionReadMode.ReadRequired, NameFactory.BoolNameReference(),
                          Block.CreateStatement())
                          .SetModifier(EntityModifier.Native)
                          .Parameters(FunctionParameter.Create("cmp", type_ref(), ExpressionReadMode.CannotBeRead)))
                      .With(FunctionBuilder.Create(NameFactory.GreaterOperator,
                          ExpressionReadMode.ReadRequired, NameFactory.BoolNameReference(),
                          Block.CreateStatement())
                          .SetModifier(EntityModifier.Native)
                          .Parameters(FunctionParameter.Create("cmp", type_ref(), ExpressionReadMode.CannotBeRead)))
                      .With(FunctionBuilder.Create(NameFactory.GreaterEqualOperator,
                          ExpressionReadMode.ReadRequired, NameFactory.BoolNameReference(),
                          Block.CreateStatement())
                          .SetModifier(EntityModifier.Native)
                          .Parameters(FunctionParameter.Create("cmp", type_ref(), ExpressionReadMode.CannotBeRead)))

                      .With(FunctionBuilder.Create(NameFactory.EqualOperator,
                          ExpressionReadMode.ReadRequired, NameFactory.BoolNameReference(),
                          Block.CreateStatement())
                          .SetModifier(EntityModifier.Native)
                          .Parameters(FunctionParameter.Create("cmp", type_ref(), ExpressionReadMode.CannotBeRead)))
                      .With(FunctionBuilder.Create(NameFactory.NotEqualOperator,
                          ExpressionReadMode.ReadRequired, NameFactory.BoolNameReference(),
                          Block.CreateStatement())
                          .SetModifier(EntityModifier.Native)
                          .Parameters(FunctionParameter.Create("cmp", type_ref(), ExpressionReadMode.CannotBeRead)))
                    ;
        }

        private static TypeDefinition createChannelType()
        {
            return TypeBuilder.Create(NameDefinition.Create(NameFactory.ChannelTypeName,
                    TemplateParametersBuffer.Create().Add("T").Values))
                .SetModifier(EntityModifier.HeapOnly | EntityModifier.Native)
                .Constraints(ConstraintBuilder.Create("T").SetModifier(EntityModifier.Const))
                // default constructor
                .With(FunctionDefinition.CreateInitConstructor(EntityModifier.Native,
                    null, Block.CreateStatement()))
                .With(FunctionBuilder.Create(NameFactory.ChannelSend,
                    ExpressionReadMode.ReadRequired, NameFactory.BoolNameReference(), Block.CreateStatement())
                    .SetModifier(EntityModifier.Native)
                    .Parameters(FunctionParameter.Create("value", NameReference.Create("T"), ExpressionReadMode.CannotBeRead)))
                .With(FunctionBuilder.Create(NameFactory.ChannelClose,
                    ExpressionReadMode.OptionalUse, NameFactory.UnitNameReference(),
                    Block.CreateStatement())
                    .SetModifier(EntityModifier.Native))
                .With(FunctionBuilder.Create(NameFactory.ChannelReceive,
                    ExpressionReadMode.ReadRequired, NameFactory.OptionNameReference(NameReference.Create("T")),
                    Block.CreateStatement())
                    .SetModifier(EntityModifier.Native))
                /*.With(FunctionDefinition.CreateFunction(EntityModifier.None, NameDefinition.Create(NameFactory.ChannelTryReceive),
                    null,
                    ExpressionReadMode.ReadRequired, NameFactory.OptionNameReference(NameReference.Create("T")),
                    Block.CreateStatement(new[] { Return.Create(Undef.Create()) })))*/
                .Parents(NameFactory.IObjectNameReference())
                .Build();
        }

        private TypeDefinition createOptionType(out FunctionDefinition emptyConstructor, out FunctionDefinition valueConstructor)
        {
            TypeMutability mutability_override = this.Options.AtomicPrimitivesMutable ? TypeMutability.ForceConst : TypeMutability.None;

            const string elem_type = "OPT";

            valueConstructor = FunctionDefinition.CreateInitConstructor(EntityModifier.Public,
                                new[] { FunctionParameter.Create("value", NameReference.Create(elem_type)) },
                                    Block.CreateStatement(new[] {
                                        Assignment.CreateStatement(NameReference.CreateThised(NameFactory.OptionValue),
                                            NameReference.Create("value")),
                                        Assignment.CreateStatement(NameReference.CreateThised(NameFactory.OptionHasValue),
                                            BoolLiteral.CreateTrue())
                                    }));

            emptyConstructor = FunctionDefinition.CreateInitConstructor(EntityModifier.Public,
                                null,
                                    Block.CreateStatement(
                                        Assignment.CreateStatement(NameReference.CreateThised(NameFactory.OptionHasValue),
                                            BoolLiteral.CreateFalse())
                                    ));

            return TypeBuilder.Create(NameDefinition.Create(NameFactory.OptionTypeName,
              TemplateParametersBuffer.Create().Add(elem_type, VarianceMode.Out).Values))
                            .With(Alias.Create(NameFactory.OptionTypeParameterMember, NameReference.Create(elem_type),
                                EntityModifier.Public))
                            .With(VariableDeclaration.CreateStatement(NameFactory.OptionValue, NameReference.Create(elem_type),
                                Undef.Create()))
                            .With(VariableDeclaration.CreateStatement(NameFactory.OptionHasValue, NameFactory.BoolNameReference(mutability_override),
                                Undef.Create()))
                            .With(emptyConstructor)
                            .With(valueConstructor)
                            ;
        }

        private static TypeDefinition createIComparableType()
        {
            var eq = FunctionBuilder.Create(NameFactory.EqualOperator, NameFactory.BoolNameReference(),
                Block.CreateStatement(
                    Return.Create(ExpressionFactory.IsEqual(FunctionCall.Create(NameReference.CreateThised(NameFactory.ComparableCompare),
                        NameReference.Create("cmp")), NameFactory.OrderingEqualReference()))))
                .Parameters(FunctionParameter.Create("cmp", NameFactory.ReferenceNameReference(NameFactory.IComparableNameReference(TypeMutability.ReadOnly))));

            var gt = FunctionBuilder.Create(NameFactory.GreaterOperator, NameFactory.BoolNameReference(),
                Block.CreateStatement(
                    Return.Create(ExpressionFactory.IsEqual(FunctionCall.Create(NameReference.CreateThised(NameFactory.ComparableCompare),
                        NameReference.Create("cmp")), NameFactory.OrderingGreaterReference()))))
                .Parameters(FunctionParameter.Create("cmp", NameFactory.ReferenceNameReference(NameFactory.IComparableNameReference())));
            var lt = FunctionBuilder.Create(NameFactory.LessOperator, NameFactory.BoolNameReference(),
                Block.CreateStatement(
                    Return.Create(ExpressionFactory.IsEqual(FunctionCall.Create(NameReference.CreateThised(NameFactory.ComparableCompare),
                        NameReference.Create("cmp")), NameFactory.OrderingLessReference()))))
                .Parameters(FunctionParameter.Create("cmp", NameFactory.ReferenceNameReference(NameFactory.IComparableNameReference())));
            var ge = FunctionBuilder.Create(NameFactory.GreaterEqualOperator, NameFactory.BoolNameReference(),
                Block.CreateStatement(
                    Return.Create(ExpressionFactory.IsNotEqual(FunctionCall.Create(NameReference.CreateThised(NameFactory.ComparableCompare),
                        NameReference.Create("cmp")), NameFactory.OrderingLessReference()))))
                .Parameters(FunctionParameter.Create("cmp", NameFactory.ReferenceNameReference(NameFactory.IComparableNameReference())));
            var le = FunctionBuilder.Create(NameFactory.LessEqualOperator, NameFactory.BoolNameReference(),
                Block.CreateStatement(
                    Return.Create(ExpressionFactory.IsNotEqual(FunctionCall.Create(NameReference.CreateThised(NameFactory.ComparableCompare),
                        NameReference.Create("cmp")), NameFactory.OrderingGreaterReference()))))
                .Parameters(FunctionParameter.Create("cmp", NameFactory.ReferenceNameReference(NameFactory.IComparableNameReference())));

            return TypeBuilder.CreateInterface(NameFactory.IComparableTypeName)
                .Parents(NameFactory.IEquatableNameReference())
                .SetModifier(EntityModifier.Base)
                .With(FunctionBuilder.CreateDeclaration(NameFactory.ComparableCompare, NameFactory.OrderingNameReference())
                    .SetModifier(EntityModifier.Pinned)
                    .Parameters(FunctionParameter.Create("cmp",
                        NameFactory.ReferenceNameReference(NameFactory.ShouldBeThisNameReference(NameFactory.IComparableTypeName, TypeMutability.ReadOnly)))))
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

        private static TypeDefinition createTuple(IOptions options, int count, out FunctionDefinition factory)
        {
            var type_parameters = new List<string>();
            var properties = new List<Property>();
            for (int i = 0; i < count; ++i)
            {
                var type_name = $"TPT{i}";
                type_parameters.Add(type_name);
                properties.Add(PropertyBuilder.CreateReferential(options, NameFactory.TupleItemName(i),
                        () => NameReference.Create(type_name))
                    .WithAutoField(Undef.Create(), options.ReassignableModifier())
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
                    TemplateParametersBuffer.Create(type_parameters).Add(base_type_name, VarianceMode.Out).Values))
                .SetModifier(EntityModifier.Mutable)
                .Parents(NameFactory.ITupleNameReference(type_parameters.Concat(base_type_name)
                    .Select(it => NameReference.Create(it)).ToArray()))
                .With(ExpressionFactory.BasicConstructor(properties.Select(it => it.Name.Name).ToArray(),
                     type_parameters.Select(it => NameReference.Create(it)).ToArray()))

                .With(properties)

                .With(PropertyBuilder.CreateIndexer(options, NameFactory.ReferenceNameReference(base_type_name))
                    .Parameters(FunctionParameter.Create(NameFactory.IndexIndexerParameter, NameFactory.SizeNameReference()))
                    .With(PropertyMemberBuilder.CreateIndexerGetter(Block.CreateStatement(item_selector))
                        .Modifier(EntityModifier.Override)))

                .Constraints(TemplateConstraint.Create(base_type_name, null, null, null,
                    type_parameters.Select(it => NameReference.Create(it))));

            // creating static factory method for the above tuple

            NameReference func_result_typename = NameFactory.TupleNameReference(type_parameters.Concat(base_type_name)
                .Select(it => NameReference.Create(it)).ToArray());
            factory = FunctionBuilder.Create(NameFactory.CreateFunctionName,
                TemplateParametersBuffer.Create(type_parameters.Concat(base_type_name).ToArray()).Values,
                    func_result_typename,
                    Block.CreateStatement(Return.Create(
                         ExpressionFactory.StackConstructor(func_result_typename,
                            Enumerable.Range(0, count).Select(i => NameReference.Create(NameFactory.TupleItemName(i))).ToArray()))))
                    .SetModifier(EntityModifier.Static)
                    .Parameters(Enumerable.Range(0, count).Select(i => FunctionParameter.Create(NameFactory.TupleItemName(i),
                        NameReference.Create(type_parameters[i]))).ToArray())

                    .Constraints(TemplateConstraint.Create(base_type_name, null, null, null,
                        type_parameters.Select(it => NameReference.Create(it))));

            return builder;
        }

        private static TypeDefinition createIIterator()
        {
            const string elem_type = "ITRT";
            return TypeBuilder.CreateInterface(NameDefinition.Create(NameFactory.IIteratorTypeName, elem_type, VarianceMode.Out))
                            .SetModifier(EntityModifier.Mutable)

                            .With(FunctionBuilder.CreateDeclaration(NameFactory.IteratorNext, ExpressionReadMode.ReadRequired,
                                    NameFactory.OptionNameReference(NameFactory.ReferenceNameReference(NameReference.Create(elem_type))))
                                    .SetModifier(EntityModifier.Mutable))
                                    ;
        }

        private static TypeDefinition createUtf8StringIterator(IOptions options)
        {
            const string str_name = "str";
            const string index_name = "index";

            TypeBuilder builder = TypeBuilder.Create(NameFactory.Utf8StringIteratorTypeName)

                .SetModifier(EntityModifier.Mutable)
                .Parents(NameFactory.IIteratorNameReference(NameFactory.CharNameReference()))

                .With(VariableDeclaration.CreateStatement(index_name,
                     NameFactory.SizeNameReference(),
                     null,
                     options.ReassignableModifier()))
                .With(VariableDeclaration.CreateStatement(str_name,
                    NameFactory.PointerNameReference(NameFactory.Utf8StringNameReference(TypeMutability.ReadOnly)),
                    Undef.Create()))

                .With(ExpressionFactory.BasicConstructor(new[] { str_name },
                    new[] { NameFactory.PointerNameReference(
                        NameFactory.Utf8StringNameReference(TypeMutability.ReadOnly)) }))

                 .With(FunctionBuilder.Create(NameFactory.IteratorNext,
                    NameFactory.OptionNameReference(LifetimeScope.Attachment,
                        NameFactory.ReferenceNameReference(NameFactory.CharNameReference())),
                    Block.CreateStatement(
                        // if this.index < this.str.length then
                        IfBranch.CreateIf(ExpressionFactory.IsLess(NameReference.CreateThised(index_name),
                            NameReference.CreateThised(str_name, NameFactory.StringLength)),
                            new IExpression[] {
                                // let ch = this.str[this.index]
                                VariableDeclaration.CreateStatement("ch",null,
                                    FunctionCall.Indexer(NameReference.CreateThised(str_name), NameReference.CreateThised(index_name))),
                                // this.index += ch.length
                                 ExpressionFactory.IncBy(() => NameReference.CreateThised(index_name),
                                    NameReference.Create("ch",NameFactory.CharLength)),
                                // return ch
                                Return.Create( ExpressionFactory.OptionOf(NameFactory.ReferenceNameReference(LifetimeScope.Attachment,
                                    NameFactory.CharNameReference()),
                                    NameReference.Create("ch") ))
                            }
                            ),
                        Return.Create(ExpressionFactory.OptionEmpty(NameFactory.ReferenceNameReference(LifetimeScope.Attachment,
                            NameFactory.CharNameReference())))
                        ))
                      .SetModifier(EntityModifier.Mutable | EntityModifier.Override))
            ;

            return builder;

        }

        private static TypeDefinition createIndexIterator(IOptions options)
        {
            const string elem_type_name = "XIRT";
            const string coll_name = "coll";
            const string index_name = "index";

            TypeBuilder builder = TypeBuilder.Create(
                NameDefinition.Create(NameFactory.IndexIteratorTypeName,
                    TemplateParametersBuffer.Create(VarianceMode.Out, elem_type_name).Values))

                .SetModifier(EntityModifier.Mutable)
                .Parents(NameFactory.IIteratorNameReference(elem_type_name))

                .With(VariableDeclaration.CreateStatement(index_name,
                     NameFactory.SizeNameReference(),
                     null,
                     options.ReassignableModifier()))
                .With(VariableDeclaration.CreateStatement(coll_name,
                    NameFactory.PointerNameReference(NameFactory.IIndexableNameReference(elem_type_name,
                        overrideMutability: TypeMutability.ReadOnly)),
                    Undef.Create()))

                .With(ExpressionFactory.BasicConstructor(new[] { coll_name },
                    new[] { NameFactory.PointerNameReference(
                        NameFactory.IIndexableNameReference(elem_type_name,
                        overrideMutability: TypeMutability.ReadOnly)) }))

                 .With(FunctionBuilder.Create(NameFactory.IteratorNext,
                    NameFactory.OptionNameReference(LifetimeScope.Attachment,
                        NameFactory.ReferenceNameReference(NameReference.Create(elem_type_name))),
                    Block.CreateStatement(
                        // if this.index >= this.coll.count then
                        IfBranch.CreateIf(ExpressionFactory.IsGreaterEqual(NameReference.CreateThised(index_name),
                            NameReference.CreateThised(coll_name, NameFactory.IIterableCount)),
                                // return None
                                Return.Create(ExpressionFactory.OptionEmpty(NameFactory.ReferenceNameReference(
                                    NameReference.Create(elem_type_name)))),
                                // else
                                IfBranch.CreateElse(
                                    new[] {
                                        // let el = this.coll[this.index]
                                        VariableDeclaration.CreateStatement("el",null,
                                            FunctionCall.Indexer(NameReference.CreateThised(coll_name), NameReference.CreateThised(index_name))),
                                        // this.index += 1
                                         ExpressionFactory.Inc(()=> NameReference.CreateThised(index_name)),
                                    // return Some(el)
                                    Return.Create( ExpressionFactory.OptionOf(NameFactory.ReferenceNameReference(LifetimeScope.Attachment,
                                        NameReference.Create(elem_type_name)),
                                        NameReference.Create("el")))
                                        }))

                        ))
                      .SetModifier(EntityModifier.Mutable | EntityModifier.Override))

            ;

            return builder;

        }
        private static TypeDefinition createIIndexable(IOptions options)
        {
            const string elem_type_name = "IXBT";

            TypeBuilder builder = TypeBuilder.CreateInterface(
                NameDefinition.Create(NameFactory.IIndexableTypeName,
                    TemplateParametersBuffer.Create(VarianceMode.Out, elem_type_name).Values))

                .Parents(NameFactory.ISequenceNameReference(NameReference.Create(elem_type_name)),
                    NameFactory.ICountedNameReference())

                .With(FunctionBuilder.Create(NameFactory.IterableGetIterator,
                    NameFactory.ReferenceNameReference(LifetimeScope.Attachment, NameFactory.IIteratorNameReference(elem_type_name)),
                    Block.CreateStatement(
                        Return.Create(ExpressionFactory.HeapConstructor(NameFactory.IndexIteratorNameReference(elem_type_name),
                            NameFactory.ThisReference()))))
                    .SetModifier(EntityModifier.Override))

                .With(PropertyBuilder.CreateIndexer(options, () => NameFactory.ReferenceNameReference(elem_type_name))
                    .Parameters(FunctionParameter.Create(NameFactory.IndexIndexerParameter, NameFactory.SizeNameReference()))
                    .With(PropertyMemberBuilder.CreateIndexerGetter(body: null)
                        .Modifier(EntityModifier.Override)));

            return builder;
        }

        private static TypeDefinition createITuple(IOptions options, int count)
        {
            var type_parameters = new List<string>();
            var properties = new List<Property>();
            foreach (int i in Enumerable.Range(0, count))
            {
                var type_name = $"TIPT{i}";
                type_parameters.Add(type_name);
                properties.Add(PropertyBuilder.CreateReferential(options, NameFactory.TupleItemName(i), () => NameReference.Create(type_name))
                    .WithGetter(body: null).Build());
            }

            const string base_type_name = "TIPC";

            TypeBuilder builder = TypeBuilder.CreateInterface(
                NameDefinition.Create(NameFactory.ITupleTypeName,
                    TemplateParametersBuffer.Create(VarianceMode.Out, type_parameters.Concat(base_type_name).ToArray()).Values))
                .Parents(NameFactory.IIndexableNameReference(NameReference.Create(base_type_name)))

                .With(properties)

                .With(PropertyBuilder.Create(options, NameFactory.IIterableCount, () => NameFactory.SizeNameReference())
                    .WithGetter(Block.CreateStatement(Return.Create(NatLiteral.Create($"{count}"))), EntityModifier.Override))

                .Constraints(TemplateConstraint.Create(base_type_name, null, null, null, type_parameters.Select(it => NameReference.Create(it))));

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
            return this.UnitType.InstanceOf.HasSameCore(typeInstance);
        }
        public bool IsIntType(IEntityInstance typeInstance)
        {
            return typeInstance.IsExactlySame(this.Int64Type.InstanceOf, jokerMatchesAll: false);
        }
        public bool IsNatType(IEntityInstance typeInstance)
        {
            return typeInstance.IsExactlySame(this.Nat64Type.InstanceOf, jokerMatchesAll: false);
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
        public EntityInstance Reference(IEntityInstance instance, TypeMutability mutability,
            bool viaPointer)
        {
            return Reference(instance, mutability, Lifetime.Timeless, viaPointer);
        }
        public EntityInstance Reference(IEntityInstance instance, TypeMutability mutability,
            Lifetime lifetime, bool viaPointer)
        {
            TypeDefinition typedef = (viaPointer ? this.PointerType : this.ReferenceType);
            return typedef.GetInstance(mutability, TemplateTranslation.Create(typedef.InstanceOf, instance ), lifetime);
        }

        public int Dereference(IEntityInstance instance, out IEntityInstance result)
        {
            int count = 0;

            while (true)
            {
                if (!DereferencedOnce(instance, out result, out bool via_pointer))
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
