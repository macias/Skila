using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using System.Collections.Generic;
using Skila.Language.Semantics;
using Skila.Language.Extensions;

namespace Skila.Language.Entities
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class Namespace : TypeContainerDefinition
    {
        public override IEnumerable<EntityInstance> AvailableEntities => this.NestedEntityInstances();

        public static Namespace Create(NameDefinition name)
        {
            return new Namespace(name);
        }
        public static Namespace Create(string name)
        {
            return new Namespace(NameDefinition.Create(name));
        }

        private Namespace(NameDefinition name) : base(EntityModifier.Static, name, null)
        {
            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }

        public override void Validate(ComputationContext ctx)
        {
            base.Validate(ctx);

        }
    }
}
