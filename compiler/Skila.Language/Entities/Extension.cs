using System.Diagnostics;
using NaiveLanguageTools.Common;
using System.Collections.Generic;
using Skila.Language.Extensions;

namespace Skila.Language.Entities
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class Extension : TypeContainerDefinition
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

        public static Extension Create(string name = null,IEnumerable<NameReference> includes = null)
        {
            return new Extension(NameDefinition.Create(name ?? ""),includes);
        }

        private Extension(NameDefinition name,
            IEnumerable<NameReference> includes) : base(EntityModifier.Static, name, constraints: null, includes: includes)
        {
            this.attachPostConstructor();
        }

        public override void Surf(ComputationContext ctx)
        {
            this.ChildrenNodes.WhereType<ISurfable>().ForEach(it => it.Surfed(ctx));
        }
    }
}
