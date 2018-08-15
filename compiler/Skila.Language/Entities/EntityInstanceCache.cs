﻿using Skila.Language.Comparers;
using Skila.Language.Extensions;
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
            private readonly EntityInstance.RuntimeCore core;
            private readonly Dictionary<TypeMutability, Dictionary<Lifetime, EntityInstance>> cache;

            internal CoreCache()
            {
                this.core = new EntityInstance.RuntimeCore();
                this.cache = new Dictionary<TypeMutability, Dictionary<Lifetime, EntityInstance>>();
            }

            internal EntityInstance GetInstance(IEntity entity, IReadOnlyList<IEntityInstance> arguments,
                TypeMutability overrideMutability, TemplateTranslation translation, Lifetime lifetime)
            {
                if (!this.cache.TryGetValue(overrideMutability, out var inner_cache))
                {
                    inner_cache = new Dictionary<Lifetime, EntityInstance>();
                    this.cache.Add(overrideMutability, inner_cache);
                }

                if (!inner_cache.TryGetValue(lifetime, out EntityInstance instance))
                {
                    instance = EntityInstance.CreateUnregistered(this.core, entity, arguments, translation,
                        overrideMutability, lifetime);
                    inner_cache.Add(lifetime, instance);
                }

                return instance;
            }
        }

        // building cache as static field within EntityInstance is tempting but it would mean
        // that (by default) entire cache is bigger and bigger as the tests run
        // cleaning cache manually would look ugly so we keep cache as non-static data outside EntityInstance

        private readonly NullableDictionary<TemplateTranslation, Dictionary<IReadOnlyList<IEntityInstance>, CoreCache>> instancesCache;

        private readonly IEntity entity;
        private readonly Later<EntityInstance> instanceOf;
        public EntityInstance InstanceOf => this.instanceOf.Value;

        public EntityInstanceCache(IEntity entity, Func<EntityInstance> instanceOfCreator)
        {
            this.entity = entity;
            this.instancesCache = new NullableDictionary<TemplateTranslation, Dictionary<IReadOnlyList<IEntityInstance>, CoreCache>>();
            this.instanceOf = new Later<EntityInstance>(instanceOfCreator);
        }

        public EntityInstance GetInstance(IEnumerable<IEntityInstance> arguments, TypeMutability overrideMutability,
            TemplateTranslation translation, Lifetime lifetime)
        {
            if (!instancesCache.TryGetValue(translation, out var inner_cache))
            {
                inner_cache = new Dictionary<IReadOnlyList<IEntityInstance>, CoreCache>(EntityInstanceArgumentsComparer.Instance);
                instancesCache.Add(translation, inner_cache);
            }

            var list_args = (arguments ?? Enumerable.Empty<IEntityInstance>()).StoreReadOnlyList();
            if (!inner_cache.TryGetValue(list_args, out var core_cache))
            {
                core_cache = new CoreCache();
                inner_cache.Add(list_args, core_cache);
            }

            return core_cache.GetInstance(this.entity, list_args, overrideMutability, translation, lifetime);
        }
    }
}