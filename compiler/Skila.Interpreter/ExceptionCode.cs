using System.Runtime.CompilerServices;

namespace Skila.Interpreter
{
    static class ExceptionCode
    {
        internal static string SourceInfo([CallerFilePath] string file = "",
                        [CallerMemberName] string member = "",
                        [CallerLineNumber] int line = 0)
        {
            return $"{member} {file}:{line}";
        }
    }
}
