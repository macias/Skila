using Skila.Language.Entities;
using Skila.Language.Extensions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Language
{
    public sealed class ScopeTable : IEnumerable<EntityInstance>
    {
        private readonly IReadOnlyCollection<EntityInstance> entities;
        private readonly Dictionary<string, List<List<EntityInstance>>> dict;

        public ScopeTable(IEnumerable<EntityInstance> entities)
        {
            this.entities = entities.StoreReadOnly();
            this.dict = new Dictionary<string, List<List<EntityInstance>>>();

            if (!this.entities.Any())
                return;

            int arities = 1 + this.entities.Select(it => it.Target.Name.Arity).Max();
            foreach (EntityInstance instance in this.entities)
            {
                IEntity entity = instance.Target;
                NameDefinition name = entity.Name;
                if (!this.dict.TryGetValue(name.Name, out List<List<EntityInstance>> list))
                {
                    list = Enumerable.Range(0, arities).Select(_ => (List<EntityInstance>)null).ToList();
                    list[0] = new List<EntityInstance>();
                    this.dict.Add(name.Name, list);
                }

                if (name.Arity == 0)
                    list[0].Add(instance);
                else
                {
                    list[name.Arity] = list[name.Arity] ?? new List<EntityInstance>();
                    list[name.Arity].Add(instance);

                    if (entity is FunctionDefinition)
                        list[0].Add(instance.BuildNoArguments());
                }
            }
        }


        public IEnumerator<EntityInstance> GetEnumerator()
        {
            return this.entities.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        internal IEnumerable<EntityInstance> Find(ITemplateName name)
        {
            if (!this.dict.TryGetValue(name.Name, out List<List<EntityInstance>> list) || name.Arity >= list.Count)
                return Enumerable.Empty<EntityInstance>();

            return list[name.Arity] ?? Enumerable.Empty<EntityInstance>();
        }

        internal static ScopeTable Combine(ScopeTable table, IEnumerable<EntityInstance> entities)
        {
            return new ScopeTable(table.Concat(entities));
        }
    }
}