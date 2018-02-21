## Memory

The requirement is all resources like files, sockets are released right away
when they are not longer used. Initially it meant that RC GC should be used.

However "rumour" is M&S GC is more efficient:

* [Does reference counting really use less memory than tracing garbage collection? Swift vs OCaml
](http://flyingfrogblog.blogspot.com/2017/12/does-reference-counting-really-use-less.html)
* [Does reference counting really use less memory than tracing garbage collection? Mathematica vs Swift vs OCaml vs F# on .NET and Mono
](http://flyingfrogblog.blogspot.com/2017/12/does-reference-counting-really-use-less_26.html)
* [http://flyingfrogblog.blogspot.com/2018/01/background-reading-on-reference.html](http://flyingfrogblog.blogspot.com/2018/01/background-reading-on-reference.html)

If ever M&S makes its way into Skila then maybe a hybrid approach could work.

1. Introduce interface `IDisposable` (like in C#, with private `Dipose` though using NVI pattern),
2. Require that type holding `IDisposable` is `IDisposable`
3. Any `IDisposable` type when casting cannot shake off `IDisposable`
4. Use GC for `IDisposables` and M&S for the rest (unlike C# `Dispose` is called only by GC)
 