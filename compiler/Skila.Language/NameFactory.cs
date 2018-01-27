using System.Collections.Generic;
using NaiveLanguageTools.Common;
using System.Linq;

namespace Skila.Language
{
    public sealed class NameFactory
    {
        // something user cannot use as a symbol
        private const string magicMarker = "'";

        public const string RootNamespace = ":root:";
        public const string SystemNamespace = "System";
        public const string ConcurrencyNamespace = "Concurrency";
        public const string CollectionsNamespace = "Collections";

        public const string JokerTypeName = "[[@@]]";
        public const string IFunctionTypeName = "IFunction";
        public const string TupleTypeName = "Tuple";
        public const string ITupleTypeName = "ITuple";
        public const string IIndexableTypeName = "IIndexable";
        public const string IndexIteratorTypeName = "IndexIterator";
        public const string VoidTypeName = "Void";
        public const string UnitTypeName = "Unit";
        public const string UnitValue = "unit";
        public const string ObjectTypeName = "Object";
        public const string ReferenceTypeName = "Ref";
        public const string PointerTypeName = "Ptr";
        public const string BoolTypeName = "Bool";
        public const string IntTypeName = "Int";
        //public const string EnumTypeName = "Enum";
        public const string OrderingTypeName = "Ordering";
        public const string ComparableTypeName = "Comparable";
        public const string StringTypeName = "String";
        public const string ChannelTypeName = "Channel";
        public const string OptionTypeName = "Option";
        public const string ExceptionTypeName = "Exception";
        public const string ISequenceTypeName = "ISequence";
        public const string ChunkTypeName = "Chunk";
        public const string ArrayTypeName = "Array";
        public const string IIterableTypeName = "IIterable";
        public const string IIteratorTypeName = "IIterator";
        public const string IEquatableTypeName = "IEquatable";

        public const string DateTypeName = "Date";
        public const string DateDayOfWeekProperty = "dayOfWeek";

        public const string DayOfWeekTypeName = "DayOfWeek";
        public const string SundayDayOfWeekTypeName = "sunday";
        public const string MondayDayOfWeekTypeName = "monday";
        public const string TuesdayDayOfWeekTypeName = "tuesday";
        public const string WednesdayDayOfWeekTypeName = "wednesday";
        public const string ThursdayDayOfWeekTypeName = "thursday";
        public const string FridayDayOfWeekTypeName = "friday";
        public const string SaturdayDayOfWeekTypeName = "saturday";

        public const string DoubleTypeName = "Double";
        public const string ThisVariableName = "this";
        public const string ItTypeName = "It";
        public const string BaseVariableName = "base";
        public const string SelfFunctionName = "self";
        public const string SuperFunctionName = "super";
        public const string SourceCopyConstructorParameter = "source";
        public const string SourceConvConstructorParameter = "value";

        public const string OrderingLess = "less";
        public const string OrderingEqual = "equal";
        public const string OrderingGreater = "greater";

        public const string ComparableCompare = "compare";

        public const string AddOperator = "+";
        public const string MulOperator = "*";
        public const string SubOperator = "-";
        public const string EqualOperator = "==";
        public const string NotEqualOperator = "!=";
        public const string GreaterOperator = ">";
        public const string GreaterEqualOperator = ">=";
        public const string LessOperator = "<";
        public const string LessEqualOperator = "<=";
        public const string NotOperator = "not";

        public const string PropertyIndexerName = "at";
        public const string IterableCount = "count";
        public const string IteratorGet = "get";
        public const string IteratorNext = "next";

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

        public const string OptionHasValue = "HasValue";
        public const string OptionValue = "Value";

        public const string SpreadFunctionName = "spread";
        public const string ConvertFunctionName = "to";
        public const string InitConstructorName = "init";
        public const string ZeroConstructorName = "zero";
        public const string NewConstructorName = "new";

        public const string ParseFunctionName = "parse";
        public const string CreateFunctionName = "create";
        public const string ConcatFunctionName = "concat";
        public const string MapFunctionName = "map";
        public const string FilterFunctionName = "filter";
        public const string AppendFunctionName = "append";

        public static NameReference UnitTypeReference()
        {
            return NameReference.Create(NameReference.Root, UnitTypeName);
        }

        /*public static NameReference JokerTypeReference()
        {
            return EntityInstance.Joker.NameOf;
        }*/

        public static string TupleItemName(int index)
        {
            return $"item{index}";
        }
        public static NameReference IFunctionTypeReference(params INameReference[] types)
        {
            return NameReference.Create(NameReference.Root, IFunctionTypeName, types);
        }

        public static NameReference IntTypeReference()
        {
            return NameReference.Create(NameReference.Root, IntTypeName);
        }
        /* public static NameReference EnumTypeReference()
         {
             return NameReference.Create(NameReference.Root, EnumTypeName);
         }*/
        public static NameReference ThisReference()
        {
            return NameReference.Create(ThisVariableName);
        }
        public static NameReference ItTypeReference()
        {
            return NameReference.Create(ItTypeName);
        }
        public static NameReference BoolTypeReference()
        {
            return NameReference.Create(NameReference.Root, BoolTypeName);
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
            return NameReference.Create(OrderingTypeReference(), OrderingEqual);
        }
        public static NameReference OrderingLessReference()
        {
            return NameReference.Create(OrderingTypeReference(), OrderingLess);
        }
        public static NameReference OrderingGreaterReference()
        {
            return NameReference.Create(OrderingTypeReference(), OrderingGreater);
        }

        public static NameReference DayOfWeekTypeReference()
        {
            return NameReference.Create(SystemNamespaceReference(), DayOfWeekTypeName);
        }
        public static NameReference DateTypeReference()
        {
            return NameReference.Create(SystemNamespaceReference(), DateTypeName);
        }
        public static NameReference EquatableTypeReference()
        {
            return NameReference.Create(SystemNamespaceReference(), IEquatableTypeName);
        }
        public static NameReference ChannelTypeReference(string templateParamName)
        {
            return ChannelTypeReference(NameReference.Create(templateParamName));
        }
        public static NameReference ChannelTypeReference(NameReference templateParamName)
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
        public static NameReference ISequenceTypeReference(string templateParamName, MutabilityFlag overrideMutability = MutabilityFlag.ConstAsSource)
        {
            return ISequenceTypeReference(NameReference.Create(templateParamName), overrideMutability);
        }
        public static NameReference IIndexableTypeReference(string templateParamName, MutabilityFlag overrideMutability = MutabilityFlag.ConstAsSource)
        {
            return IIndexableTypeReference(NameReference.Create(templateParamName), overrideMutability);
        }
        public static NameReference ISequenceTypeReference(INameReference templateTypeName, MutabilityFlag overrideMutability = MutabilityFlag.ConstAsSource)
        {
            return NameReference.Create(overrideMutability, CollectionsNamespaceReference(), ISequenceTypeName, templateTypeName);
        }
        public static NameReference IIteratorTypeReference(INameReference templateTypeName)
        {
            return NameReference.Create(CollectionsNamespaceReference(), IIteratorTypeName, templateTypeName);
        }
        public static NameReference IIteratorTypeReference(string templateTypeName)
        {
            return IIteratorTypeReference(NameReference.Create(templateTypeName));
        }
        public static NameReference IndexIteratorTypeReference(INameReference templateTypeName)
        {
            return NameReference.Create(CollectionsNamespaceReference(), IndexIteratorTypeName, templateTypeName);
        }
        public static NameReference IndexIteratorTypeReference(string templateTypeName)
        {
            return IndexIteratorTypeReference(NameReference.Create(templateTypeName));
        }
        public static NameReference IIndexableTypeReference(INameReference templateTypeName,
            MutabilityFlag overrideMutability = MutabilityFlag.ConstAsSource)
        {
            return NameReference.Create(overrideMutability, CollectionsNamespaceReference(), IIndexableTypeName, templateTypeName);
        }
        public static NameReference IIterableTypeReference(string templateParamName,
            MutabilityFlag mutability = MutabilityFlag.ConstAsSource)
        {
            return IIterableTypeReference(NameReference.Create(templateParamName), mutability);
        }
        public static NameReference IIterableTypeReference(NameReference templateParamName,
            MutabilityFlag mutability = MutabilityFlag.ConstAsSource)
        {
            return NameReference.Create(mutability, CollectionsNamespaceReference(), IIterableTypeName,
                templateParamName);
        }

        public static NameReference ChunkTypeReference(string templateParamName)
        {
            return ChunkTypeReference(NameReference.Create(templateParamName));
        }
        public static NameReference ChunkTypeReference(INameReference templateParamName)
        {
            return NameReference.Create(CollectionsNamespaceReference(), ChunkTypeName, templateParamName);
        }
        public static NameReference ArrayTypeReference(string templateParamName)
        {
            return ArrayTypeReference(NameReference.Create(templateParamName));
        }
        public static NameReference ArrayTypeReference(INameReference templateParamName)
        {
            return NameReference.Create(CollectionsNamespaceReference(), ArrayTypeName, templateParamName);
        }
        public static NameReference ConcatReference()
        {
            return NameReference.Create(CollectionsNamespaceReference(), ConcatFunctionName);
        }
        public static NameReference TupleTypeReference(params INameReference[] templateParamNames)
        {
            return NameReference.Create(CollectionsNamespaceReference(), TupleTypeName, templateParamNames);
        }
        public static NameReference TupleFactoryReference()
        {
            return NameReference.Create(CollectionsNamespaceReference(), TupleTypeName);
        }
        public static NameReference ITupleTypeReference(params INameReference[] templateParamNames)
        {
            return NameReference.Create(CollectionsNamespaceReference(), ITupleTypeName, templateParamNames);
        }
        public static NameReference ITupleMutableTypeReference(params INameReference[] templateParamNames)
        {
            return NameReference.Create(MutabilityFlag.ForceMutable, CollectionsNamespaceReference(), ITupleTypeName, templateParamNames);
        }

        public static NameReference ObjectTypeReference(MutabilityFlag overrideMutability = MutabilityFlag.ConstAsSource)
        {
            return NameReference.Create(overrideMutability, NameReference.Root, ObjectTypeName);
        }

        public static NameReference SystemNamespaceReference()
        {
            return NameReference.Create(NameReference.Root, SystemNamespace);
        }
        public static NameReference CollectionsNamespaceReference()
        {
            return NameReference.Create(SystemNamespaceReference(), CollectionsNamespace);
        }
        public static NameReference ConcurrencyNamespaceReference()
        {
            return NameReference.Create(SystemNamespaceReference(), ConcurrencyNamespace);
        }

        public static NameReference StringTypeReference()
        {
            return NameFactory.PointerTypeReference(NameReference.Create(SystemNamespaceReference(), NameFactory.StringTypeName));
        }
        public static NameReference ComparableTypeReference()
        {
            return NameReference.Create(SystemNamespaceReference(), NameFactory.ComparableTypeName);
        }
        public static NameReference OrderingTypeReference()
        {
            return NameReference.Create(SystemNamespaceReference(), NameFactory.OrderingTypeName);
        }
        public static NameReference OptionTypeReference(INameReference name)
        {
            return NameReference.Create(SystemNamespaceReference(), NameFactory.OptionTypeName, name);
        }

        public static NameReference ExceptionTypeReference()
        {
            return NameReference.Create(SystemNamespaceReference(), NameFactory.ExceptionTypeName);
        }
        public static NameReference DoubleTypeReference()
        {
            return NameReference.Create(NameReference.Root, NameFactory.DoubleTypeName);
        }
        public static NameReference PointerTypeReference(INameReference name)
        {
            return NameReference.Create(NameReference.Root, NameFactory.PointerTypeName, name);
        }
        public static NameReference PointerTypeReference(string typeName)
        {
            return PointerTypeReference(NameReference.Create(typeName));
        }
        public static NameReference ReferenceTypeReference(INameReference name)
        {
            return NameReference.Create(NameReference.Root, NameFactory.ReferenceTypeName, name);
        }
        public static NameReference ReferenceTypeReference(string name)
        {
            return ReferenceTypeReference(NameReference.Create(name));
        }

        internal static INameReference ShouldBeThisTypeReference(string typeName)
        {
            // todo: Skila1 supported the notion of dynamic "this type", Skila-3 should also have it
            // so once we have time to do it this method will help us fast track all the use cases to replace
            return NameReference.Create(typeName);
        }
    }
}