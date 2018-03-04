using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Comparers;
using Skila.Language.Entities;
using Skila.Language.Extensions;
using Skila.Language.Semantics;

namespace Skila.Language
{
    // used in statements like "break foo;", "continue bar;"
    // we don't use NameReference for it because it is more complex -- NR binds to EntityInstance
    // (which in turn requires Entity, and loops are not entities), EntityInstance uses Evaluation
    // and to compute it we would have to evaluate loop first which leads to circular computations
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class LabelReference : Node, ITemplateName,IComputable
    {
        public static LabelReference CreateLocal(string name)
        {
            return new LabelReference(name,isLocal:true);
        }
        public static LabelReference CreateGlobal(string name)
        {
            return new LabelReference(name, isLocal: false);
        }

        public int Arity => 0;
        public string Name { get; }

        private Option<ILabelBindable> binding;
        private readonly bool isLocal;

        public ILabelBindable Binding => this.binding.Value;
        public bool IsComputed => this.binding.HasValue;

        public override IEnumerable<INode> OwnedNodes => Enumerable.Empty<INode>();

        private LabelReference(string name,bool isLocal)
        {
            this.Name = name;
            this.isLocal = isLocal;

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }

        public override string ToString()
        {
            return Name;
        }

        public void Evaluate(ComputationContext ctx)
        {
            {
                if (this.isLocal)
                {
                    if (ctx.EvalLocalNames != null && ctx.EvalLocalNames.TryGet<IAnchor>(this, out IAnchor anchor))
                        this.binding = new Option<ILabelBindable>(anchor);
                }
                else
                {
                    TypeDefinition curr_type = this.EnclosingScope<TypeDefinition>();
                    IReadOnlyCollection<ILabelBindable> found = curr_type.NestedEntities()
                        .WhereType<ILabelBindable>(it => EntityBareNameComparer.Instance.Equals(it.Label, this)).StoreReadOnly();
                    if (found.Count>0)
                    {
                        this.binding = new Option<ILabelBindable>(found.First());
                        if (found.Count > 1)
                            ctx.AddError(ErrorCode.AmbiguousReference, this);
                    }
                }

                if (!this.binding.HasValue)
                {
                    this.binding = new Option<ILabelBindable>(null);
                    ctx.ErrorManager.AddError(ErrorCode.ReferenceNotFound, this);
                }
            }
        }
    }
}