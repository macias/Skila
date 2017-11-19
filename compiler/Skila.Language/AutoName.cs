using NaiveLanguageTools.Common;

namespace Skila.Language
{
    internal sealed class AutoName
    {
        private const string AutoNameMarker = "\\";

        private DynamicDictionary<string, int> counters;

        public AutoName()
        {
            this.counters = DynamicDictionary.CreateWithDefault<string, int>();
        }
        public string CreateNew(string hint = "")
        {
            int counter = counters[hint]++;
            return AutoNameMarker + hint + (counter == 0 && hint != "" ? "" : $"_{counter}");
        }
    }
}
