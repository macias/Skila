using System.Collections.Generic;
using NaiveLanguageTools.Common;
using System.Linq;
using System.Diagnostics;

namespace Skila.Language
{
    public sealed class NameFactory
    {
        // something user cannot use as a symbol
        private const string magicMarker = "'";

        private const string mutableMarker = "¡";

        public const string RootNamespace = ":root:";
        public const string SystemNamespace = "System";
        public const string ConcurrencyNamespace = "Concurrency";
        public const string CollectionsNamespace = "Collections";
        public const string IoNamespace = "Io";
        public const string TextNamespace = "Text";

        public const string JokerTypeName = "[[@@]]";
        public const string IFunctionTypeName = "IFunction";
        public const string TupleTypeName = "Tuple";
        public const string ITupleTypeName = "ITuple";
        public const string IIndexableTypeName = "IIndexable";
        public const string IndexIteratorTypeName = "IndexIterator";
        public const string Utf8StringIteratorTypeName = "Utf8StringIterator";
        public const string FileTypeName = "File";
        public const string TypeInfoTypeName = "TypeInfo";
        public const string CaptureTypeName = "Capture";
        public const string MatchTypeName = "Match";
        public const string RegexTypeName = "Regex";
        public const string VoidTypeName = "Void";
        public const string UnitTypeName = "Unit";
        public const string IObjectTypeName = "IObject";
        public const string ReferenceTypeName = "Ref";
        public const string PointerTypeName = "Ptr";
        public const string BoolTypeName = "Bool";
        public const string CharTypeName = "Char";
        public const string Int16TypeName = "Int16";
        public const string Int64TypeName = "Int64";
        public const string IntTypeName = "Int"; // todo: make it platform dependent
        public const string Nat8TypeName = "Nat8";
        public const string Nat64TypeName = "Nat64";
        public const string NatTypeName = "Nat"; // todo: make it platform dependent
        public const string SizeTypeName = "Size"; // todo: make it platform dependent
        //public const string EnumTypeName = "Enum";
        public const string OrderingTypeName = "Ordering";
        public const string IComparableTypeName = "IComparable";
        public const string Utf8StringTypeName = "Utf8String";
        public const string StringTypeName = "String";
        public const string ChannelTypeName = "Channel";
        public const string OptionTypeName = "Option";
        public const string OptionTypeParameterMember = "type";
        public const string ExceptionTypeName = "Exception";
        public const string ISequenceTypeName = "ISequence";
        public const string ICountedTypeName = "ICounted";
        public const string LinqExtensionName = "Linq";
        public const string ChunkTypeName = "Chunk";
        public const string ArrayTypeName = "Array";
        public const string IIterableTypeName = "IIterable";
        public const string IIteratorTypeName = "IIterator";
        public const string IEquatableTypeName = "IEquatable";

        public const string DateTypeName = "Date";
        public const string DateDayOfWeekProperty = "dayOfWeek";

        public const string MainFunctionName = "main";

        public const string DayOfWeekTypeName = "DayOfWeek";
        public const string SundayDayOfWeekTypeName = "sunday";
        public const string MondayDayOfWeekTypeName = "monday";
        public const string TuesdayDayOfWeekTypeName = "tuesday";
        public const string WednesdayDayOfWeekTypeName = "wednesday";
        public const string ThursdayDayOfWeekTypeName = "thursday";
        public const string FridayDayOfWeekTypeName = "friday";
        public const string SaturdayDayOfWeekTypeName = "saturday";

        public const string CaptureStartFieldName = "start";
        public const string CaptureEndFieldName = "end";
        public const string CaptureIdFieldName = "id";
        public const string CaptureNameFieldName = "name";

        public const string NumMinValueName = "minValue";
        public const string NumMaxValueName = "maxValue";
        public const string RealNanName = "nan";

        public const string MatchStartFieldName = "start";
        public const string MatchEndFieldName = "end";
        public const string MatchCapturesFieldName = "captures";

        public const string RegexPatternFieldName = "pattern";
        public const string RegexContainsFunctionName = "contains";
        public const string RegexMatchFunctionName = "match";

        public const string RealTypeName = "Real";
        public const string Real64TypeName = "Real64";
        public const string Real32TypeName = "Real32";
        public const string ThisVariableName = "this";
        public const string SelfTypeTypeName = "Self";
        public const string ItTypeName = "It";
        public const string BaseVariableName = "base";
        public const string RecurFunctionName = "recur";
        public const string SuperFunctionName = "super";
        public const string SourceCopyConstructorParameter = "source";
        public const string SourceConvConstructorParameter = "value";

        public const string OrderingLess = "less";
        public const string OrderingEqual = "equal";
        public const string OrderingGreater = "greater";

        public const string ComparableCompare = "compare";

        public const string CommandLineProgramPath = "program";
        public const string CommandLineArguments = "args";

        public const string AddOverflowOperator = "+®";
        public const string AddOperator = "+";
        public const string MulOperator = "*";
        public const string DivideOperator = "/";
        public const string SubOperator = "-";
        public const string EqualOperator = "==";
        public const string NotEqualOperator = "!=";
        public const string GreaterOperator = ">";
        public const string GreaterEqualOperator = ">=";
        public const string LessOperator = "<";
        public const string LessEqualOperator = "<=";
        public const string NotOperator = "not";

        public const string Sink = "_";

        public const string AtFunctionName = "at";
        public const string PropertyIndexerName = AtFunctionName;
        public const string IIterableCount = "count";
        public static readonly string IteratorNext = MutableName("next");

        public const string StringTrim = "trim";
        // don't use terms left/right because it will confuse right-to-left devs
        public const string StringTrimStart = "trimStart";
        public const string StringTrimEnd = "trimEnd";
        public const string StringConcat = ConcatFunctionName;
        public const string StringIndexOf = "indexOf";
        public const string StringLastIndexOf = "lastIndexOf";
        public const string StringLength = "length";
        public const string StringReverse = ReverseFunctionName;
        public const string StringSplit = "split";
        public const string StringRemove = "remove";
        public const string StringSlice = "slice";

        public const string CharLength = "length";

        public const string FileReadLines = "readLines";
        public const string FileExists = "exists";

        public const string IterableGetIterator = "getIterator";

        public const string LambdaInvoke = "invoke";

        public const string ChannelSend = "send";
        public const string ChannelClose = "close";
        public const string ChannelReceive = "receive";
        //public const string ChannelTryReceive = "tryReceive";

        public const string PropertyGetter = "get";
        public const string PropertySetter = "set";
        public const string PropertyAutoField = magicMarker + "field";
        public const string PropertySetterValueParameter = "value";

        public const string EnumConstructorParameter = "ord";

        public const string ChunkSizeConstructorParameter = "size";
        public const string IndexIndexerParameter = "index";

        public const string FileFilePathParameter = "filePath";

        public const string OptionHasValue = "hasValue";
        public const string OptionValue = "value";

        public const string SpreadFunctionName = "spread";
        public const string StoreFunctionName = "store";
        public const string ConvertFunctionName = "to";
        public const string InitConstructorName = "init";
        public const string ZeroConstructorName = "zero";
        public const string NewConstructorName = "new";

        public const string ParseFunctionName = "parse";
        public const string CreateFunctionName = "create";
        public const string ConcatFunctionName = "concat";
        public const string MapFunctionName = "map";
        public const string FilterFunctionName = "filter";
        public const string ReverseFunctionName = "reverse";
        public const string AnyFunctionName = "any";
        public const string AllFunctionName = "all";
        public static readonly string AppendFunctionName = MutableName("append");

        public const string GetTypeFunctionName = "getType";

        public static NameReference UnitNameReference()
        {
            return NameReference.Create(NameReference.Root, UnitTypeName);
        }

        public static string TupleItemName(int index)
        {
            return $"item{index}";
        }
        public static NameReference IFunctionNameReference(params INameReference[] types)
        {
            return NameReference.Create(NameReference.Root, IFunctionTypeName, types);
        }


        public static NameReference Int16NameReference()
        {
            return NameReference.Create(NameReference.Root, Int16TypeName);
        }
        public static NameReference Int64NameReference(TypeMutability mutability = TypeMutability.None)
        {
            return NameReference.Create( mutability, NameReference.Root, Int64TypeName);
        }
        public static NameReference IntNameReference()
        {
            return NameReference.Create(NameReference.Root, IntTypeName);
        }
        public static NameReference Nat8NameReference(TypeMutability mutability = TypeMutability.None)
        {
            return NameReference.Create(mutability, NameReference.Root, Nat8TypeName);
        }
        public static NameReference SinkReference()
        {
            return NameReference.Create(NameFactory.Sink);
        }

        public static NameReference Nat64NameReference()
        {
            return NameReference.Create(NameReference.Root, Nat64TypeName);
        }
        public static NameReference NatNameReference()
        {
            return NameReference.Create(NameReference.Root, NatTypeName);
        }
        public static NameReference SizeNameReference(TypeMutability mutability = TypeMutability.None)
        {
            return NameReference.Create(mutability, NameReference.Root, SizeTypeName);
        }
        public static NameReference SelfNameReference(TypeMutability mutability = TypeMutability.None)
        {
            return NameReference.Create(mutability, SelfTypeTypeName);
        }
        /* public static NameReference EnumNameReference()
         {
             return NameReference.Create(NameReference.Root, EnumTypeName);
         }*/
        public static NameReference ThisReference()
        {
            return NameReference.Create(ThisVariableName);
        }
        public static NameReference ItNameReference(TypeMutability mutability = TypeMutability.None)
        {
            return NameReference.Create(mutability, ItTypeName);
        }
        public static NameReference BoolNameReference(TypeMutability mutability = TypeMutability.None)
        {
            return NameReference.Create(mutability, NameReference.Root, BoolTypeName);
        }
        public static NameReference CharNameReference()
        {
            return NameReference.Create(NameReference.Root, CharTypeName);
        }

        internal static NameDefinition InitConstructorNameDefinition()
        {
            return NameDefinition.Create(InitConstructorName);
        }
        internal static NameDefinition ZeroConstructorNameDefinition()
        {
            return NameDefinition.Create(ZeroConstructorName);
        }
        internal static NameDefinition NewConstructorNameDefinition()
        {
            return NameDefinition.Create(NewConstructorName);
        }

        public static NameReference OrderingEqualReference()
        {
            return NameReference.Create(OrderingNameReference(), OrderingEqual);
        }
        public static NameReference OrderingLessReference()
        {
            return NameReference.Create(OrderingNameReference(), OrderingLess);
        }
        public static NameReference OrderingGreaterReference()
        {
            return NameReference.Create(OrderingNameReference(), OrderingGreater);
        }

        public static NameReference DayOfWeekNameReference()
        {
            return NameReference.Create(SystemNamespaceReference(), DayOfWeekTypeName);
        }
        public static NameReference DateNameReference()
        {
            return NameReference.Create(SystemNamespaceReference(), DateTypeName);
        }
        public static NameReference IEquatableNameReference(TypeMutability mutability = TypeMutability.None)
        {
            return NameReference.Create(mutability, SystemNamespaceReference(), IEquatableTypeName);
        }
        public static NameReference ChannelNameReference(string templateParamName)
        {
            return ChannelNameReference(NameReference.Create(templateParamName));
        }
        public static NameReference ChannelNameReference(NameReference templateParamName)
        {
            return NameReference.Create(ConcurrencyNamespaceReference(), ChannelTypeName, templateParamName);
        }
        public static NameReference IndexIndexerReference()
        {
            return NameReference.Create(NameFactory.IndexIndexerParameter);
        }
        public static NameReference PropertySetterValueReference()
        {
            return NameReference.Create(NameFactory.PropertySetterValueParameter);
        }
        public static NameReference SpreadFunctionReference()
        {
            return NameReference.Create(SystemNamespaceReference(), SpreadFunctionName);
        }
        public static NameReference StoreFunctionReference()
        {
            return NameReference.Create(SystemNamespaceReference(), StoreFunctionName);
        }
        public static NameReference IIndexableNameReference(string templateParamName, TypeMutability overrideMutability = TypeMutability.None)
        {
            return IIndexableNameReference(NameReference.Create(templateParamName), overrideMutability);
        }
        public static NameReference IIteratorNameReference(INameReference templateTypeName, TypeMutability mutability = TypeMutability.None)
        {
            return NameReference.Create(mutability, CollectionsNamespaceReference(), IIteratorTypeName, templateTypeName);
        }
        public static NameReference IIteratorNameReference(string templateTypeName, TypeMutability mutability = TypeMutability.None)
        {
            return IIteratorNameReference(NameReference.Create(templateTypeName), mutability);
        }
        public static NameReference Utf8StringIteratorNameReference()
        {
            return NameReference.Create(SystemNamespaceReference(), Utf8StringIteratorTypeName);
        }
        public static NameReference IndexIteratorNameReference(INameReference templateTypeName)
        {
            return NameReference.Create(CollectionsNamespaceReference(), IndexIteratorTypeName, templateTypeName);
        }
        public static NameReference FileNameReference()
        {
            return NameReference.Create(IoNamespaceReference(), FileTypeName);
        }
        public static NameReference IndexIteratorNameReference(string templateTypeName)
        {
            return IndexIteratorNameReference(NameReference.Create(templateTypeName));
        }
        public static NameReference IIndexableNameReference(INameReference templateTypeName,
            TypeMutability overrideMutability = TypeMutability.None)
        {
            return NameReference.Create(overrideMutability, CollectionsNamespaceReference(), IIndexableTypeName, templateTypeName);
        }
        public static NameReference LinqExtensionReference()
        {
            return NameReference.Create(CollectionsNamespaceReference(), LinqExtensionName);
        }
        public static NameReference ICountedNameReference(TypeMutability mutability = TypeMutability.None)
        {
            return NameReference.Create(mutability, CollectionsNamespaceReference(), ICountedTypeName);
        }
        public static NameReference ISequenceNameReference(INameReference templateTypeName, TypeMutability mutability = TypeMutability.None)
        {
            return NameReference.Create(mutability, CollectionsNamespaceReference(), ISequenceTypeName, templateTypeName);
        }
        public static NameReference ISequenceNameReference(string templateParamName, TypeMutability mutability = TypeMutability.None)
        {
            return ISequenceNameReference(NameReference.Create(templateParamName), mutability);
        }

        public static NameReference IIterableNameReference(string templateParamName,
            TypeMutability overrideMutability = TypeMutability.None)
        {
            return IIterableNameReference(NameReference.Create(templateParamName), overrideMutability);
        }

        public static NameReference IIterableNameReference(NameReference templateParamName,
            TypeMutability mutability = TypeMutability.None)
        {
            return NameReference.Create(mutability, CollectionsNamespaceReference(), IIterableTypeName,
                templateParamName);
        }

        public static NameReference ChunkNameReference(string templateParamName)
        {
            return ChunkNameReference(NameReference.Create(templateParamName));
        }

        public static NameReference ChunkNameReference(INameReference templateParamName)
        {
            return NameReference.Create(CollectionsNamespaceReference(), ChunkTypeName, templateParamName);
        }
        public static NameReference ArrayNameReference(string templateParamName, TypeMutability mutability = TypeMutability.None)
        {
            return ArrayNameReference(NameReference.Create(templateParamName), mutability);
        }
        public static NameReference ArrayNameReference(INameReference templateParamName, TypeMutability mutability = TypeMutability.None)
        {
            return NameReference.Create(mutability, CollectionsNamespaceReference(), ArrayTypeName, templateParamName);
        }
        public static NameReference ConcatReference()
        {
            return NameReference.Create(CollectionsNamespaceReference(), ConcatFunctionName);
        }
        public static NameReference TupleNameReference(params INameReference[] templateParamNames)
        {
            return NameReference.Create(CollectionsNamespaceReference(), TupleTypeName, templateParamNames);
        }
        public static NameReference TupleFactoryReference()
        {
            return NameReference.Create(CollectionsNamespaceReference(), TupleTypeName);
        }
        public static NameReference ITupleNameReference(params INameReference[] templateParamNames)
        {
            return NameReference.Create(CollectionsNamespaceReference(), ITupleTypeName, templateParamNames);
        }
        public static NameReference ITupleMutableNameReference(params INameReference[] templateParamNames)
        {
            return NameReference.Create(TypeMutability.ForceMutable, CollectionsNamespaceReference(), ITupleTypeName, templateParamNames);
        }

        public static NameReference IObjectNameReference(TypeMutability overrideMutability = TypeMutability.None)
        {
            return NameReference.Create(overrideMutability, NameReference.Root, IObjectTypeName);
        }

        public static NameReference SystemNamespaceReference()
        {
            return NameReference.Create(NameReference.Root, SystemNamespace);
        }
        public static NameReference CollectionsNamespaceReference()
        {
            return NameReference.Create(SystemNamespaceReference(), CollectionsNamespace);
        }
        public static NameReference TextNamespaceReference()
        {
            return NameReference.Create(SystemNamespaceReference(), TextNamespace);
        }
        public static NameReference IoNamespaceReference()
        {
            return NameReference.Create(SystemNamespaceReference(), IoNamespace);
        }
        public static NameReference ConcurrencyNamespaceReference()
        {
            return NameReference.Create(SystemNamespaceReference(), ConcurrencyNamespace);
        }

        public static NameReference Utf8StringPointerNameReference(TypeMutability mutability = TypeMutability.None)
        {
            return NameFactory.PointerNameReference(Utf8StringNameReference(mutability));
        }
        public static NameReference StringPointerNameReference(TypeMutability mutability = TypeMutability.None)
        {
            return NameFactory.PointerNameReference(StringNameReference(mutability));
        }
        public static NameReference StringNameReference(TypeMutability mutability = TypeMutability.None)
        {
            return NameReference.Create(mutability, SystemNamespaceReference(), NameFactory.StringTypeName);
        }
        public static NameReference Utf8StringNameReference(TypeMutability mutability = TypeMutability.None)
        {
            return NameReference.Create(mutability, SystemNamespaceReference(), NameFactory.Utf8StringTypeName);
        }
        public static NameReference IComparableNameReference(TypeMutability mutability = TypeMutability.None)
        {
            return NameReference.Create(mutability, SystemNamespaceReference(), NameFactory.IComparableTypeName);
        }
        public static NameReference OrderingNameReference()
        {
            return NameReference.Create(SystemNamespaceReference(), NameFactory.OrderingTypeName);
        }
        public static NameReference OptionNameReference(INameReference name, TypeMutability mutability = TypeMutability.None)
        {
            return NameReference.Create(mutability, SystemNamespaceReference(), NameFactory.OptionTypeName, name);
        }
        public static NameReference ExceptionNameReference()
        {
            return NameReference.Create(SystemNamespaceReference(), NameFactory.ExceptionTypeName);
        }
        public static NameReference TypeInfoPointerNameReference()
        {
            return PointerNameReference(NameReference.Create(SystemNamespaceReference(), NameFactory.TypeInfoTypeName));
        }
        public static NameReference CaptureNameReference()
        {
            return NameReference.Create(TextNamespaceReference(), NameFactory.CaptureTypeName);
        }
        public static NameReference MatchNameReference()
        {
            return NameReference.Create(TextNamespaceReference(), NameFactory.MatchTypeName);
        }
        public static NameReference RegexNameReference()
        {
            return NameReference.Create(TextNamespaceReference(), NameFactory.RegexTypeName);
        }
        public static NameReference RealNameReference()
        {
            return NameReference.Create(NameReference.Root, NameFactory.RealTypeName);
        }
        public static NameReference Real64NameReference()
        {
            return NameReference.Create(NameReference.Root, NameFactory.Real64TypeName);
        }
        public static NameReference PointerNameReference(INameReference name, TypeMutability mutability = TypeMutability.None)
        {
            return NameReference.Create(mutability, NameReference.Root, NameFactory.PointerTypeName, name);
        }
        public static NameReference PointerNameReference(string typeName)
        {
            return PointerNameReference(NameReference.Create(typeName));
        }
        public static NameReference ReferenceNameReference(INameReference name)
        {
            return NameReference.Create(NameReference.Root, NameFactory.ReferenceTypeName, name);
        }
        public static NameReference ReferenceNameReference(string name)
        {
            return ReferenceNameReference(NameReference.Create(name));
        }

        internal static INameReference ShouldBeThisNameReference(string typeName, TypeMutability mutability = TypeMutability.None)
        {
            // todo: Skila1 supported the notion of dynamic "this type", Skila-3 should also have it
            // so once we have time to do it this method will help us fast track all the use cases to replace
            return NameReference.Create(mutability, typeName);
        }

        public static string MutableName(string s)
        {
            return mutableMarker + s;
        }

        public static string UnmutableName(string s)
        {
            return s.Substring(mutableMarker.Length);
        }

        public static bool IsMutableName(string s)
        {
            return s.StartsWith(mutableMarker);
        }
    }
}