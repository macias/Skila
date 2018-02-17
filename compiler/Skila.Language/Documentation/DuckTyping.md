## Duck typing

Duck typing is implemented like in Go -- when some protocol `X` is required
anything can be passed as long it has the same methods and properties
(there is also an option to turn all interfaces into protocols). Support
for duck typing is disabled by default and it is considered obsolete 
mainly because of tag interfaces.

Consider:

      interface ILinearIndexable : IIndexable
      {
      }

      interface IConstIndexable : IIndexable
      {
      }

With duck typing all 3 interfaces would be interchangeable ignoring the
fact they have different meaning. It is not only that your type `A`
implements some interface, it is also important that your type `A` **is**
this or that.

In language like C# duck typing looks useful on one occassion only --
when bringing addition meaning to already existing types. Real case,
RX library has `IObservable` (stream of data), 
`Subject` : `IObservable` (factory without initial value), 
`BehaviorSubject` : `IObservable` (factory with initial value)
types, and we need to add to them new interfaces -- `ICurrentObservable`
(bringing current and future values on subscription) and `IFutureObservable`
(bringing future values only on subscription). Again those are tag interfaces
but here it is not the point. C# has no means for easy adding those new 
interfaces as overlays to already existing ones.

So what we need is not duck typing which affects entire type system
but the notion of shell type which could be used on **particular** existing
type transforming it to **particular** existing interface.


