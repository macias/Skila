using Skila.Language.Builders;
using Skila.Language.Entities;
using Skila.Language.Flow;
using System.Linq;
using System;
using NaiveLanguageTools.Common;
using Skila.Language.Expressions.Literals;
using System.Collections.Generic;

namespace Skila.Language.Expressions
{
    public static class ExpressionFactory
    {
        public static IEnumerable<IExpression> Nop { get; } = Enumerable.Empty<IExpression>();

        public static IExpression OptionalDeclaration(string name, INameReference typeName, IExpression rhs)
        {
            var instructions = new List<IExpression>();

            string temp = AutoName.Instance.CreateNew("opt_temp");
            instructions.Add(VariableDeclaration.CreateStatement(temp, null, rhs));

            // partial declaration (initialization will be done later)
            instructions.Add(VariableDeclaration.CreateStatement(name,
                typeName ?? NameReference.Create(NameReference.Create(temp),
                    BrowseMode.InstanceToStatic, NameFactory.OptionTypeParameterMember),
                null));

            instructions.Add(OptionalAssignment(NameReference.Create(name), NameReference.Create(temp)));

            return Chain.Create(instructions);
        }
        public static IExpression OptionalDeclaration(IEnumerable<VariablePrototype> variables,
            IEnumerable<IExpression> rhsOptions)
        {
            // todo: add support for spread
            if (variables.Count() != rhsOptions.Count())
                throw new NotImplementedException();

            // thanks to `and` parallel optional declaration uses shortcut computation
            // that is, evaluating rhs options stops on the first failure
            IExpression combined = null;
            foreach (Tuple<VariablePrototype,IExpression> pair in variables.SyncZip(rhsOptions))
            {
                VariablePrototype lhs = pair.Item1;
                IExpression rhs = pair.Item2;

                IExpression decl = OptionalDeclaration(lhs.Name, lhs.TypeName, rhs);
                if (combined == null)
                    combined = decl;
                else
                    combined = And(combined, decl);
            }

            return combined;
        }
        public static IExpression OptionalAssignment(IExpression lhs, IExpression rhs)
        {
            return OptionalAssignment(new[] { lhs }, new[] { rhs });
        }
        public static IExpression OptionalAssignment(IEnumerable<IExpression> lhsExpressions, IEnumerable<IExpression> rhsExpressions)
        {
            // todo: add support for spread
            if (lhsExpressions.Count() != rhsExpressions.Count())
                throw new NotImplementedException();

            // please note we could have dummy assignments in form
            // _ ?= x
            // in such case we are not interested in the assigment but the fact it was sucessful or not

            lhsExpressions = lhsExpressions.Select(lhs => lhs is NameReference lhs_name && lhs_name.IsSink ? null : lhs);

            var temp_names = new List<string>();
            IExpression condition = null;
            foreach (Tuple<IExpression, IExpression> pair in rhsExpressions.SyncZip(lhsExpressions))
            {
                IExpression rhs = pair.Item1;
                IExpression lhs = pair.Item2;

                IExpression opt;

                if (lhs == null)
                {
                    temp_names.Add(null);
                    opt = rhs;
                }
                else
                {
                    string temp = AutoName.Instance.CreateNew("optassign");
                    temp_names.Add(temp);
                    opt = VariableDeclaration.CreateExpression(temp, null, rhs);
                }


                IExpression curr = NameReference.Create(opt, BrowseMode.Decompose, NameFactory.OptionHasValue);

                if (condition == null)
                    condition = curr;
                else
                    condition = And(condition, curr);
            }

            var success_body = new List<IExpression>();
            {

                foreach (Tuple<IExpression, string> pair in lhsExpressions.SyncZip(temp_names))
                {
                    IExpression lhs = pair.Item1;
                    string temp = pair.Item2;

                    if (lhs != null)
                        success_body.Add(Assignment.CreateStatement(lhs,
                            NameReference.Create(NameReference.Create(temp), BrowseMode.Decompose, NameFactory.OptionValue)));
                }

                success_body.Add(BoolLiteral.CreateTrue());
            }

            IfBranch result = IfBranch.CreateIf(condition,
                    success_body,
                    IfBranch.CreateElse(BoolLiteral.CreateFalse()));

            return result;
        }

        public static IExpression OptionOf(INameReference typeName, IExpression value, Memory memory = Memory.Stack)
        {
            return Constructor(NameFactory.OptionTypeReference(typeName), memory, value);
        }
        public static IExpression OptionEmpty(string typeName, Memory memory = Memory.Stack)
        {
            return OptionEmpty(NameReference.Create(typeName), memory);
        }
        public static IExpression OptionEmpty(INameReference typeName, Memory memory = Memory.Stack)
        {
            return Constructor(NameFactory.OptionTypeReference(typeName), memory);
        }
        public static FunctionDefinition BasicConstructor(string[] names, INameReference[] typenames)
        {
            return FunctionDefinition.CreateInitConstructor(EntityModifier.None,
                Enumerable.Range(0, names.Length).Select(i => FunctionParameter.Create(names[i], typenames[i])),
                Block.CreateStatement(
                    names.Select(s => Assignment.CreateStatement(NameReference.CreateThised(s), NameReference.Create(s)))));
        }

        public static Block BodyReturnUndef()
        {
            return Block.CreateStatement(Return.Create(Undef.Create()));
        }
        public static TypeBuilder WithEquatableEquals(this TypeBuilder builder, EntityModifier modifier = null)
        {
            return builder.With(FunctionBuilder.Create(NameDefinition.Create(NameFactory.EqualOperator),
                                            ExpressionReadMode.ReadRequired, NameFactory.BoolTypeReference(),
                                            Block.CreateStatement(
                          IfBranch.CreateIf(IsSame.Create(NameReference.CreateThised(), NameReference.Create("cmp")),
                                new[] { Return.Create(BoolLiteral.CreateTrue()) }),
                          // let obj = cmp cast? Self
                          VariableDeclaration.CreateStatement("obj", null, CheckedSelfCast("cmp",
                            NameFactory.ReferenceTypeReference(builder.CreateTypeNameReference(MutabilityOverride.Neutral)))),
                        // return this==obj.value
                        Return.Create(ExpressionFactory.IsEqual(NameReference.Create(NameFactory.ThisVariableName),
                            NameReference.Create("obj")))))
                                            .SetModifier(EntityModifier.Override | modifier)
                                            .Parameters(FunctionParameter.Create("cmp",
                                                NameFactory.ReferenceTypeReference(NameFactory.IEquatableTypeReference(MutabilityOverride.Neutral)))));
        }
        public static IExpression CheckedSelfCast(string paramName, INameReference currentTypeName)
        {
            return Block.CreateExpression(
                // this is basically check for Self type
                // assert this.getType()==other.GetType()
                // please note
                // assert other is CurrentType
                // has different (and incorrect) meaning, because it does not really check both entities type
                // consider this to be IFooChild of IFoo->IFooParent->IFooChild
                // and current type would be IFooParent and the other would be of this type
                AssertTrue(IsSame.Create(FunctionCall.Create(NameReference.CreateThised(NameFactory.GetTypeFunctionName)),
                    FunctionCall.Create(NameReference.Create(paramName, NameFactory.GetTypeFunctionName)))),

                // maybe in future it would be possible to dynamically cast to the actual type of other
                // this would mean static dispatch to later calls
                ExpressionFactory.GetOptionValue(ExpressionFactory.DownCast(NameReference.Create(paramName), currentTypeName))
                );
        }
        public static TypeBuilder WithComparableCompare(this TypeBuilder builder, EntityModifier modifier = null)
        {
            return builder.With(FunctionBuilder.Create(NameDefinition.Create(NameFactory.ComparableCompare),
                                            ExpressionReadMode.ReadRequired, NameFactory.OrderingTypeReference(),
                                            Block.CreateStatement(
                          IfBranch.CreateIf(IsSame.Create(NameReference.CreateThised(), NameReference.Create("cmp")),
                                new[] { Return.Create(NameFactory.OrderingEqualReference()) }),
                            // let obj = cmp cast? Self
                            VariableDeclaration.CreateStatement("obj", null, CheckedSelfCast("cmp",
                                NameFactory.ReferenceTypeReference(builder.CreateTypeNameReference(MutabilityOverride.Neutral)))),
                        // return this.compare(obj.value)
                        Return.Create(FunctionCall.Create(NameReference.CreateThised(NameFactory.ComparableCompare),
                            NameReference.Create("obj")))))
                                            .SetModifier(EntityModifier.Override | modifier)
                                            .Parameters(FunctionParameter.Create("cmp",
                                                NameFactory.ReferenceTypeReference(NameFactory.IComparableTypeReference(MutabilityOverride.Neutral)))));
        }
        public static FunctionCall BaseInit(params FunctionArgument[] arguments)
        {
            return FunctionCall.Constructor(NameReference.CreateBaseInitReference(), arguments);
        }

        public static FunctionCall ThisInit(params FunctionArgument[] arguments)
        {
            return FunctionCall.Constructor(NameReference.Create(NameFactory.ThisVariableName, NameFactory.InitConstructorName),
                arguments);
        }
        public static IExpression DownCast(IExpression lhs, INameReference rhsTypeName)
        {
            // if the expression is not of the given type we get null
            // if it is the runtime type IS PRESERVED
            // say you have statically types object
            // x *Object
            // and in runtime x is Orange
            // if you cast it to Vehicle you will get null, when you cast it to Fruit you will get Orange (sic!)
            IExpression condition = IsType.Create(lhs, rhsTypeName);
            IExpression success = ExpressionFactory.StackConstructor(NameFactory.OptionTypeReference(rhsTypeName),
                FunctionArgument.Create(ReinterpretType.Create(lhs, rhsTypeName)));
            IExpression failure = ExpressionFactory.StackConstructor(NameFactory.OptionTypeReference(rhsTypeName));
            return IfBranch.CreateIf(condition, new[] { success }, IfBranch.CreateElse(new[] { failure }));
        }

        public static IExpression Readout(params string[] name)
        {
            return Readout(NameReference.Create(name));
        }
        public static IExpression Readout(IExpression expr)
        {
            return Assignment.CreateStatement(NameFactory.SinkReference(), expr);
        }

        public static IExpression HeapConstructor(string innerTypeName, params IExpression[] arguments)
        {
            return HeapConstructor(NameReference.Create(innerTypeName), arguments);
        }
        public static IExpression HeapConstructor(NameReference innerTypeName)
        {
            return HeapConstructor(innerTypeName, Enumerable.Empty<FunctionArgument>().ToArray());
            /*
#if USE_NEW_CONS
            return FunctionCall.Create(NameReference.Create(innerTypeName, NameFactory.NewConstructorName));
#else
            NameReference dummy;
            return constructorCall(innerTypeName, out dummy, true);
#endif*/
        }
        public static IExpression HeapConstructor(NameReference innerTypeName, params IExpression[] arguments)
        {
            return HeapConstructor(innerTypeName, arguments.Select(it => FunctionArgument.Create(it)).ToArray());
        }
        public static IExpression HeapConstructor(NameReference innerTypeName, params FunctionArgument[] arguments)
        {
#if USE_NEW_CONS
            return FunctionCall.Create(NameReference.Create(innerTypeName, NameFactory.NewConstructorName), arguments);
#else
            NameReference dummy;
            return constructorCall(innerTypeName, out dummy, Memory.Heap, arguments);
#endif
        }

        public static IExpression Constructor(NameReference typeName, Memory memory)
        {
            return Constructor(typeName, memory, new FunctionArgument[] { });
        }
        public static IExpression StackConstructor(NameReference typeName)
        {
            return StackConstructor(typeName, new FunctionArgument[] { });
        }
        public static IExpression StackConstructor(string typeName, params FunctionArgument[] arguments)
        {
            return StackConstructor(NameReference.Create(typeName), arguments);
        }
        public static IExpression StackConstructor(string typeName)
        {
            return StackConstructor(NameReference.Create(typeName));
        }
        public static IExpression StackConstructor(string typeName, params IExpression[] arguments)
        {
            return Constructor(typeName, Memory.Stack, arguments);
        }
        public static IExpression Constructor(string typeName, Memory memory, params IExpression[] arguments)
        {
            return Constructor(NameReference.Create(typeName), memory, arguments);
        }
        public static IExpression Constructor(NameReference typeName, Memory memory, params IExpression[] arguments)
        {
            return Constructor(typeName, memory, arguments.Select(it => FunctionArgument.Create(it)).ToArray());
        }
        public static IExpression Tuple(params IExpression[] arguments)
        {
            if (arguments.Length == 0)
                throw new System.Exception();
            else if (arguments.Length == 1)
                return arguments.Single();
            else
                return FunctionCall.Create(NameReference.Create(NameFactory.TupleFactoryReference(), NameFactory.CreateFunctionName), arguments);
        }
        public static IExpression InitializeIndexable(string name, params IExpression[] arguments)
        {
            return Block.CreateStatement(arguments.ZipWithIndex().Select(it =>
                Assignment.CreateStatement(FunctionCall.Indexer(NameReference.Create(name),
                FunctionArgument.Create(NatLiteral.Create($"{it.Item2}"))),
                it.Item1)));
        }
        public static IExpression StackConstructor(NameReference typeName, params IExpression[] arguments)
        {
            return StackConstructor(typeName, arguments.Select(it => FunctionArgument.Create(it)).ToArray());
        }
        public static IExpression Constructor(NameReference typeName, Memory memory, params FunctionArgument[] arguments)
        {
            NameReference dummy;
            return constructorCall(typeName, out dummy, memory, arguments);
        }
        public static IExpression StackConstructor(NameReference typeName, params FunctionArgument[] arguments)
        {
            NameReference dummy;
            return constructorCall(typeName, out dummy, Memory.Stack, arguments);
        }
        public static IExpression StackConstructor(NameReference typeName, out NameReference constructorReference,
            params FunctionArgument[] arguments)
        {
            return constructorCall(typeName, out constructorReference, Memory.Stack, arguments);
        }
        private static IExpression constructorCall(NameReference typeName,
            // todo: hack, we don't have nice error translation from generic error to more specific one
            out NameReference constructorReference,
            Memory memory,
            params FunctionArgument[] arguments)
        {
            string local_this = AutoName.Instance.CreateNew("cons_obj");
            var var_ref = NameReference.Create(local_this);
            constructorReference = NameReference.Create(var_ref, NameFactory.InitConstructorName);

            var var_decl = VariableDeclaration.CreateStatement(local_this, null, Alloc.Create(typeName, memory));
            var init_call = FunctionCall.Constructor(constructorReference, arguments);

            return Block.CreateInitialization(
                // __this__ = alloc()
                var_decl,
                // __this__.init(args)
                init_call,
                // --> __this__
                var_ref);
        }

        public static IExpression Add(IExpression lhs, IExpression rhs)
        {
            return FunctionCall.Create(NameReference.Create(lhs, NameFactory.AddOperator), FunctionArgument.Create(rhs));
        }
        public static IExpression AddOverflow(IExpression lhs, IExpression rhs)
        {
            return FunctionCall.Create(NameReference.Create(lhs, NameFactory.AddOverflowOperator), FunctionArgument.Create(rhs));
        }
        public static IExpression Inc(Func<IExpression> lhs)
        {
            return IncBy(lhs, Nat8Literal.Create("1"));
        }
        internal static IExpression Dec(Func<IExpression> lhs)
        {
            return DecBy(lhs, Nat8Literal.Create("1"));
        }
        internal static IExpression IncBy(Func<IExpression> lhs, IExpression byExpr)
        {
            return Assignment.CreateStatement(lhs(), Add(lhs(), byExpr));
        }
        internal static IExpression DecBy(Func<IExpression> lhs, IExpression byExpr)
        {
            return Assignment.CreateStatement(lhs(), Sub(lhs(), byExpr));
        }
        public static IExpression Inc(string name)
        {
            return Inc(() => NameReference.Create(name));
        }
        public static IExpression Dec(string name)
        {
            return Dec(() => NameReference.Create(name));
        }
        public static IExpression IncBy(string name, string addName)
        {
            return IncBy(name, NameReference.Create(addName));
        }
        public static IExpression IncBy(string name, IExpression factor)
        {
            return Assignment.CreateStatement(NameReference.Create(name), Add(NameReference.Create(name), factor));
        }
        public static IExpression Mul(IExpression lhs, IExpression rhs)
        {
            return FunctionCall.Create(NameReference.Create(lhs, NameFactory.MulOperator), FunctionArgument.Create(rhs));
        }
        public static IExpression Divide(string lhs, string rhs)
        {
            return Divide(NameReference.Create(lhs), NameReference.Create(rhs));
        }
        public static IExpression Divide(IExpression lhs, IExpression rhs)
        {
            return FunctionCall.Create(NameReference.Create(lhs, NameFactory.DivideOperator), FunctionArgument.Create(rhs));
        }
        public static IExpression Sub(IExpression lhs, IExpression rhs)
        {
            return FunctionCall.Create(NameReference.Create(lhs, NameFactory.SubOperator), FunctionArgument.Create(rhs));
        }
        public static IExpression Add(string lhs, string rhs)
        {
            return Add(NameReference.Create(lhs), NameReference.Create(rhs));
        }
        public static IExpression Mul(string lhs, string rhs)
        {
            return Mul(NameReference.Create(lhs), NameReference.Create(rhs));
        }
        public static IExpression And(IExpression lhs, IExpression rhs)
        {
            return BoolOperator.Create(BoolOperator.OpMode.And, lhs, rhs);
        }
        public static IExpression Or(IExpression lhs, IExpression rhs)
        {
            return BoolOperator.Create(BoolOperator.OpMode.Or, lhs, rhs);
        }
        public static IExpression IsEqual(IExpression lhs, IExpression rhs)
        {
            return FunctionCall.Create(NameReference.Create(lhs, NameFactory.EqualOperator), FunctionArgument.Create(rhs));
        }
        public static IExpression IsEqual(string lhs, string rhs)
        {
            return IsEqual(NameReference.Create(lhs), NameReference.Create(rhs));
        }
        public static IExpression IsGreaterEqual(IExpression lhs, IExpression rhs)
        {
            return FunctionCall.Create(NameReference.Create(lhs, NameFactory.GreaterEqualOperator), FunctionArgument.Create(rhs));
        }
        public static IExpression IsLess(IExpression lhs, IExpression rhs)
        {
            return FunctionCall.Create(NameReference.Create(lhs, NameFactory.LessOperator), FunctionArgument.Create(rhs));
        }
        public static IExpression IsLessEqual(IExpression lhs, IExpression rhs)
        {
            return FunctionCall.Create(NameReference.Create(lhs, NameFactory.LessEqualOperator), FunctionArgument.Create(rhs));
        }
        public static IExpression IsGreater(IExpression lhs, IExpression rhs)
        {
            return FunctionCall.Create(NameReference.Create(lhs, NameFactory.GreaterOperator), FunctionArgument.Create(rhs));
        }
        public static IExpression IsNotEqual(IExpression lhs, IExpression rhs)
        {
            return FunctionCall.Create(NameReference.Create(lhs, NameFactory.NotEqualOperator), FunctionArgument.Create(rhs));
        }
        public static IExpression NotEqual(string lhs, string rhs)
        {
            return IsNotEqual(NameReference.Create(lhs), NameReference.Create(rhs));
        }
        public static IExpression Not(IExpression expr)
        {
            return FunctionCall.Create(NameReference.Create(expr, NameFactory.NotOperator));
        }
        public static IExpression GenericThrow()
        {
            return Throw.Create(HeapConstructor(NameFactory.ExceptionTypeReference()));
        }

        public static IExpression AssertTrue(IExpression condition)
        {
            return IfBranch.CreateIf(ExpressionFactory.Not(condition), new[] { GenericThrow() });
        }

        public static IExpression AssertEqual(IExpression expected, IExpression actual)
        {
            return AssertTrue(IsEqual(expected, actual));
        }

        public static IExpression AssertOptionIsSome(IExpression option)
        {
            return AssertTrue(OptionIsSome(option));
        }
        public static IExpression AssertOptionIsNull(IExpression option)
        {
            return AssertTrue(OptionIsNull(option));
        }

        public static IExpression Ternary(IExpression condition, IExpression then, IExpression otherwise)
        {
            return IfBranch.CreateIf(condition, then, IfBranch.CreateElse(otherwise));
        }

        public static IExpression OptionCoalesce(IExpression option, IExpression fallback)
        {
            string get_opt = AutoName.Instance.CreateNew("try_opt");

            return Ternary(OptionalDeclaration(get_opt, null, option), NameReference.Create(get_opt), fallback);
        }

        public static IExpression OptionIsSome(IExpression option)
        {
            return OptionalAssignment(NameFactory.SinkReference(), option);
        }

        public static IExpression OptionIsNull(IExpression option)
        {
            return Not(OptionIsSome(option));
        }

        public static IExpression GetOptionValue(IExpression option)
        {
            return OptionCoalesce(option, GenericThrow());
        }

    }
}