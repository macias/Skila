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

        public static Extension Create()
        {
            return new Extension(null);
        }

        private Extension(NameDefinition name) : base(EntityModifier.Static, name ?? NameDefinition.Create(""), null)
        {
            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }
    }
}
