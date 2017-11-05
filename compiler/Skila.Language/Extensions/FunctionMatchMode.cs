using System;

namespace Skila.Language.Extensions
{
    [Flags]
    public enum FunctionMatchMode
    {
        None = 0,

        CheckOutcome = 1,
        NewOverridesInit = 2,
        // compare `Self` as resolved type (String, Foo, whatever), 
        // it works only when base function has concrete type, and overridine one has Self, not other way around
     //   SubstituteSelfOutcome = 4,
        // I hope this is not introducing bugs -- anyway, this flag is useful when checking type argument against template constraints 
        // like -- does Int with <(Self) meet constraint of T with <(T) 
      //  SubstituteSelfParam = 8,
        // it is possible to provide ancestor class param in derived function
   //     ParamsContravariant = 16,

        // for normal usage
        AllRegular = CheckOutcome,// + SubstituteSelfOutcome,
        // for checking how functions override each other:
        // the only difference is two different functions can replace each other, init and new constructor
        // thus the extra flag
        AllOverriding = AllRegular + NewOverridesInit,
    }
   
}
