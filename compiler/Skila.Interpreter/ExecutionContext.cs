using Skila.Language;
using System.Collections.Generic;

namespace Skila.Interpreter
{
    internal struct ExecutionContext
    {
        public static ExecutionContext Create(Environment env, Interpreter interpreter)
        {
            return new ExecutionContext(env,interpreter);
        }

        public Interpreter Interpreter { get; } 

        public Environment Env { get; }
        public VariableRegistry LocalVariables;
        public ObjectData ThisArgument { get; set; }
        public ObjectData[] FunctionArguments { get; set; }
        public IReadOnlyList<IEntityInstance> TemplateArguments { get; set; }
        internal Heap Heap { get; }
        public RoutineRegistry Routines { get; }
        internal TypeRegistry TypeRegistry { get; }

        private ExecutionContext(Environment env,Interpreter interpreter) :this ()
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
    }
}
