using NaiveLanguageTools.Common;
using Skila.Language.Extensions;
using Skila.Language.Printout;
using System.Collections.Generic;

namespace Skila.Language
{
    public sealed class TemplateArgument : OwnedNode,IPrintable,ISurfable
    {
        public INameReference TypeName { get; }

        public override IEnumerable<INode> ChildrenNodes => new[] { this.TypeName };

        public bool IsSurfed { get; set; }

        public TemplateArgument(Lifetime lifetime, INameReference typeName)
        {
            this.TypeName = typeName;

            this.attachPostConstructor();
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
            this.ChildrenNodes.WhereType<ISurfable>().ForEach(it => it.Surfed(ctx));
        }
    }
}
