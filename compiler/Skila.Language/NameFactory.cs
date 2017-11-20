using System.Collections.Generic;
using NaiveLanguageTools.Common;
using System.Linq;
using System;
using Skila.Language.Entities;

namespace Skila.Language
{
    public sealed class NameFactory
    {
        public const string RootNamespace = ":root:";
        public const string SystemNamespace = "System";
        public const string ConcurrencyNamespace = "Concurrency";
        public const string CollectionsNamespace = "Collections";

        public const string JokerTypeName = "[[@@]]";
        public const string FunctionTypeName = "IFunction";
        public const string VoidTypeName = "Void";
        public const string UnitTypeName = "Unit";
        public const string ObjectTypeName = "Object";
        public const string ReferenceTypeName = "Ref";
        public const string PointerTypeName = "Ptr";
        public const string BoolTypeName = "Bool";
        public const string IntTypeName = "Int";
        public const string StringTypeName = "String";
        public const string ChannelTypeName = "Channel";
        public const string OptionTypeName = "Option";
        public const string ExceptionTypeName = "Exception";
        public const string ISequenceTypeName = "ISequence";
        public const string IIterableTypeName = "IIterable";
        public const string DoubleTypeName = "Double";
        public const string ThisVariableName = "this";

        public const string AddOperator = "+";
        public const string NotOperator = "not";

        public const string LambdaInvoke = "invoke";

        public const string ChannelSend = "send";
        public const string ChannelClose = "close";
        public const string ChannelReceive = "receive";
        //public const string ChannelTryReceive = "tryReceive";

        public const string PropertyGetter = "get";
        public const string PropertySetter = "set";
        public const string PropertyAutoField = "field";
        public const string PropertySetterParameter = "value";

        public const string OptionHasValue = "HasValue";
        public const string OptionValue = "Value";

        public const string SpreadFunctionName = "spread";
        public const string ConvertFunctionName = "to";
        public const string InitConstructorName = "init";
        public const string ZeroConstructorName = "zero";
        public const string NewConstructorName = "new";

        public static NameReference UnitTypeReference()
        {
            return NameReference.Create(NameReference.Root, UnitTypeName);
        }

        /*public static NameReference JokerTypeReference()
        {
            return EntityInstance.Joker.NameOf;
        }*/

        public static NameReference FunctionTypeReference(IEnumerable<INameReference> arguments, INameReference result)
        {
            return NameReference.Create(NameReference.Root, FunctionTypeName, arguments.Concat(result).ToArray());
        }

        public static NameReference IntTypeReference()
        {
            return NameReference.Create(NameReference.Root, IntTypeName);
        }
        public static NameReference ThisReference()
        {
            return NameReference.Create(ThisVariableName);
        }
        public static NameReference BoolTypeReference()
        {
            return NameReference.Create(NameReference.Root, BoolTypeName);
        }
        public static NameReference VoidTypeReference()
        {
            return NameReference.Create(NameReference.Root, VoidTypeName);
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

        public static NameReference IIterableTypeReference(string templateParamName)
        {
            return NameReference.Create(CollectionsNamespaceReference(), IIterableTypeName, NameReference.Create(templateParamName));
        }
        public static NameReference ChannelTypeReference(string templateParamName)
        {
            return ChannelTypeReference(NameReference.Create(templateParamName));
        }
        public static NameReference ChannelTypeReference(NameReference templateParamName)
        {
            return NameReference.Create(ConcurrencyNamespaceReference(), ChannelTypeName, templateParamName);
        }
        public static NameReference ISequenceTypeReference(string templateParamName)
        {
            return NameReference.Create(CollectionsNamespaceReference(), ISequenceTypeName, NameReference.Create(templateParamName));
        }

        public static NameReference ObjectTypeReference()
        {
            return NameReference.Create(NameReference.Root, ObjectTypeName);
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
            return NameReference.Create(SystemNamespaceReference(), NameFactory.StringTypeName);
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

    }
}