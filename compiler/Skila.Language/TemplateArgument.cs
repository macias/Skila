using NaiveLanguageTools.Common;
using Skila.Language.Extensions;
using Skila.Language.Printout;
using System.Collections.Generic;

namespace Skila.Language
{
    public sealed class TemplateArgument : Node,IPrintable,ISurfable
    {
        public INameReference TypeName { get; }

        public override IEnumerable<INode> OwnedNodes => new[] { this.TypeName };

        public bool IsSurfed { get; set; }

        public TemplateArgument(Lifetime lifetime, INameReference typeName)
        {
            this.TypeName = typeName;

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }
        public TemplateArgument(INameReference typeName) : this(Lifetime.Timeless,typeName)
        {
        }

        public ICode Printout()
        {
            return this.TypeName.Printout();
        }

        public void Surf(ComputationContext ctx)
        {
            this.OwnedNodes.WhereType<ISurfable>().ForEach(it => it.Surfed(ctx));
        }
    }
}
