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
        ConverterDeclaredWithIgnoredOutput = 21,
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
        HeapTypeAsValue = 33,
        NoDefaultConstructor = 34,
        CannotAutoInitializeCompoundType = 35,
        CannotReassignReadOnlyVariable = 36,
        GlobalMutableVariable = 37,
        MutableFieldInImmutableType = 38,
        AssigningToNonReassignableData = 39,
        PostInitializedCustomGetter = 40,
        InheritanceMutabilityViolation = 41,
        ViolatedMutabilityConstraint = 42,
        ViolatedBaseConstraint = 43,
        ViolatedInheritsConstraint = 44,
        PropertyMultipleAccessors = 45,
        CrossInheritingHeapOnlyType = 46,
        InitializationWithUndef = 47,
        InheritingSealedType = 48,
        TypeImplementationAsSecondaryParent = 49,
        CannotSpawnWithMutableArgument = 50,
        CannotSpawnOnMutableContext = 51,
        MissingOverrideModifier = 52,
        BaseFunctionMissingImplementation = 53,
        NonAbstractTypeWithAbstractMethod = 54,
        SealedTypeWithBaseMethod = 55,
        CannotOverrideSealedMethod = 56,
        ViolatedHasFunctionConstraint = 57,
        ConstraintConflictingTypeHierarchy = 58,
        SelectingAmbiguousTemplateFunction = 59,
        NamedRecursiveFunctionReference = 60, // you have to refer to current function as "self", not by its actual name
        ReservedName = 61,
        CannotInferResultType = 62,
        NothingToOverride = 63,
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
        EmptyReturn = 80,
        NestedValueOfItself = 81,
        ConverterWithParameters = 82,
        ConverterNotPinned = 83,
        VarianceForbiddenPosition = 84,
        MutableFunctionInImmutableType = 85,
        PropertySetterInImmutableType = 86,
        AlteringCurrentInImmutableMethod = 87,
        CallingMutableFromImmutableMethod = 88,
        HeapRequirementChangedOnOverride = 89,
        CallingHeapFunctionWithValue = 90,
        AlteringNonMutableInstance = 91,
        EscapingReference = 92,
        AssociatedReferenceRequiresSealedType = 93,
        AssociatedReferenceRequiresSingleConstructor = 94,
        AssociatedReferenceRequiresSingleParameter = 95,
        AssociatedReferenceRequiresSingleReferenceField = 96,
        ReferenceFieldCannotBeReassignable = 97,
        AssociatedReferenceRequiresReferenceParameter = 98,
        AssociatedReferenceRequiresNonVariadicParameter = 99,
        AssociatedReferenceRequiresNonOptionalParameter = 100,
        AssociatedReferenceRequiresPassingByReference = 101,
        ReferenceAsTypeArgument = 102,
        NonGenericTrait = 103,
        UnconstrainedTrait = 104,
        MissingHostTypeForTrait = 105,
        MisplacedConstraint = 106,
        TraitConstructor = 107,
        TraitInheritingTypeImplementation = 108,
        FieldInNonImplementationType = 109,
        CannotUseValueExpression = 110,
        MainFunctionInvalidResultType = 111,
        DisabledProtocols = 112,
        CannotAssignCustomProperty = 113,
        AmbiguousReference = 114,
        AccessGrantsOnExposedMember = 115,
        NonPrimaryThisParameter = 116,
        OptionalThisParameter = 117,
        VariadicThisParameter = 118,
        NonReferenceThisParameter = 119,
        UndefinedTemplateArguments = 120,
        SelfTypeOutsideConstructor = 121,
    }
}
