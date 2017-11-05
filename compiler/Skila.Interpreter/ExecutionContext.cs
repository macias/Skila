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
        public VariableRegistry LocalVariables { get; set; }
        public ObjectData ThisArgument { get; set; }
        public Dictionary<FunctionParameter,ObjectData> Arguments { get; set; }
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
            this.Arguments = src.Arguments;
        }

        internal ExecutionContext Clone()
        {
            return new ExecutionContext(this);
        }
    }
}
