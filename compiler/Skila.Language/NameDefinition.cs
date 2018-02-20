using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Extensions;
using Skila.Language.Builders;

namespace Skila.Language
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class NameDefinition : Node, ITemplateName, ITemplateParameters
    {
        public static NameDefinition Create(string name, IEnumerable<TemplateParameter> parameters)
        {
            return new NameDefinition(name, parameters);
        }
        public static NameDefinition Create(string name, string paramName, VarianceMode variance)
        {
            return new NameDefinition(name, TemplateParametersBuffer.Create().Add(paramName, variance).Values);
        }
        public static NameDefinition Create(string name)
        {
            return new NameDefinition(name, Enumerable.Empty<TemplateParameter>());
        }

        public string Name { get; }
        public IReadOnlyList<TemplateParameter> Parameters { get; }
        public int Arity => this.Parameters.Count;

        public override IEnumerable<INode> OwnedNodes => this.Parameters;

        private NameDefinition(string name, IEnumerable<TemplateParameter> parameters)
            : base()
        {
            if (name == null)
                throw new ArgumentNullException();

            this.Name = name;
            this.Parameters = parameters.StoreReadOnlyList();

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }
        public override string ToString()
        {
            string parameters = "";
            if (Parameters.Any())
                parameters = "<" + Parameters.Select(it => it.ToString()).Join(",") + ">";
            return Name + parameters;
        }

        public NameReference CreateNameReference(IExpression prefix, EntityInstance targetInstance = null)
        {
            return NameReference.Create(prefix, this.Name,
                this.Parameters.Select(it => NameReference.Create(it.Name)), targetInstance);
        }
        public NameReference CreateNameReference(IExpression prefix, MutabilityFlag mutability, EntityInstance targetInstance = null)
        {
            return NameReference.Create(mutability, prefix, this.Name,
                this.Parameters.Select(it => NameReference.Create(it.Name)), targetInstance);
        }
    }

}
