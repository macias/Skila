using Skila.Language.Comparers;
using Skila.Language.Tools;
using System;
using System.Collections.Generic;

namespace Skila.Language.Entities
{
    public sealed class EntityInstanceCache
    {
        // building cache as static field within EntityInstance is tempting but it would mean
        // that (by default) entire cache is bigger and bigger as the tests run
        // cleaning cache manually would look ugly so we keep cache as non-static data outside EntityInstance

        private  readonly Dictionary<EntityInstance, EntityInstance.RuntimeCore> instancesCache;

        private readonly IEntity entity;
        private readonly Later<EntityInstance> instanceOf;
        public EntityInstance InstanceOf => this.instanceOf.Value;

        public EntityInstanceCache(IEntity entity, Func<EntityInstance> instanceOfCreator)
        {
            this.entity = entity;
            this.instancesCache = new Dictionary<EntityInstance, EntityInstance.RuntimeCore>(EntityInstanceCoreComparer.Instance);
            this.instanceOf = new Later<EntityInstance>(instanceOfCreator);
        }

        public EntityInstance GetInstance(IEnumerable<IEntityInstance> arguments, TypeMutability overrideMutability,
            TemplateTranslation translation, Lifetime lifetime)
        {
            EntityInstance instance = EntityInstance.CreateUnregistered(entity, arguments, translation, overrideMutability, lifetime);

            if (!instancesCache.TryGetValue(instance, out EntityInstance.RuntimeCore core))
            {
                core = new EntityInstance.RuntimeCore();
                instancesCache.Add(instance, core);
            }

            instance.SetCore(core);

            return instance;
        }
    }
}