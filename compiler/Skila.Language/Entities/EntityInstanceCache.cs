using Skila.Language.Comparers;
using System;
using System.Collections.Generic;

namespace Skila.Language.Entities
{
    public sealed class EntityInstanceCache
    {
        // every template will hold each created instance of it, so for example List<T> can hold List<string>, List<int> and so on
        // the purpose -- to have just single instance per template+arguments
        private readonly Dictionary<EntityInstanceCore,
            Tuple<EntityInstance, Dictionary<TemplateTranslation, EntityInstance>>> instancesCache;
        private readonly IEntity entity;
        private readonly Later<EntityInstance> instanceOf;
        public EntityInstance InstanceOf => this.instanceOf.Value;

        public EntityInstanceCache(IEntity entity, Func<EntityInstance> instanceOfCreator)
        {
            this.instancesCache = new Dictionary<EntityInstanceCore,
                Tuple<EntityInstance, Dictionary<
                    TemplateTranslation, EntityInstance>>>(EntityInstanceCoreSignatureComparer.Instance);
            this.entity = entity;
            this.instanceOf = new Later<EntityInstance>(instanceOfCreator);
        }

        public EntityInstance GetInstance(IEnumerable<IEntityInstance> arguments, TypeMutability overrideMutability,
            TemplateTranslation translation)
        {
            EntityInstanceCore core = EntityInstanceCore.RAW_CreateUnregistered(entity, arguments, overrideMutability);

            Tuple<EntityInstance, Dictionary<TemplateTranslation, EntityInstance>> family;
            if (!this.instancesCache.TryGetValue(core, out family))
            {
                // this is the base (core) entity instance, by "definition" always without translation
                EntityInstance base_instance = EntityInstance.RAW_CreateUnregistered(core, null);
                family = Tuple.Create(base_instance, new Dictionary<TemplateTranslation, EntityInstance>());
                this.instancesCache.Add(core, family);
            }

            EntityInstance result;
            if (translation == null)
                result = family.Item1;
            else if (!family.Item2.TryGetValue(translation, out result))
            {
                result = EntityInstance.RAW_CreateUnregistered(family.Item1.Core, translation);
                family.Item2.Add(translation, result);
            }

            return result;
        }
    }
}