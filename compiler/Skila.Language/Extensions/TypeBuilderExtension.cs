using Skila.Language.Builders;
using Skila.Language.Entities;
using Skila.Language.Flow;
using Skila.Language.Expressions.Literals;

namespace Skila.Language.Expressions
{
    public static class TypeBuilderExtension
    {
        public static TypeBuilder WithEquatableEquals(this TypeBuilder builder, EntityModifier modifier = null)
        {
            return builder.With(FunctionBuilder.Create(NameFactory.EqualOperator,
                                            ExpressionReadMode.ReadRequired, NameFactory.BoolNameReference(),
                                            Block.CreateStatement(
                          IfBranch.CreateIf(IsSame.Create(NameReference.CreateThised(), NameReference.Create("cmp")),
                                new[] { Return.Create(BoolLiteral.CreateTrue()) }),
                          // let obj = cmp cast? Self
                          VariableDeclaration.CreateStatement("obj", null, ExpressionFactory.CheckedSelfCast("cmp",
                            NameFactory.ReferenceNameReference(builder.CreateTypeNameReference(TypeMutability.ReadOnly)))),
                        // return this==obj.value
                        Return.Create(ExpressionFactory.IsEqual(NameReference.Create(NameFactory.ThisVariableName),
                            NameReference.Create("obj")))))
                                            .SetModifier(EntityModifier.Override | modifier)
                                            .Parameters(FunctionParameter.Create("cmp",
                                                NameFactory.ReferenceNameReference(NameFactory.IEquatableNameReference(TypeMutability.ReadOnly)))));
        }
        public static TypeBuilder WithComparableCompare(this TypeBuilder builder, EntityModifier modifier = null)
        {
            return builder.With(FunctionBuilder.Create(NameFactory.ComparableCompare,
                                            ExpressionReadMode.ReadRequired, NameFactory.OrderingNameReference(),
                                            Block.CreateStatement(
                          IfBranch.CreateIf(IsSame.Create(NameReference.CreateThised(), NameReference.Create("cmp")),
                                new[] { Return.Create(NameFactory.OrderingEqualReference()) }),
                            // let obj = cmp cast? Self
                            VariableDeclaration.CreateStatement("obj", null, ExpressionFactory.CheckedSelfCast("cmp",
                                NameFactory.ReferenceNameReference(builder.CreateTypeNameReference(TypeMutability.ReadOnly)))),
                        // return this.compare(obj.value)
                        Return.Create(FunctionCall.Create(NameReference.CreateThised(NameFactory.ComparableCompare),
                            NameReference.Create("obj")))))
                                            .SetModifier(EntityModifier.Override | modifier)
                                            .Parameters(FunctionParameter.Create("cmp",
                                                NameFactory.ReferenceNameReference(NameFactory.IComparableNameReference(TypeMutability.ReadOnly)))));
        }
    }
}