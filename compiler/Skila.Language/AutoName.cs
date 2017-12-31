using NaiveLanguageTools.Common;

namespace Skila.Language
{
    internal sealed class AutoName
    {
        internal static readonly AutoName Instance = new AutoName();

        private const string autoNameMarker = "\\";

        private DynamicDictionary<string, int> counters;

        private AutoName()
        {
            this.counters = DynamicDictionary.CreateWithDefault<string, int>();
        }
        public string CreateNew(string hint = "")
        {
            int counter = counters[hint]++;
            return autoNameMarker + hint + (counter == 0 && hint != "" ? "" : $"_{counter}");
        }
    }
}
