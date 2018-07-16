using NaiveLanguageTools.Common;
using Skila.Language.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Language.Printout
{
    public sealed class CodeSpan : ICodeLine
    {
        private readonly List<ICode> codes;
        private readonly INode node;

        public CodeSpan(params ICode[] codes)
        {
            this.codes = codes.ToList();
        }

        public CodeSpan(string s) : this(new CodeText(s))
        {
        }

        public CodeSpan(IPrintable expr) : this(expr.Printout())
        {
        }
        public CodeSpan(INode node, IPrintable expr) : this(expr.Printout())
        {
            this.node = node;
        }

        public CodeSpan Prepend(ICode code)
        {
            this.codes.Insert(0, code);
            return this;
        }
        public CodeSpan Prepend(IPrintable printable)
        {
            this.Prepend(printable.Printout());
            return this;
        }

        public CodeSpan Append(IEnumerable<IPrintable> printables, string separator)
        {
            bool first = true;
            foreach (IPrintable print in printables)
            {
                if (!first)
                    this.Append(separator);
                this.Append(print);
                first = false;
            }
            return this;
        }
        public CodeSpan Append(ICode code)
        {
            this.codes.Add(code);
            return this;
        }
        public CodeSpan Prepend(string s)
        {
            return Prepend(new CodeText(s));
        }

        public CodeSpan Append(string s)
        {
            return Append(new CodeText(s));
        }

        public CodeSpan Append(IPrintable expr)
        {
            if (expr is null)
                throw new ArgumentNullException();
            //return Append(expr?.Printout() ?? new CodeText("Ø"));
            return Append(expr.Printout());
        }

        public void Print(IPrinter printer)
        {
            if (node != null)
                printer.Write($"«{node.DebugId}»");
            foreach (ICode code in this.codes)
                code.Print(printer);
        }

        public override string ToString()
        {
            string result = "";
            if (node != null)
                result = $"«{node.DebugId}»";

            result += this.codes
                .Select(it => it as ICodeLine)
                .TakeWhile(it => it != null)
                .Select(it => it.ToString())
                .Join("");

            return result;
        }
    }
}
