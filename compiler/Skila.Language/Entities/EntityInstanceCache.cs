using NaiveLanguageTools.Common;
using Skila.Language.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Language.Entities
{
    public sealed class EntityInstanceCache
    {
        private sealed class CoreCache
        {
            private static readonly int mutabilitySize;
            private readonly EntityInstance.RuntimeCore core;
            private readonly Dictionary<Lifetime, EntityInstance[]> cache;

            static CoreCache()
            {
                mutabilitySize = EnumExtensions.GetValues<TypeMutability>().Select(it => (int)it).Max() * 2;
            }
            internal CoreCache()
            {
                this.core = new EntityInstance.RuntimeCore();
                this.cache = new Dictionary<Lifetime, EntityInstance[]>();
            }

            internal EntityInstance GetInstance(IEntity entity, 
                TypeMutability overrideMutability, TemplateTranslation translation, Lifetime lifetime)
            {
                if (!this.cache.TryGetValue(lifetime, out EntityInstance[] inner_cache))
                {
                    inner_cache = new EntityInstance[mutabilitySize];
                    this.cache.Add(lifetime, inner_cache);
                }

                EntityInstance instance = inner_cache[(int)overrideMutability];
                if (instance == null)
                {
                    instance = EntityInstance.CreateUnregistered(this.core, entity, translation,
                        overrideMutability, lifetime);
                    inner_cache[(int)overrideMutability] = instance;
                }

                return instance;
            }
        }

        // building cache as static field within EntityInstance is tempting but it would mean
        // that (by default) entire cache is bigger and bigger as the tests run
        // cleaning cache manually would look ugly so we keep cache as non-static data outside EntityInstance

        private readonly Dictionary<TemplateTranslation, CoreCache> instancesCache;

        private readonly IEntity entity;
        private readonly Later<EntityInstance> instanceOf;
        public EntityInstance InstanceOf => this.instanceOf.Value;

        public EntityInstanceCache(IEntity entity, Func<EntityInstance> instanceOfCreator)
        {
            this.entity = entity;
            this.instancesCache = new Dictionary<TemplateTranslation, CoreCache>();
            this.instanceOf = Later.Create(instanceOfCreator);
        }

        public EntityInstance GetInstance( TypeMutability overrideMutability,
            TemplateTranslation translation, Lifetime lifetime)
        {
            translation = translation ?? TemplateTranslation.Empty;

            if (!instancesCache.TryGetValue(translation, out CoreCache core_cache))
            {
                core_cache = new CoreCache();
                instancesCache.Add(translation, core_cache);
            }

            return core_cache.GetInstance(this.entity, overrideMutability, translation, lifetime);
        }
    }
}