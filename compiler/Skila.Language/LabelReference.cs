using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Semantics;

namespace Skila.Language
{
    // used in statements like "break foo;", "continue bar;"
    // we don't use NameReference for it because it is more complex -- NR binds to EntityInstance
    // (which in turn requires Entity, and loops are not entities), EntityInstance uses Evaluation
    // and to compute it we would have to evaluate loop first which leads to circular computations
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class LabelReference : Node, ITemplateName
    {
        public static LabelReference Create(string name)
        {
            return new LabelReference(name);
        }

        public int Arity => 0;
        public string Name { get; }

        private Option<IAnchor> binding;
        public IAnchor Binding => this.binding.Value;

        public override IEnumerable<INode> OwnedNodes => Enumerable.Empty<INode>();

        private LabelReference(string name)
            : base()
        {
            this.Name = name;
            this.binding = new Option<IAnchor>();

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }

        public override string ToString()
        {
            return Name;
        }

        public void Evaluate(ComputationContext ctx)
        {
            if (!this.binding.HasValue)
            {
                IAnchor anchor;

                if (ctx.EvalLocalNames != null && ctx.EvalLocalNames.TryGet<IAnchor>(this, out anchor))
                    this.binding = new Option<IAnchor>(anchor);
                else
                {
                    this.binding = new Option<IAnchor>(null);
                    ctx.ErrorManager.AddError(ErrorCode.ReferenceNotFound, this);
                }
            }
        }
    }
}