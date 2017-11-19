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
    public sealed class VariableRegistry
    {
        private readonly LayerDictionary< IBindable, ObjectData> bag;

        public VariableRegistry()
        {
            this.bag = new LayerDictionary< IBindable, ObjectData>(ReferenceEqualityComparer<IBindable>.Instance);
        }

        internal void AddLayer(IScope scope)
        {
            if (scope != null)
                this.bag.PushLayer();
        }

        internal IEnumerable<Tuple<IBindable, ObjectData>> RemoveLayer()
        {
             return this.bag.PopLayer();
        }

        internal bool Add(IBindable bindable, ObjectData value)
        {
            if (bindable.DebugId.Id==2797)
            {
                ;
            }
            bool result = this.bag.Add(bindable,value);
            return result;
        }

        internal bool TryGet(IBindable name,out ObjectData info)
        {
            return this.bag.TryGetValue(name, out info);
        }

        public override string ToString()
        {
            IReadOnlyList<IBindable> keys = this.bag.Keys.StoreReadOnlyList();
            string result = keys.Select(it => it.Name.ToString()).Join(", ");
            if (keys.Count > 10)
                result += " ...";
            return result;
        }
    }
}
