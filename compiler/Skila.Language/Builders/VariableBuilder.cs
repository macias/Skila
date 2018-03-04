using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Skila.Language.Entities;
using Skila.Language.Expressions;

namespace Skila.Language.Builders
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class VariableBuilder : IBuilder<VariableDeclaration>
    {
        public static VariableBuilder CreateStatement(string name, INameReference typeName, IExpression initValue)
        {
            return new VariableBuilder( ExpressionReadMode.CannotBeRead, name, typeName, initValue);
        }

        private readonly string name;
        private EntityModifier modifier;
        private readonly ExpressionReadMode readMode;
        private readonly INameReference typeName;
        private readonly IExpression initValue;
        private IEnumerable<LabelReference> friends;

        private VariableDeclaration build;

        private VariableBuilder(ExpressionReadMode readMode,
                  string name,
                  INameReference typeName,
                  IExpression initValue)
        {
            this.name = name;
            this.readMode = readMode;
            this.typeName = typeName;
            this.initValue = initValue;
        }

        public VariableBuilder Modifier(EntityModifier modifier)
        {
            if (this.modifier != null || this.build != null)
                throw new InvalidOperationException();

            this.modifier = modifier;
            return this;
        }

        public VariableBuilder GrantAccess(params string[] friends)
        {
            if (this.friends != null || this.build != null)
                throw new InvalidOperationException();

            this.friends = friends.Select(it => LabelReference.CreateGlobal(it));
            return this;
        }

        public VariableDeclaration Build()
        {
            if (build == null)
                build = VariableDeclaration.Create(this.readMode, this.name, this.typeName, this.initValue, this.modifier, this.friends);
            return build;
        }
        public static implicit operator VariableDeclaration(VariableBuilder @this)
        {
            return @this.Build();
        }
    }
}
