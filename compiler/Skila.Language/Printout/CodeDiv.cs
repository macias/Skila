using Skila.Language.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Language.Printout
{
    public sealed class CodeDiv : ICode
    {
        private readonly INode node;
        private List<Tuple<bool, ICode>> codes;

        public CodeDiv(INode node,params ICode[] codes)
        {
            this.node = node;
            this.codes = codes.Select(it => Tuple.Create(false, it)).ToList();
        }

/*        public CodeDiv(string s) : this(new CodeText(s))
        {
        }
        */
        public CodeDiv(INode node, params IPrintable[] printable) : this(node,printable.Select(it => it.Printout()).ToArray())
        {

        }

        public CodeDiv Indent()
        {
            this.codes = this.codes.Select(it => Tuple.Create(true, it.Item2)).ToList();
            return this;
        }

        public CodeDiv Prepend(ICode code)
        {
            this.codes.Insert(0, Tuple.Create(false, code));
            return this;
        }

        public CodeDiv Append(ICode code)
        {
            this.codes.Add(Tuple.Create(false, code));
            return this;
        }
        public CodeDiv Append(IPrintable printable)
        {
            return this.Append(printable.Printout());
        }
        public CodeDiv Prepend(string s)
        {
            return Prepend(new CodeText(s));
        }

        public CodeDiv Append(string s)
        {
            return Append(new CodeText(s));
        }
        public void Print(IPrinter printer)
        {
            foreach (var elem in this.codes)
            {
                if (elem.Item1)
                    printer.IncreaseIndent();
                elem.Item2.Print(printer);
                if (elem.Item1)
                    printer.DescreaseIndent();
                printer.WriteLine();
            }
        }

        public override string ToString()
        {
            return this.codes.First().ToString();
        }
    }
}
