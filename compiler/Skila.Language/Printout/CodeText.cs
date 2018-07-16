using Skila.Language.Tools;

namespace Skila.Language.Printout
{
    public sealed class CodeText : ICodeLine
    {
        private readonly string text;

        public CodeText(string s)
        {
            this.text = s;
        }

        public void Print(IPrinter printer)
        {
            printer.Write(this.text);
        }

        public override string ToString()
        {
            return this.text;
        }
    }
}
