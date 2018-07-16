using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Extensions;
using Skila.Language.Builders;
using Skila.Language.Tools;
using Skila.Language.Printout;

namespace Skila.Language
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class NameDefinition : Node, ITemplateName, ITemplateParameters,IPrintable
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
            return Printout().ToString();
        }

        public ICode Printout()
        {
            var code = new CodeSpan(Name);
            if (Parameters.Any())
            {
                code.Append("<");
                code.Append(Parameters, ",");
                code.Append(">");
            }
            return code;
        }

        public NameReference CreateNameReference(IExpression prefix, EntityInstance targetInstance, bool isLocal)
        {
            return NameReference.Create(prefix, this.Name,
                this.Parameters.Select(it => NameReference.Create(it.Name)), targetInstance, isLocal);
        }
        public NameReference CreateNameReference(IExpression prefix, TypeMutability mutability)
        {
            return NameReference.Create(mutability, prefix, this.Name,
                this.Parameters.Select(it => NameReference.Create(it.Name)), target: null, isLocal: false);
        }
    }

}
