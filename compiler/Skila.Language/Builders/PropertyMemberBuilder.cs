using System;
using System.Diagnostics;
using Skila.Language.Entities;
using System.Collections.Generic;
using Skila.Language.Extensions;
using System.Linq;

namespace Skila.Language.Builders
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class PropertyMemberBuilder
    {
        private enum MemberType
        {
            IndexGetter,
            IndexSetter,
        }
        public static PropertyMemberBuilder CreateIndexerGetter(params FunctionParameter[] parameters)
        {
            return new PropertyMemberBuilder(MemberType.IndexGetter, parameters);
        }
        public static PropertyMemberBuilder CreateIndexerSetter(params FunctionParameter[] parameters)
        {
            return new PropertyMemberBuilder(MemberType.IndexSetter, parameters);
        }

        private readonly MemberType memberType;
        private IMember build;
        private readonly IEnumerable<FunctionParameter> parameters;
        private EntityModifier modifier;
        private IEnumerable<IExpression> instructions;

        private PropertyMemberBuilder(MemberType memberType, IEnumerable<FunctionParameter> parameters)
        {
            this.memberType = memberType;
            this.parameters = parameters.StoreReadOnly();
        }


        public PropertyMemberBuilder Modifier(EntityModifier modifier)
        {
            if (build != null)
                throw new Exception();

            this.modifier = modifier | this.modifier;
            return this;
        }

        public PropertyMemberBuilder Code(params IExpression[] instructions)
        {
            if (build != null || this.instructions != null)
                throw new Exception();

            this.instructions = instructions.StoreReadOnly();
            return this;
        }

        internal IMember Build(NameReference typename)
        {
            if (build == null)
            {
                switch (memberType)
                {
                    case MemberType.IndexGetter:
                        build = Property.CreateIndexerGetter(typename, parameters, modifier, instructions?.ToArray());
                        break;
                    case MemberType.IndexSetter:
                        build = Property.CreateIndexerSetter(typename, parameters, modifier, instructions?.ToArray());
                        break;
                    default: throw new Exception();
                }
            }

            return build;
        }
    }
}
