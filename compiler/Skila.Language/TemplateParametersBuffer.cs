using Skila.Language.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Language
{
    public sealed class TemplateParametersBuffer
    {
        public static TemplateParametersBuffer Create()
        {
            return new TemplateParametersBuffer();
        }

        private readonly List<TemplateParameter> values;
        public IEnumerable<TemplateParameter> Values { get { store(); return this.values; } }

        private sealed class Buffer
        {
            internal string Name;
            internal VarianceMode Mode;
            internal EntityModifier ConstraintModifier;
            internal List<NameReference> Inherits;
            internal List<NameReference> BaseOf;
            internal List<FunctionDefinition> Functions;

            internal Buffer()
            {
                this.Functions = new List<FunctionDefinition>();
                this.Inherits = new List<NameReference>();
                this.BaseOf = new List<NameReference>();
            }
        }

        private Buffer buffer;

        private TemplateParametersBuffer()
        {
            this.values = new List<TemplateParameter>();
        }
        private void store()
        {
            if (buffer != null)
                this.values.Add(new TemplateParameter(this.values.Count, buffer.Name, buffer.Mode, buffer.ConstraintModifier,
                    buffer.Functions, buffer.Inherits, buffer.BaseOf));
            buffer = null;
        }
        public TemplateParametersBuffer Add(string name, VarianceMode mode = VarianceMode.None)
        {
            store();
            this.buffer = new Buffer() { Name = name, Mode = mode };
            return this;
        }
        public TemplateParametersBuffer With(EntityModifier constraintModifier)
        {
            if (this.buffer.ConstraintModifier != null)
                throw new ArgumentException();
            this.buffer.ConstraintModifier = constraintModifier;
            return this;
        }
        public TemplateParametersBuffer Inherits(string inherits)
        {
            this.buffer.Inherits.Add(NameReference.Create(inherits));
            return this;
        }
        public TemplateParametersBuffer BaseOf(string baseOf)
        {
            this.buffer.BaseOf.Add(NameReference.Create(baseOf));
            return this;
        }
        public TemplateParametersBuffer Has(FunctionDefinition func)
        {
            this.buffer.Functions.Add(func);
            return this;
        }
    }
}
