using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Skila.Tests
{
    public sealed class AssertReporter
    {
        private static readonly HashSet<string> fails = new HashSet<string>();
        public static IEnumerable<string> Fails => fails;

        internal static void AreEqual<T>(T expected, T actual, [CallerFilePath]string callerFilePath = null, [CallerMemberName] string caller = null)
        {
            string callerTypeName = System.IO.Path.GetFileNameWithoutExtension(callerFilePath);
            if (!Object.Equals(expected, actual))
                fails.Add($"{callerTypeName}.{caller}");
        }

        internal static void IsTrue(bool condition, [CallerFilePath]string callerFilePath = null, [CallerMemberName] string caller = null)
        {
            string callerTypeName = System.IO.Path.GetFileNameWithoutExtension(callerFilePath);
            if (!condition)
                fails.Add($"{callerTypeName}.{caller}");
        }

        internal static void IsFalse(bool condition, [CallerFilePath]string callerFilePath = null, [CallerMemberName] string caller = null)
        {
            string callerTypeName = System.IO.Path.GetFileNameWithoutExtension(callerFilePath);
            if (condition)
                fails.Add($"{callerTypeName}.{caller}");
        }

        internal static void AreNotEqual<T>(T notExpected, T actual, [CallerFilePath]string callerFilePath = null, [CallerMemberName] string caller = null)
        {
            string callerTypeName = System.IO.Path.GetFileNameWithoutExtension(callerFilePath);
            if (Object.Equals(notExpected, actual))
                fails.Add($"{callerTypeName}.{caller}");
        }
    }
}
