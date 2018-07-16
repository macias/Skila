namespace Skila.Language.Tools
{
    public interface IPrinter
    {
        void WriteLine(string s = "");
        void Write(string s);

        void IncreaseIndent();
        void DescreaseIndent();
    }

}
