using Skila.Language.Entities;
using Skila.Language.Extensions;
using Skila.Language.Printout;

namespace Skila.Language.Tools
{
    public static class Printer
    {
        public static readonly IPrinter Console = new ConsolePrinter();

        public static void PrintFunction(this INode node)
        {
            IPrintable func;
            if (node is FunctionDefinition f)
                func = f;
            else
                func = node.EnclosingScope<FunctionDefinition>();

            func.Printout().Print(Printer.Console);
        }
    }
}
