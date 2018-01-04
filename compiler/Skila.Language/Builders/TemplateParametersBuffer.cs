using NaiveLanguageTools.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Language.Builders
{
    public sealed class TemplateParametersBuffer
    {
        public static TemplateParametersBuffer Create(params string[] names)
        {
            return new TemplateParametersBuffer(VarianceMode.None, names);
        }
        public static TemplateParametersBuffer Create(VarianceMode mode, params string[] names)
        {
            return new TemplateParametersBuffer(mode, names);
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

        private TemplateParametersBuffer(VarianceMode mode, IEnumerable<string> names)
        {
            this.values = new List<TemplateParameter>();
            if (names != null)
                names.ForEach(s => Add(s, mode));
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
