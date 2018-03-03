﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Comparers;
using Skila.Language.Data;
using Skila.Language.Extensions;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using System;

namespace Skila.Language
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class NameRegistry : ILayeredNameRegistry
    {
        private readonly ILayerDictionary<ITemplateName, LocalInfo> bag;
        private readonly Stack<IScope> scopes;

        public NameRegistry(bool shadowing)
        {
            this.bag = LayerDictionary.Create<ITemplateName, LocalInfo>(shadowing,EntityNameArityComparer.Instance);
            this.scopes = new Stack<IScope>();
        }

        public void AddLayer(IScope scope)
        {
            if (scope != null)
            {
                this.bag.PushLayer();
                this.scopes.Push(scope);
            }
        }

        internal IEnumerable<LocalInfo> RemoveLayer()
        {
            this.scopes.Pop();
            return this.bag.PopLayer().Select(it => it.Item2);
        }

        public IScope LastLayer()
        {
            return this.scopes.Peek();
        }


        internal bool Add(ILocalBindable bindable)
        {
            if (bindable is VariableDeclaration && bindable.DebugId.Id== 59)
            {
                ;
            }
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
            {
                info.Used = true;
                if ((name.Owner as Assignment)?.Lhs != name)
                {
                    info.Read = true;
                }
            }
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
