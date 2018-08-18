using System.Diagnostics;
using NaiveLanguageTools.Common;
using System.Collections.Generic;
using Skila.Language.Extensions;

namespace Skila.Language.Entities
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class Namespace : TypeContainerDefinition
    {
        private ScopeTable availableEntities;
        public override ScopeTable AvailableEntities
        {
            get
            {
                if (availableEntities == null)
                    this.availableEntities = new ScopeTable(this.NestedEntityInstances());
                return this.availableEntities;
            }
        }

        public static Namespace Create(NameDefinition name)
        {
            return new Namespace(name);
        }
        public static Namespace Create(string name)
        {
            return new Namespace(NameDefinition.Create(name));
        }

        private Namespace(NameDefinition name) : base(EntityModifier.Static, name, null, null)
        {
            this.attachPostConstructor();
        }

        public override void Validate(ComputationContext ctx)
        {
            base.Validate(ctx);

        }
        public override void Surf(ComputationContext ctx)
        {
            this.ChildrenNodes.WhereType<ISurfable>().ForEach(it => it.Surfed(ctx));
        }
    }
}
