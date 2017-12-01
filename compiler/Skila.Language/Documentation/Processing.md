Currently we have three stages of processing:

* surfing ;-)
* evaluating
* validating

#### Surfing

It touches the signatures part of the code -- among other things it computes function derivations.

#### Evaluating

It processes the function bodies, so we can tell that in function `f`, variable `x` is of type `Int` when we see `let x = 5`.

#### Validation

Besides trivial checking it inspects the flow of the code detecting if given variable is not used before it is assigned to.