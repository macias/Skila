using Skila.Language.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Skila.Language
{
    public interface IOptions
    {
        // in C# you cannot call "some_object.StaticMember", it has to be "SomeType.StaticMember"
        bool StaticMemberOnlyThroughTypeName { get; }
        // you can substitute prototype with any type as long all methods are covered
        bool InterfaceDuckTyping { get; }
        // when true, inner scope variable can shadow outer scope variable (with the same name)
        bool ScopeShadowing { get; }
        // in Skila we have "super" to call the base function of the current one, so supporting "base.foo()" for cross-calling
        // other base functions is rather not welcome because promote probably incorrect code
        // todo: btw. handling of "base" keyword is terrible, so fix it
        bool ReferencingBase { get; }
        // use it only on selected tests
        bool DiscardingAnyExpressionDuringTests { get; }
        bool GlobalVariables { get; }
        bool TypelessVariablesDuringTests { get; }
        bool DebugThrowOnError { get; } // useful when adding new test
        bool MiniEnvironment { get; }
    }

    public static class IOptionsExtension
    {
        public static IEnumerable<string> GetEnabledProperties<T>(this T options)
            where T : IOptions
        {
            return typeof(IOptions)
                            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                            .Where(it => it.GetValue(options).Cast<bool>())
                            .Select(it => it.Name)
                            .OrderBy(it => it);
        }
    }
}