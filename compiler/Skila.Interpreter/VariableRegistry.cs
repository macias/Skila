using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Data;
using Skila.Language.Extensions;
using Skila.Language;
using System;

namespace Skila.Interpreter
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class VariableRegistry : ILayeredNameRegistry
    {
        private readonly ILayerDictionary<ILocalBindable, ObjectData> bag;

        public VariableRegistry(bool shadowing)
        {
            this.bag = LayerDictionary.Create<ILocalBindable, ObjectData>(shadowing,ReferenceEqualityComparer<ILocalBindable>.Instance);
        }

        public void AddLayer(IScope scope)
        {
            if (scope != null)
                this.bag.PushLayer();
        }

        internal IEnumerable<Tuple<ILocalBindable, ObjectData>> RemoveLayer()
        {
            return this.bag.PopLayer();
        }

        internal bool Add(ILocalBindable bindable, ObjectData value)
        {
            if (value.DebugId.Id==183100)
            {
                ;
            }
            if (bindable.DebugId.Id == 2797)
            {
                ;
            }
            bool result = this.bag.Add(bindable, value);
            return result;
        }

        internal bool TryGet(ILocalBindable bindable, out ObjectData info)
        {
            if (bindable == null)
            {
                info = null;
                return false;
            }
            else
                return this.bag.TryGetValue(bindable, out info);
        }

        public override string ToString()
        {
            IReadOnlyList<ILocalBindable> keys = this.bag.Keys.StoreReadOnlyList();
            string result;
            if (keys.Count == 0)
                result = "<empty>";
            else
            {
                result = keys.Select(it => it.Name.ToString()).Join(", ");
                if (keys.Count > 10)
                    result += " ...";
            }
            return result;
        }
    }
}
