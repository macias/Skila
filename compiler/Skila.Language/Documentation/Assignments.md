## Assignments

### Tracking

We have 3 kind of flows:

**certain**

    something;
    anything;

**maybe**

    if ... then
        return;
    end
    something; // maybe

**unreachable**

    return;
    something; // unreachable

Depending how the flow goes the variables could be assigned for sure, or they "maybe" be assigned. Maybe state is temporary
-- for local purposes such assignment is treated as certain, but once we go to outer scope such assignment will be removed.

Consider such code

    let x;
    loop
        if ... then
	        break;
	    end

	    x = 5; // "maybe" assignment because inside "maybe" flow
	    let y = x; // in this scope this is good
    end-loop // here "maybe" assignments are removed

    let z = x; // error, "x" is not properly assigned

There are some technicalities -- `break` jumps outside the loop, while `continue` inside it. With `if` there could be `else` so we have
to merge the outcomes of two branches.

### Compound assignments (designing)

In C++

    a += b

does not really mean:

    a = a + b

The first one is mutating operation, the second is non-mutating 
addition with assigment. We would like to have cookie and eat it as well, 
thus maybe we could have compound assigment as the shortcut for full form:

    a += b
    a = a + b

And for mutation we would have completely different syntax, like:

    a ! + b

?