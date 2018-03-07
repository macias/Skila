## References

### lvalue, rvalue, references:

* [Understanding lvalues and rvalues in C and C++](https://eli.thegreenplace.net/2011/12/15/understanding-lvalues-and-rvalues-in-c-and-c)
* [A Brief Introduction to Rvalue References
](http://www.artima.com/cppsource/rvalue.html)
* [Rvalue References: C++0x Features in VC10, Part 2](https://blogs.msdn.microsoft.com/vcblog/2009/02/03/rvalue-references-c0x-features-in-vc10-part-2/)

### Go interfaces

* [Go Data Structures: Interfaces](https://research.swtch.com/interfaces)
* [Go Interfaces](http://www.airs.com/blog/archives/277)
* [Go Interface Values](http://www.airs.com/blog/archives/281)

### Static vs. instance operator overloading

* [Why are overloaded operators always static in C#?](https://blogs.msdn.microsoft.com/ericlippert/2007/05/14/why-are-overloaded-operators-always-static-in-c/)
* [Why overloaded operators cannot be defined as static members of a class [in C++]?](https://stackoverflow.com/questions/11894124/why-overloaded-operators-cannot-be-defined-as-static-members-of-a-class)

### Variance

* [How can compiler compute automatically co- and contravariance?](http://stackoverflow.com/questions/32234737/how-can-compiler-compute-automatically-co-and-contravariance)
* [Exact rules for variance validity](http://blogs.msdn.com/b/ericlippert/archive/2009/12/03/exact-rules-for-variance-validity.aspx)
* [Covariance and Contravariance in C#: Why Do We Need A Syntax At All?](http://blogs.msdn.com/b/ericlippert/archive/2007/10/29/covariance-and-contravariance-in-c-part-seven-why-do-we-need-a-syntax-at-all.aspx)

## References/pointers dereference assignment

Or in other words injecting the value via references/pointers. This is legal in C:

    (*ptr) = 1;

But it is not welcome in Skila, however for now we don't see a clear way to solve it.

First problem that when allowing this, signature of functions would be too heavy (because the
fact assignment is done should be signaled to outside world, like `ref` in C#).

So it would be great to forbid it, but assuming atomic primitives (`Int`, `Bool`) are immutable
there would be no way to mutate them on purpose via reference/pointer.
