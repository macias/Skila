
using Skila.Language;
using Skila.Language.Entities;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Interpreter
{
    internal struct ExecutionContext : ICallContext
    {
        public static ExecutionContext Create(Environment env, Interpreter interpreter)
        {
            var ctx = new ExecutionContext(env, interpreter);
            ctx.TypeRegistry.RegisterAddAsync(ctx, env.UnitType.InstanceOf).Wait();
            return ctx;
        }

        public Interpreter Interpreter { get; }

        public Environment Env { get; }
        public VariableRegistry LocalVariables;
        public ObjectData ThisArgument { get; set; }
        public ObjectData[] FunctionArguments { get; set; }
        public IReadOnlyList<IEntityInstance> TemplateArguments { get; set; }

        public TemplateTranslation Translation { get; set; }

        internal Heap Heap { get; }
        public RoutineRegistry Routines { get; }
        internal TypeRegistry TypeRegistry { get; }

        private ExecutionContext(Environment env, Interpreter interpreter) : this()
        {
            this.Env = env;
            this.Heap = new Heap();
            this.Routines = new RoutineRegistry();
            this.TypeRegistry = new TypeRegistry();

            this.Interpreter = interpreter;
        }
        private ExecutionContext(ExecutionContext src) : this()
        {
            this.Env = src.Env;
            this.Heap = src.Heap;
            this.Routines = src.Routines;
            this.ThisArgument = src.ThisArgument;
            this.FunctionArguments = src.FunctionArguments;
            this.TypeRegistry = src.TypeRegistry;
            this.Interpreter = src.Interpreter;
        }

        internal ExecutionContext Clone()
        {
            return new ExecutionContext(this);
        }

        internal ObjectData GetArgument(FunctionDefinition func, string paramName)
        {
            FunctionParameter param = func.Parameters.SingleOrDefault(it => it.Name.Name == paramName);
            if (param == null)
                throw new System.Exception($"Internal error {ExceptionCode.SourceInfo()}");
            return this.FunctionArguments[param.Index];
        }

        internal ComputationContext CreateBareComputation()
        {
            return ComputationContext.CreateBare(this.Env);
        }
    }
}
