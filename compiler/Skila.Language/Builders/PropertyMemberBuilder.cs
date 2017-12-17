using System;
using System.Diagnostics;
using Skila.Language.Entities;
using System.Collections.Generic;
using Skila.Language.Extensions;
using System.Linq;
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
        public static PropertyMemberBuilder CreateGetter(params IExpression[] instructions)
        {
            PropertyMemberBuilder builder = new PropertyMemberBuilder(MemberType.Getter, instructions);
            return builder;
        }
        public static PropertyMemberBuilder CreateIndexerGetter(params IExpression[] instructions)
        {
            PropertyMemberBuilder builder = new PropertyMemberBuilder(MemberType.IndexGetter, instructions);
            return builder;
        }
        public static PropertyMemberBuilder CreateIndexerSetter(params IExpression[] instructions)
        {
            PropertyMemberBuilder builder = new PropertyMemberBuilder(MemberType.IndexSetter,instructions);
            return builder;
        }

        private readonly MemberType memberType;
        private IMember build;
        private EntityModifier modifier;
        private readonly IEnumerable<IExpression> instructions;

        private PropertyMemberBuilder(MemberType memberType,params IExpression[] instructions)
        {
            this.memberType = memberType;
            this.instructions = instructions.StoreReadOnly();
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
                        build = Property.CreateIndexerGetter(propertyBuilder.Typename, propertyBuilder.Params, modifier, 
                            instructions?.ToArray());
                        break;
                    case MemberType.IndexSetter:
                        build = Property.CreateIndexerSetter(propertyBuilder.Typename, propertyBuilder.Params, modifier, 
                            instructions?.ToArray());
                        break;
                    case MemberType.Getter:
                        build = Property.CreateGetter(propertyBuilder.Typename, Block.CreateStatement(instructions), modifier);
                        break;
                    default: throw new Exception();
                }
            }

            return build;
        }
    }
}
