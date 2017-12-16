namespace Skila.Interpreter
{
    // in Skila some types are stack based (values), in order to use C# reference types for simulating them we need
    // to wrap them in this interface which will indicate special copy (value-like)

    internal interface IInstanceValue
    {
        IInstanceValue Copy();
    }
}