using Skila.Language.Entities;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Expressions.Literals;
using System.Collections.Generic;
using System;
using Skila.Language.Expressions;

namespace Skila.Language.Builders
{
    public sealed class ConstructorCall
    {
        private Block build;
        private readonly NameReference varReference;
        private readonly VariableDeclaration varDeclaration;
        private readonly FunctionCall initCall;
        private readonly List<IExpression> objectInitialization;

        public static ConstructorCall HeapConstructor(string innerTypeName, params IExpression[] arguments)
        {
            return HeapConstructor(NameReference.Create(innerTypeName), arguments);
        }
        public static ConstructorCall HeapConstructor(NameReference innerTypeName)
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
        public static ConstructorCall HeapConstructor(NameReference innerTypeName, params IExpression[] arguments)
        {
            return HeapConstructor(innerTypeName, arguments.Select(it => FunctionArgument.Create(it)).ToArray());
        }
        public static ConstructorCall HeapConstructor(NameReference innerTypeName, params FunctionArgument[] arguments)
        {
#if USE_NEW_CONS
            return FunctionCall.Create(NameReference.Create(innerTypeName, NameFactory.NewConstructorName), arguments);
#else
            NameReference dummy;
            return new ConstructorCall(innerTypeName, out dummy, Memory.Heap, arguments);
#endif
        }

        public static ConstructorCall Constructor(NameReference typeName, Memory memory)
        {
            return Constructor(typeName, memory, new FunctionArgument[] { });
        }
        public static ConstructorCall StackConstructor(NameReference typeName)
        {
            return StackConstructor(typeName, new FunctionArgument[] { });
        }
        public static ConstructorCall StackConstructor(string typeName, params FunctionArgument[] arguments)
        {
            return StackConstructor(NameReference.Create(typeName), arguments);
        }
        public static ConstructorCall StackConstructor(string typeName)
        {
            return StackConstructor(NameReference.Create(typeName));
        }
        public static ConstructorCall StackConstructor(string typeName, params IExpression[] arguments)
        {
            return Constructor(typeName, Memory.Stack, arguments);
        }
        public static ConstructorCall Constructor(string typeName, Memory memory, params IExpression[] arguments)
        {
            return Constructor(NameReference.Create(typeName), memory, arguments);
        }
        public static ConstructorCall Constructor(NameReference typeName, Memory memory, params IExpression[] arguments)
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
        public static ConstructorCall StackConstructor(NameReference typeName, params IExpression[] arguments)
        {
            return StackConstructor(typeName, arguments.Select(it => FunctionArgument.Create(it)).ToArray());
        }
        public static ConstructorCall Constructor(NameReference typeName, Memory memory, params FunctionArgument[] arguments)
        {
            NameReference dummy;
            return new ConstructorCall(typeName, out dummy, memory, arguments);
        }
        public static ConstructorCall StackConstructor(NameReference typeName, params FunctionArgument[] arguments)
        {
            NameReference dummy;
            return new ConstructorCall(typeName, out dummy, Memory.Stack, arguments);
        }
        public static ConstructorCall StackConstructor(NameReference typeName, out NameReference constructorReference,
            params FunctionArgument[] arguments)
        {
            return new ConstructorCall(typeName, out constructorReference, Memory.Stack, arguments);
        }
        private ConstructorCall(NameReference typeName,
            // todo: hack, we don't have nice error translation from generic error to more specific one
            out NameReference constructorReference,
            Memory memory,
            params FunctionArgument[] arguments)
        {
            string local_this = AutoName.Instance.CreateNew("cons_obj");
            this.varReference = NameReference.Create(local_this);
            constructorReference = NameReference.Create(varReference, NameFactory.InitConstructorName);

            this.varDeclaration = VariableDeclaration.CreateStatement(local_this, null, Alloc.Create(typeName, memory));
            this.initCall = FunctionCall.Constructor(constructorReference, arguments);

            this.objectInitialization = new List<IExpression>();
        }

        public ConstructorCall Init(string member, IExpression initValue)
        {
            return Init(member, initValue, out Assignment dummy);
        }
        public ConstructorCall Init(string member, IExpression initValue,out Assignment assignment)
        {
            if (this.build != null)
                throw new InvalidOperationException();

            assignment = Assignment.CreateInitialization(NameReference.Create(varReference, member), initValue);
            this.objectInitialization.Add(assignment);
            return this;
        }

        public IExpression Build()
        {
            return buildExpression();
        }

        private Expression buildExpression()
        {
            if (this.build == null)
                this.build = Block.CreateInitialization(
                    // __this__ = alloc()
                    varDeclaration,
                    // __this__.init(args)
                    initCall,
                    // all __this__.member = ...
                    objectInitialization,
                    // --> __this__
                    varReference);

            return this.build;
        }

        // unfortunately C# does not allow conversion to an interface :-|
        public static implicit operator Expression(ConstructorCall @this)
        {
            return @this.buildExpression();
        }
    }
}