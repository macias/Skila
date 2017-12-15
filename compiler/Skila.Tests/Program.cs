using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using Skila.Language;
using Skila.Language.Extensions;
using Skila.Language.Semantics;
using NaiveLanguageTools.Common;
using Skila.Interpreter;

namespace Skila.Tests
{
    class Program
    {

        public static void Main()
        {
            //   new Semantics.CompilerProtection().Environment();
            // new Semantics.Concurrency().ErrorSpawningMutables();
            // new Semantics.Exceptions().ErrorThrowingNonException();
            // new Semantics.Expressions().ErrorSelfAssignment();
            //  new Semantics.Flow().ErrorReadingIfWithoutElse();
            //new Semantics.FunctionCalls().ProperMethodCallTypeInference();
            //  new Semantics.FunctionDefinitions().ErrorUsingDisabledParameters();
            //new Semantics.Inheritance().ErrorEnumCrossInheritance();
            //new Semantics.MemoryClasses().ImplicitValueReferenceConversionOnCall();
            //new Semantics.MethodDefinitions().Basics();
            //new Semantics.Mutability().ErrorAssigningMutableToImmutable();
            // new Semantics.NameResolution().ResolvingIt();
            //  new Semantics.OverloadCalls().PreferringNonVariadicFunction();
               new Semantics.Properties().ErrorIgnoringGetter();
            //new Semantics.Templates().ErrorHasConstraint();
            //new Semantics.TypeMatching().UnionMatching();
            //new Semantics.Types().ErrorInstanceMemberReference();
            //new Semantics.Variables().ErrorVariableNotUsed();

            //  new Execution.Closures().ResultTypeInference();
            //new Execution.Concurrency().SingleMessage();
           //  new Execution.FunctionCalls().ReturningUnit();
            //new Execution.Inheritance().VirtualCall();
            //new Execution.Interfaces().TraitFunctionCall();
            //new Execution.Objects().UsingEnums();
            //new Execution.Pointers().ExplicitDereferencing();
            //new Execution.Properties().Indexer();
            //new Execution.Templates().HasConstraintWithValue();

            {
                double start = Stopwatch.GetTimestamp();
                runTest<IErrorReporter>(nameof(Semantics), checkErrorCoverage: true);

                Console.WriteLine($"Semantics time: {(Stopwatch.GetTimestamp() - start) / Stopwatch.Frequency}s");
            }

            Console.WriteLine();

            {
                double start = Stopwatch.GetTimestamp();
                runTest<IInterpreter>(nameof(Execution), checkErrorCoverage: false);

                Console.WriteLine($"Interpretation time: {(Stopwatch.GetTimestamp() - start) / Stopwatch.Frequency}s");
            }

            if (AssertX.Fails.Any())
            {
                Console.WriteLine("Fails");
                foreach (string s in AssertX.Fails.OrderBy(it => it))
                    Console.WriteLine(s);
            }
            Console.ReadLine();
        }

        private static void runTest<T>(string @namespace, bool checkErrorCoverage)
            where T : class
        {
            HashSet<ErrorCode> reported_errors = checkErrorCoverage ? new HashSet<ErrorCode>() : null;
            var missed_atrr = new List<string>();
            int failed = 0;
            int total = 0;
            Assembly mscorlib = typeof(Program).Assembly;
            foreach (Type type in mscorlib.GetTypes()
                .Where(it => it.GetCustomAttributes<TestClassAttribute>().Any())
                .Where(it => it.Namespace.EndsWith(@namespace))
                .OrderBy(it => it.Name))
            {
                missed_atrr.AddRange(runTest<T>(type, ref total, ref failed, reported_errors));
            }

            if (checkErrorCoverage)
            {
                HashSet<ErrorCode> all_errors = Tools.GetEnumValues<ErrorCode>().Where(it => !$"{it}".StartsWith("NOTEST")).ToHashSet();
                all_errors.ExceptWith(reported_errors);
                if (all_errors.Any())
                {
                    var fc = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine();
                    Console.WriteLine("Missed coverage for following errors:");
                    Console.ForegroundColor = fc;
                    foreach (ErrorCode e in all_errors)
                        Console.WriteLine(e);
                }
                var no_tests = reported_errors.Where(it => $"{it}".StartsWith("NOTEST")).ToArray();
                if (no_tests.Any())
                {
                    var fc = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine();
                    Console.WriteLine("Following errors are covered by tests, change their names:");
                    Console.ForegroundColor = fc;
                    foreach (ErrorCode e in no_tests)
                        Console.WriteLine(e);
                }
            }

            if (missed_atrr.Any())
            {
                var fc = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine();
                Console.WriteLine("Missed unit test attribute:");
                Console.ForegroundColor = fc;
                foreach (string s in missed_atrr)
                    Console.WriteLine(s);
            }

            Console.WriteLine();
            {
                var fc = Console.ForegroundColor;
                if (failed == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"All {total} tests passed.");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{failed} tests failed out of {total}.");
                }
                Console.ForegroundColor = fc;
            }
        }

        private static IEnumerable<string> runTest<T>(Type type, ref int total, ref int failed, HashSet<ErrorCode> errors)
            where T : class
        {
            int init_fails = failed;

            var miss_attr = new List<string>();
            object test = Activator.CreateInstance(type);
            foreach (System.Reflection.MethodInfo method in test.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .OrderBy(it => it.Name))
            {
                ++total;

                try
                {
                    if (!method.GetCustomAttributes(typeof(TestMethodAttribute), false).Any())
                        miss_attr.Add(type.Name + "." + method.Name);
                    else
                    {
                        string test_name = $"{type.Name}.{method.Name}";
                        Console.Write(test_name);
                        // this is not just dumb casting -- it checks if the given test returns the expected object
                        // so for example, semantic test is not mixed with interpretation test
                        T result = method.Invoke(test, new object[] { }).Cast<T>();
                        if (result == null)
                            throw new Exception("Internal error");
                        if (result is IErrorReporter reporter)
                            errors.AddRange(reporter.Errors.Select(it => it.Code));
                        ClearWrite(test_name);
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(" FAILED: " + ex.InnerException.StackTrace.Split('\n').FirstOrDefault());
                    ++failed;
                }
            }

            if (init_fails == failed)
            {
                Console.WriteLine($"{type.Name} passed.");
            }

            return miss_attr;
        }

        private static void ClearWrite(string s)
        {
            Console.Write(new string('\b', s.Length) + new string(' ', s.Length) + new string('\b', s.Length));
        }
    }
}
