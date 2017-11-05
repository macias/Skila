using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;

namespace Skila.Language.Entities
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class Namespace : TypeContainerDefinition
    {
        public static Namespace Create(NameDefinition name)
        {
            return new Namespace(name);
        }
        public static Namespace Create(string name)
        {
            return new Namespace(NameDefinition.Create(name));
        }

        private Namespace(NameDefinition name) : base(EntityModifier.None, name)
        {
            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }
    }
}
