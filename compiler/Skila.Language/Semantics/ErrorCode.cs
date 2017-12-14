﻿namespace Skila.Language.Semantics
{
    public enum ErrorCode
    {
        TypeMismatch = 1,
        NoValueExpression = 2,
        NotFunctionType = 3,
        MissingTypeAndValue = 4,
        OverloadingDuplicateFunctionDefinition = 5,
        SelfAssignment = 6,
        TargetFunctionNotFound = 7,
        CannotReadExpression = 8,
        ExpressionValueNotUsed = 9,
        ArgumentForFunctionAlreadyGiven = 10,
        AnonymousTailVariadicParameter = 11,
        InvalidVariadicLimits = 12,
        InvalidNumberVariadicArguments = 13,
        InstanceMemberAccessInStaticContext = 14,
        UnreachableCode = 15,
        MissingReturn = 16,
        MixingSlicingTypes = 17,
        ReferenceNotFound = 18,
        MiddleElseBranch = 19,
        ReturnOutsideFunction = 20,
        PassingVoidValue = 21,
        LoopControlOutsideLoop = 22,
        NameAlreadyExists = 23,
        CircularReference = 24,
        IsTypeOfKnownTypes = 25,
        CyclicTypeHierarchy = 26,
        PersistentReferenceVariable = 27,
        VariableNotInitialized = 28,
        BindableNotUsed = 29,
        AssigningRValue = 30,
        NOTEST_AmbiguousOverloadedCall = 31,
        AddressingRValue = 32,
        HeapTypeOnStack = 33,
        NoDefaultConstructor = 34,
        CannotAutoInitializeCompoundType = 35,
        CannotReassignReadOnlyVariable = 36,
        GlobalReassignableVariable = 37,
        ReassignableFieldInImmutableType = 38,
        MutableFieldInImmutableType = 39,
        GlobalMutableVariable = 40,
        ImmutableInheritsMutable = 41,
        ViolatedConstConstraint = 42,
        ViolatedBaseConstraint = 43,
        ViolatedInheritsConstraint = 44,
        PropertyMultipleAccessors = 45,
        CrossInheritingHeapOnlyType = 46,
        InitializationWithUndef = 47,
        InheritingSealedType = 48,
        TypeImplementationAsSecondaryParent = 49,
        CannotSpawnWithMutableArgument = 50,
        CannotSpawnOnMutableContext = 51,
        MissingDerivedModifier = 52,
        MissingFunctionImplementation = 53,
        NonAbstractTypeWithAbstractMethod = 54,
        SealedTypeWithBaseMethod = 55,
        CannotDeriveSealedMethod = 56,
        ViolatedHasFunctionConstraint = 57,
        ConstraintConflictingTypeHierarchy = 58,
        SelectingAmbiguousTemplateFunction = 59,
        NamedRecursiveReference = 60, // you have to refer to current function as "self", not by its actual name
        ReservedName = 61,
        CannotInferResultType = 62,
        NothingToDerive = 63,
        AccessForbidden = 64,
        AlteredAccessLevel = 65,
        ConflictingModifier = 66,
        DerivationWithoutSuperCall = 67,
        SuperCallWithUnchainedBase = 68,
        TestingAgainstTypeSet = 69,
        VirtualCallFromConstructor = 70,
        ConstructorCallFromFunctionBody = 71,
        CrossReferencingBaseMember = 72,
        MissingThisPrefix = 73,
        DiscardingNonFunctionCall = 74,
        GlobalVariable = 75,
        MissingTypeName = 76,
        EnumCrossInheritance = 77,
        DereferencingValue = 78,
        StaticMemberAccessInInstanceContext = 79,
    }
}
