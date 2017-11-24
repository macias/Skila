using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Comparers;
using Skila.Language.Data;
using Skila.Language.Extensions;
using Skila.Language.Entities;

namespace Skila.Language
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class NameRegistry : INameRegistry
    {
        private readonly ILayerDictionary<ITemplateName, LocalInfo> bag;

        public NameRegistry(bool shadowing)
        {
            this.bag = LayerDictionary.Create<ITemplateName, LocalInfo>(shadowing,EntityNameArityComparer.Instance);
        }

        public void AddLayer(IScope scope)
        {
            if (scope != null)
                this.bag.PushLayer();
        }

        internal IEnumerable<ILocalBindable> RemoveLayer()
        {
            return this.bag.PopLayer().Where(it => !it.Item2.Used).Select(it => it.Item2.Bindable);
        }

        internal bool Add(ILocalBindable bindable)
        {
            bool result = this.bag.Add(bindable.Name, new LocalInfo(bindable));
            return result;
        }

        internal bool TryGet<T>(ITemplateName name, out T value)
            where T : class, IBindable
        {
            LocalInfo info;
            if (!this.bag.TryGetValue(name, out info))
            {
                value = null;
                return false;
            }

            value = info.Bindable as T;
            bool result = value != null;
            if (result)
                info.Used = true;
            return result;
        }

        public override string ToString()
        {
            IReadOnlyCollection<ITemplateName> names = this.bag.Keys.StoreReadOnly();
            string result = names.Select(it => it.ToString()).Join(", ");
            if (names.Count > 10)
                result += " ...";
            return result;
        }
    }
}
