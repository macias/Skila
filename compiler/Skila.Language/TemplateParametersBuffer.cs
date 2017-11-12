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
        public IEnumerable<TemplateParameter> Values
        {
            get
            {
                this.closed = true;
                return this.values;
            }
        }

        private bool closed;

        private TemplateParametersBuffer()
        {
            this.values = new List<TemplateParameter>();
        }
        public TemplateParametersBuffer Add(string name, VarianceMode mode = VarianceMode.None)
        {
            if (this.closed)
                throw new InvalidOperationException();

            this.values.Add(new TemplateParameter(this.values.Count, name, mode));
            return this;
        }
    }
}
