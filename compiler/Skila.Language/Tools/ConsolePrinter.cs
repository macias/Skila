using System;

namespace Skila.Language.Tools
{
    public sealed class ConsolePrinter : IPrinter
    {
        private int indentation;
        private bool beginOfLine;

        public ConsolePrinter()
        {
            beginOfLine = true;
        }
        public void WriteLine(string s = "")
        {
            indent();
            Console.WriteLine(s);
            this.beginOfLine = true;
        }

        private void indent()
        {
            if (this.beginOfLine)
                //                Console.Write(new string('·', indentation * 2));
                Console.Write(new string(' ', indentation * 2));
        }

        public void Write(string s)
        {
            indent();
            Console.Write(s);
            this.beginOfLine = false;
        }

        public void IncreaseIndent()
        {
            ++indentation;
        }
        public void DescreaseIndent()
        {
            --indentation;
        }
    }
}
