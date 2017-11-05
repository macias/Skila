using NaiveLanguageTools.Common;
using Skila.Language.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Language.Entities
{
    public sealed class FunctorSignature : Node, IFunctionSignature
    {
        public IReadOnlyList<FunctionParameter> Parameters { get; }
        public INameReference ResultTypeName { get; }

        public override IEnumerable<INode> OwnedNodes => this.Parameters.Select(it => it.Cast<INode>()).Concat(ResultTypeName);

        internal FunctorSignature(IEnumerable<FunctionParameter> parameters,INameReference resultTypeName)
        {
            this.Parameters = parameters.Indexed().StoreReadOnlyList();
            this.ResultTypeName = resultTypeName;

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }
    }

}