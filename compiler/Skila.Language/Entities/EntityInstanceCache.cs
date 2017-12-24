using System;
using System.Collections.Generic;

namespace Skila.Language.Entities
{
    public sealed class EntityInstanceCache
    {      
        // every template will hold each created instance of it, so for example List<T> can hold List<string>, List<int> and so on
        // the purpose -- to have just single instance per template+arguments
        private readonly Dictionary<EntityInstanceSignature, EntityInstance> instancesCache;
        private readonly IEntity entity;
        private readonly Lazy<EntityInstance> instanceOf;
        public EntityInstance InstanceOf => this.instanceOf.Value;

        public EntityInstanceCache(IEntity entity,Func<EntityInstance> instanceOfCreator) 
        {
            this.instancesCache = new Dictionary<EntityInstanceSignature, EntityInstance>();
            this.entity = entity;
            this.instanceOf = new Lazy<EntityInstance>(instanceOfCreator);
        }

        public EntityInstance GetInstance(IEnumerable<IEntityInstance> arguments,bool overrideMutability,TemplateTranslation translation)
        {
            var signature = new EntityInstanceSignature(arguments, overrideMutability,translation);

            EntityInstance result;
            if (!this.instancesCache.TryGetValue(signature, out result))
            {
                result = EntityInstance.RAW_CreateUnregistered(entity, signature);
                this.instancesCache.Add(signature, result);
            }

            return result;
        }
   }
}