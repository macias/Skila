using Skila.Language;
using System.Collections.Generic;

namespace Skila.Interpreter
{
    internal struct ExecutionContext
    {
        public static ExecutionContext Create(Environment env)
        {
            return new ExecutionContext(env);
        }

        public Environment Env { get; }
        public VariableRegistry LocalVariables;
        public ObjectData ThisArgument { get; set; }
        public ObjectData[] FunctionArguments { get; set; }
        public IReadOnlyList<IEntityInstance> TemplateArguments { get; set; }
        internal Heap Heap { get; }
        public RoutineRegistry Routines { get; }

        private ExecutionContext(Environment env) :this ()
        {
            this.Env = env;
            this.Heap = new Heap();
            this.Routines = new RoutineRegistry();
        }
        private ExecutionContext(ExecutionContext src) : this()
        {
            this.Env = src.Env;
            this.Heap = src.Heap;
            this.Routines = src.Routines;
            this.ThisArgument = src.ThisArgument;
            this.FunctionArguments = src.FunctionArguments;
        }

        internal ExecutionContext Clone()
        {
            return new ExecutionContext(this);
        }
    }
}
