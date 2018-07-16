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
        private static int testOffset = 0;
        private static int testIndex;

        public static void Main()
        {
            {
                new Semantics.CompilerProtection().Environment();
                // new Semantics.Concurrency().ErrorSpawningMutables();
                // new Semantics.Exceptions().ErrorThrowingNonException();
                //  new Semantics.Expressions().ErrorIsSameOnValues();
                //new Semantics.Extensions().ErrorInvalidDefinitions();
                //new Semantics.Flow().ErrorUnreachableCodeAfterBreakSingleReport();
                //   new Semantics.FunctionCalls().ErrorVariadicFunctionMissingSpread();
                //new Semantics.FunctionDefinitions().ErrorCannotInferResultType();
                // new Semantics.Interfaces().ErrorDuckTypingInterfaceValues();
                //new Semantics.Inheritance().ProperGenericWithCostraintsMethodOverride();
                new Semantics.Lifetimes().ErrorLocalVariableReferenceEscapesFromFunction();
                // new Semantics.MemoryClasses().ErrorViolatingAssociatedReference();
                //new Semantics.MethodDefinitions().Basics();
                // new Semantics.Mutability().MutabilityIgnoredOnValueCopy();
                //new Semantics.NameResolution().ResolvingForDuplicatedType();
                //new Semantics.ObjectInitialization().ErrorCustomGetterWithInitialization();
                //  new Semantics.OverloadCalls().PreferringNonVariadicFunction();
                //new Semantics.Properties().ErrorSettingCustomGetter();
                // new Semantics.Templates().ErrorSwapNonReassignableValues();
                //new Semantics.TypeMatchingTest().ErrorMixingSlicingTypes();
                //new Semantics.Types().ErrorInOutVariance();
                //new Semantics.Variables().ErrorInvalidVariable();

                // new Execution.Closures().ClosureRecursiveCall();
                //new Execution.Collections().AccessingTuple();
                //new Execution.Concurrency().SingleMessage();
                //new Execution.Extensions().InstanceCallStaticDispatch();
                 //new Execution.Flow().ShortcutComputationInOptionalDeclaration();
                //new Execution.FunctionCalls().MinLimitVariadicFunctionWithSpread();
                //new Execution.Inheritance().InheritingEnums();
                //new Execution.Interfaces().DuckDeepVirtualCallInterface();
                //new Execution.Io().CommandLine();
                //new Execution.Library().StringToInt();
                //new Execution.Mutability().NoMutability();
                //new Execution.NameResolution().GenericTypeAliasing();
                //new Execution.ObjectInitialization().InitializingWithCustomSetter();
                //new Execution.Objects().CallingImplicitConstMethodOnHeapOnlyPointer();
                //new Execution.Pointers().RefCountsOnIgnoringFunctionCall();
                //new Execution.Properties().AutoPropertiesWithPointers();
                //new Execution.Templates().SwapPointers();
                //new Execution.Text().StringUtf8Encoding();
            }

            // if (false)
            {
                const double golden_avg_s = 2.04;
                const double golden_min_s = 0.00;
                const double golden_max_s = 2.56;

                long min_ticks, max_ticks;
                long start = Stopwatch.GetTimestamp();
                int count = runTests<IErrorReporter>(nameof(Semantics), out min_ticks, out max_ticks, checkErrorCoverage: true);

                reportTime("Semantics", start, count, golden_avg_s, min_ticks, golden_min_s, max_ticks, golden_max_s);
            }

            Console.WriteLine();

            //if (false)
            {
                const double golden_avg_s = 2.06;
                const double golden_min_s = 1.17;
                const double golden_max_s = 2.17;

                long min_ticks, max_ticks;
                long start = Stopwatch.GetTimestamp();
                int count = runTests<IInterpreter>(nameof(Execution), out min_ticks, out max_ticks, checkErrorCoverage: false);

                reportTime("Interpretation", start, count, golden_avg_s, min_ticks, golden_min_s, max_ticks, golden_max_s);
            }

            if (AssertReporter.Fails.Any())
            {
                Console.WriteLine("Fails");
                foreach (string s in AssertReporter.Fails.OrderBy(it => it))
                    Console.WriteLine(s);
            }

            if (testOffset != 0)
            {
                var fc = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine();
                Console.WriteLine("NOT ALL TESTS WERE CHECKED!");
                Console.ForegroundColor = fc;
            }

            Console.Write("\a"); // beep

            if (System.Diagnostics.Debugger.IsAttached)
                Console.ReadLine();
        }

        private static void reportTime(string title, long start, int count, double goldenAvg, long minTicks, double goldenMin,
            long maxTicks, double goldenMax)
        {
            double time_s = (Stopwatch.GetTimestamp() - start) * 1.0 / Stopwatch.Frequency;
            double avg_s = time_s / count;
            double min_s = minTicks * 1.0 / Stopwatch.Frequency;
            double max_s = maxTicks * 1.0 / Stopwatch.Frequency;

            Console.Write($"{title} time: {time_s.ToString("0.00")}s");
            Console.Write(", ");
            //reportTime("min", min_s, goldenMin, useColoring: false);
            //Console.Write(", ");
            //reportTime("max", max_s, goldenMax, useColoring: false);
            //Console.Write(", ");
            reportTime("avg", avg_s, goldenAvg, useColoring: true);
            Console.WriteLine();
        }
        private static void reportTime(string part, double currentValue, double goldenValue, bool useColoring)
        {
            Console.Write($"{part}: {currentValue.ToString("0.00")}");
            const int rounding = 100;
            int diff = (int)Math.Round((currentValue - goldenValue) * rounding);

            var fc = Console.ForegroundColor;
            if (useColoring)
            {
                if (diff > 2)
                    Console.ForegroundColor = ConsoleColor.Red;
                else if (diff < -2)
                    Console.ForegroundColor = ConsoleColor.Cyan;
            }
            Console.Write($"/{(diff * 1.0 / rounding).ToString("+0.00;-0.00;0")}");
            Console.ForegroundColor = fc;
        }

        private static int runTests<T>(string @namespace, out long minTicks, out long maxTicks, bool checkErrorCoverage)
            where T : class
        {
            minTicks = long.MaxValue;
            maxTicks = long.MinValue;

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
                missed_atrr.AddRange(runTests<T>(type, ref total, ref failed, ref minTicks, ref maxTicks, reported_errors));
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

            return total;
        }

        private static IEnumerable<string> runTests<T>(Type type, ref int total, ref int failed, ref long minTicks, ref long maxTicks,
            HashSet<ErrorCode> errors)
            where T : class
        {
            var miss_attr = new List<string>();

            int init_fails = failed;

            object test = Activator.CreateInstance(type);
            foreach (System.Reflection.MethodInfo method in test.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .OrderBy(it => it.Name))
            {
                ++total;
                if (testIndex >= testOffset)
                {
                    try
                    {
                        if (!method.GetCustomAttributes(typeof(TestMethodAttribute), false).Any())
                            miss_attr.Add(type.Name + "." + method.Name);
                        else
                        {
                            string test_name = $"{type.Name}.{method.Name} ({testIndex})";
                            Console.Write(test_name);
                            // this is not just dumb casting -- it checks if the given test returns the expected object
                            // so for example, semantic test is not mixed with interpretation test
                            long start_ticks = Stopwatch.GetTimestamp();
                            T result = method.Invoke(test, new object[] { }).Cast<T>();
                            long ticks = Stopwatch.GetTimestamp() - start_ticks;
                            minTicks = Math.Min(minTicks, ticks);
                            maxTicks = Math.Max(maxTicks, ticks);
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

                ++testIndex;
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
