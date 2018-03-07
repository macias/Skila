using System.Diagnostics;
using NaiveLanguageTools.Common;
using System.Collections.Generic;
using Skila.Language.Extensions;

namespace Skila.Language.Entities
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class Extension : TypeContainerDefinition
    {
        public override IEnumerable<EntityInstance> AvailableEntities => this.NestedEntityInstances();

        public static Extension Create(string name = null,IEnumerable<NameReference> includes = null)
        {
            return new Extension(NameDefinition.Create(name ?? ""),includes);
        }

        private Extension(NameDefinition name,
            IEnumerable<NameReference> includes) : base(EntityModifier.Static, name, constraints: null, includes: includes)
        {
            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }
    }
}
