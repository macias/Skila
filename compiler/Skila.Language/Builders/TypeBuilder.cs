using System;
using System.Diagnostics;
using System.Linq;
using Skila.Language.Entities;
using NaiveLanguageTools.Common;
using System.Collections.Generic;

namespace Skila.Language.Builders
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class TypeBuilder : IBuilder<TypeDefinition>
    {
        public static TypeBuilder Create(NameDefinition name)
        {
            return new TypeBuilder(name);
        }
        public static TypeBuilder Create(string name,params string[] typeParameters)
        {
            var buff = TemplateParametersBuffer.Create();
            typeParameters.ForEach(it => buff.Add(it,VarianceMode.None));
            return new TypeBuilder(NameDefinition.Create(name, buff.Values));
        }

        private readonly NameDefinition name;
        private readonly List<INode> features;
        private NameReference[] parents;
        private EntityModifier modifier;
        private TypeDefinition build;
        private bool allowSlicing;
        private bool isPlain;

        private TypeBuilder(NameDefinition name)
        {
            this.name = name;
            this.features = new List<INode>();
        }
        public TypeBuilder Parents(params NameReference[] parents)
        {
            if (this.parents != null || this.build != null)
                throw new InvalidOperationException();

            this.parents = parents;
            return this;
        }
        public TypeBuilder Parents(params string[] parents)
        {
            return this.Parents(parents.Select(it => NameReference.Create(it)).ToArray());
        }

        internal TypeBuilder Plain(bool isPlain)
        {
            this.isPlain = isPlain;
            return this;
        }

        public TypeBuilder With(INode node)
        {
            this.features.Add(node);
            return this;
        }

        public TypeBuilder Slicing(bool allow)
        {
            this.allowSlicing = allow;
            return this;
        }

        public TypeBuilder Modifier(EntityModifier modifier)
        {
            if (this.modifier != null || this.build != null)
                throw new InvalidOperationException();

            this.modifier = modifier;
            return this;
        }

        public TypeDefinition Build()
        {
            if (build == null)
                build = TypeDefinition.Create(isPlain,
                    this.modifier ?? EntityModifier.None,
                    this.name, 
                    allowSlicing,
                    this.parents,
                    features);
            return build;
        }
        public static implicit operator TypeDefinition(TypeBuilder @this)
        {
            return @this.Build();
        }
    }
}
