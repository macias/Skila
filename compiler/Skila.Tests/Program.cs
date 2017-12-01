﻿using System;
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
            //new Semantics.CompilerProtection().CircularConversion();
            //new Semantics.Templates().ErrorHasConstraint();
            //new Semantics.Inheritance().ErrorLoopedAncestors();
            // new Exceptions().ErrorThrowingNonException();
            //  new Semantics.Properties().ErrorAssigningRValue();
            //new Semantics.Mutability().ErrorAssigningMutableToImmutable();
            //new Semantics.MemoryClasses().ImplicitValueReferenceConversionOnCall();
            //  new Semantics.NameResolution().ErrorAccessForbidden();
              new Semantics.FunctionCalls().ErrorCallingSuperFunctionByFunctioName();
            //  new OverloadCalls().PreferringNonVariadicFunction();
            //  new Variables().TypeInference();
            // new Semantics.Expressions().ErrorIgnoringParameters();
            //new MethodDefinitions().Basics();
            //   new Semantics.FunctionDefinitions().ErrorCannotInferResultType();
            //new Semantics.TypeMatching().UnionMatching();
            //  new Semantics.Flow().ErrorReadingIfWithoutElse();
            //new Semantics.Variables().FunctionAssignment();
            // new Semantics.Types().ErrorConflictingModifier();
            // new Semantics.Concurrency().ErrorSpawningMutables();

            //  new Execution.Properties().AutoProperties();
            //new Execution.Objects().AccessingObjectFields();
            //new Execution.Pointers().DereferenceOnAssignment();
            // new Execution.FunctionCalls().LambdaRecursiveCall();
            //new Execution.Concurrency().SingleMessage();
            //new Execution.Inheritance().VirtualCallAtBase();
            //new Execution.Interfaces().DuckVirtualCallWithGenericBase();
            //new Execution.Templates().HasConstraintWithValue();
            //  new Execution.Closures().ResultTypeInference();

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
            Console.ReadLine();
        }

        private static void runTest<T>(string @namespace, bool checkErrorCoverage)
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
