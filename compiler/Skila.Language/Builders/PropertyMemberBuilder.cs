using System;
using System.Diagnostics;
using Skila.Language.Entities;
using Skila.Language.Expressions;

namespace Skila.Language.Builders
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class PropertyMemberBuilder
    {
        private enum MemberType
        {
            IndexGetter,
            IndexSetter,
            Getter,
        }
        public static PropertyMemberBuilder CreateGetter(Block body)
        {
            PropertyMemberBuilder builder = new PropertyMemberBuilder(MemberType.Getter, body);
            return builder;
        }
        public static PropertyMemberBuilder CreateIndexerGetter(Block body)
        {
            PropertyMemberBuilder builder = new PropertyMemberBuilder(MemberType.IndexGetter, body);
            return builder;
        }
        public static PropertyMemberBuilder CreateIndexerSetter(Block body)
        {
            PropertyMemberBuilder builder = new PropertyMemberBuilder(MemberType.IndexSetter,body);
            return builder;
        }

        private readonly MemberType memberType;
        private IMember build;
        private EntityModifier modifier;
        private readonly Block body;

        private PropertyMemberBuilder(MemberType memberType,Block body)
        {
            this.memberType = memberType;
            this.body = body;
        }


        public PropertyMemberBuilder Modifier(EntityModifier modifier)
        {
            if (build != null)
                throw new Exception();

            this.modifier = modifier | this.modifier;
            return this;
        }

        internal IMember Build(PropertyBuilder propertyBuilder)
        {
            if (build == null)
            {
                switch (memberType)
                {
                    case MemberType.IndexGetter:
                        build = Property.CreateIndexerGetter(propertyBuilder.ValueTypeName, propertyBuilder.Params, modifier, 
                            body);
                        break;
                    case MemberType.IndexSetter:
                        build = Property.CreateIndexerSetter(propertyBuilder.ValueTypeName, propertyBuilder.Params, modifier, 
                            body);
                        break;
                    case MemberType.Getter:
                        build = Property.CreateGetter(propertyBuilder.ValueTypeName, body, modifier);
                        break;
                    default: throw new Exception();
                }
            }

            return build;
        }
    }
}
