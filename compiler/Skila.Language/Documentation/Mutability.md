## Mutability

### Terms

Immutable data -- data we can guarantee are immutable (in deep sense).

Not immutable data -- data we cannot guarantee are immutable, so in fact
they can be immutable.
 
### Sharing

Think of two threads. Sharing mutable data in writer+writer 
scenario is out of the question. Writer+reader model is technically
reasonable but it leads to nasty surprises on the reader side:

    if x>0 then
      println(x); // --> -5

True, it can also happen in single thread, but then it is at least
reproducible each time while in concurrent environment it depends
on timing.

So we have 3 options:

   * share immutable data
   * share mutable data by fully replicating it (in future)
   * move data instead of sharing it (in future)

### Tracking mutability

We decided to have immutable data by default and mark explicitly
mutable ones (except for type constraints). You can pass mutable data only when mutable data
is expected. This prevents user from aliasing mutability:

    var p *Object; // in theory pointer to immutable data
    p = my_mutable; // aliasing mutability (incorrect)
    share p; // sharing mutable data in fact

One needs to mark immutable type with mutable modifier in order
to accept mutable data:

    var mut p *Object; // pointer to mutable data
    p = my_mutable; // OK

Of course we cannot share `p` now but it was exactly the point.

Marking all mutable data seems like a lot of work just in order to 
**maybe** share something one day, but the reverse pattern is used
in C++ and it does not work well -- the moment you realize you need
`const` somewhere it might be the same moment you also note you have 
to rewrite half of your program just to add `const`.

#### Type constraints

Consider:

    type Point<T>

Intuitively it means `Point` accepts any type. If we assume generic
type parameters are also immutable by default, we would write:

    type Point<T>
       where T : any

for any type. But constraints are about narrowing possibilities,
and such approach does not play coherently with other constraints.

Thus:

    type Point<T>

means any type for `T` and:

    type Point<T>
       where T : const

narrows it down to immutable types only.

### Comments

Maybe it would be better to have 3 kinds of data -- immutable, mutable (truly)
and unknown (mutable or not). `Object` would be for example unknown, because
by itself it is not mutable but since it is not sealed we cannot guarantee
what happens next. This could lead to more intuitive code:

    def foo(p *Object)

The above code would mean that `foo` takes any descendant type of 
`Object` (currently it means it takes only immutable descendant).