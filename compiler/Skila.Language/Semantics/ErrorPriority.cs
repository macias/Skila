using NaiveLanguageTools.Common;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Language.Semantics
{
    public sealed class ErrorPriority
    {
        private DynamicDictionary<ErrorCode, HashSet<ErrorCode>> overrides;
        private Dictionary<ErrorCode, HashSet<ErrorCode>> isOverridenBy;

        public ErrorPriority()
        {
            this.overrides = DynamicDictionary.CreateWithDefault<ErrorCode, HashSet<ErrorCode>>();
            this.isOverridenBy = new Dictionary<ErrorCode, HashSet<ErrorCode>>();

            add(ErrorCode.VariableNotInitialized,
                ErrorCode.MissingTypeAndValue);

            add(ErrorCode.NotFunctionType,
                ErrorCode.ReferenceNotFound);

            add(ErrorCode.NOTEST_AmbiguousOverloadedCall, 
                ErrorCode.OverloadingDuplicateFunctionDefinition);

            add(ErrorCode.ExpressionValueNotUsed, 
                ErrorCode.CannotReadExpression);

            add(ErrorCode.MissingThisPrefix, 
                ErrorCode.InstanceMemberAccessInStaticContext);

            add(ErrorCode.VirtualFunctionMissingImplementation,
                ErrorCode.EnumCrossInheritance);

            add(ErrorCode.BindableNotUsed,
                ErrorCode.ReservedName,
                ErrorCode.NameAlreadyExists);
        }
        private void add(ErrorCode lower, params ErrorCode[] higherCodes)
        {
            foreach (ErrorCode higher in higherCodes)
                this.overrides[higher].Add(lower);
            // using 'add' method to cause an exception when lower is added second time
            this.isOverridenBy.Add(lower, higherCodes.ToHashSet());
        }

        public IEnumerable<ErrorCode> GetHigher(ErrorCode err)
        {
            HashSet<ErrorCode> result;
            if (this.isOverridenBy.TryGetValue(err, out result))
                return result;
            else
                return Enumerable.Empty<ErrorCode>();
        }

        public IEnumerable<ErrorCode> GetLower(ErrorCode err)
        {
            return this.overrides[err];
        }
    }
}
