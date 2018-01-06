## Properties

In Skila properties are more than in C#. First of all 
they create scope, so it is possible to put internal
fields inside given property.

Secondly getters can override regular methods, in case of
indexer getter it override method `at`.

And because of that one has to be specific what is overriden.

    interface IFoo
    { 
        bool what { get; }
    }

    class Foo : IFoo
    {
        bool what { override get {...} set {...} }
    }

Writing this as:

        override bool what { get {...} set {...} }

would mean we override both getter and setter which is not 
true, because in such code setter is novelty.
