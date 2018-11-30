// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Roslynator.CSharp.CSharpFactory;

namespace Roslynator.CSharp.Refactorings
{
    internal static class ImplementCustomEnumeratorRefactoring
    {
        public static void ComputeRefactoring(
            RefactoringContext context,
            TypeDeclarationSyntax typeDeclaration,
            SemanticModel semanticModel)
        {
            INamedTypeSymbol symbol = semanticModel.GetDeclaredSymbol(typeDeclaration, context.CancellationToken);

            if (symbol.IsAbstract)
                return;

            INamedTypeSymbol ienumerableOfT = symbol.AllInterfaces.FirstOrDefault(f => f.OriginalDefinition.HasMetadataName(MetadataNames.System_Collections_Generic_IEnumerable_T));

            if (ienumerableOfT == null)
                return;

            INamedTypeSymbol enumerator = symbol.FindTypeMember(
                "Enumerator",
                arity: 0,
                f => f.TypeKind == TypeKind.Struct && f.DeclaredAccessibility == Accessibility.Public,
                includeBaseTypes: true);

            if (enumerator != null)
                return;

            context.RegisterRefactoring(
                "Implement custom enumerator",
                ct => RefactorAsync(context.Document, typeDeclaration, symbol, ienumerableOfT.TypeArguments.Single(), ct),
                RefactoringIdentifiers.ImplementCustomEnumerator);
        }

        private static Task<Document> RefactorAsync(
            Document document,
            TypeDeclarationSyntax typeDeclaration,
            INamedTypeSymbol typeSymbol,
            ITypeSymbol elementSymbol,
            CancellationToken cancellationToken)
        {
            TypeSyntax type = typeSymbol.ToTypeSyntax();
            TypeSyntax elementType = elementSymbol.ToTypeSyntax();

            StructDeclarationSyntax enumeratorDeclaration = StructDeclaration(
                Modifiers.Public(),
                "Enumerator",
                CreateMembers(typeSymbol, type, elementType).ToSyntaxList());

            enumeratorDeclaration = enumeratorDeclaration.WithFormatterAnnotation();

            TypeDeclarationSyntax newTypeDeclaration = MemberDeclarationInserter.Default.Insert(typeDeclaration, enumeratorDeclaration);

            return document.ReplaceNodeAsync(typeDeclaration, newTypeDeclaration, cancellationToken);
        }

        private static IEnumerable<MemberDeclarationSyntax> CreateMembers(INamedTypeSymbol symbol, TypeSyntax type, TypeSyntax elementType)
        {
            string identifier = NameGenerator.CreateName(symbol, firstCharToLower: true) ?? DefaultNames.Variable;

            string identifierWithUnderscore = "_" + identifier;

            yield return FieldDeclaration(Modifiers.Private_ReadOnly(), type, identifierWithUnderscore);

            yield return FieldDeclaration(Modifiers.Private(), CSharpTypeFactory.IntType(), "_index");

            yield return ConstructorDeclaration(
                Modifiers.Internal(),
                Identifier("Enumerator"),
                ParameterList(Parameter(type, identifier)),
                Block(
                    SimpleAssignmentStatement(IdentifierName(identifierWithUnderscore), IdentifierName(identifier)),
                    SimpleAssignmentStatement(IdentifierName("_index"), NumericLiteralExpression(-1))));

            yield return PropertyDeclaration(
                Modifiers.Public(),
                elementType,
                Identifier(WellKnownMemberNames.CurrentPropertyName),
                AccessorList(
                    GetAccessorDeclaration(
                        Block(
                            ThrowStatement(
                                ObjectCreationExpression(ParseTypeName("System.NotImplementedException").WithSimplifierAnnotation()))))));

            yield return MethodDeclaration(
                Modifiers.Public(),
                CSharpTypeFactory.BoolType(),
                Identifier(WellKnownMemberNames.MoveNextMethodName),
                ParameterList(),
                Block(
                    ThrowStatement(
                        ObjectCreationExpression(ParseTypeName("System.NotImplementedException").WithSimplifierAnnotation()))));

            yield return MethodDeclaration(
                Modifiers.Public(),
                VoidType(),
                Identifier("Reset"),
                ParameterList(),
                Block(
                    ThrowStatement(
                        ObjectCreationExpression(ParseTypeName("System.NotImplementedException").WithSimplifierAnnotation()))));

            yield return MethodDeclaration(
                Modifiers.Public_Override(),
                CSharpTypeFactory.BoolType(),
                Identifier(WellKnownMemberNames.ObjectEquals),
                ParameterList(Parameter(CSharpTypeFactory.ObjectType(), "obj")),
                Block(
                    ThrowStatement(
                        ObjectCreationExpression(ParseTypeName("System.NotSupportedException").WithSimplifierAnnotation()))));

            yield return MethodDeclaration(
                Modifiers.Public_Override(),
                CSharpTypeFactory.IntType(),
                Identifier(WellKnownMemberNames.ObjectGetHashCode),
                ParameterList(),
                Block(
                    ThrowStatement(
                        ObjectCreationExpression(ParseTypeName("System.NotSupportedException").WithSimplifierAnnotation()))));

            yield return ClassDeclaration(
                default(SyntaxList<AttributeListSyntax>),
                Modifiers.Private(),
                Identifier("EnumeratorImpl"),
                default(TypeParameterListSyntax),
                BaseList(
                    SimpleBaseType(
                        ParseTypeName($"System.Collections.Generic.IEnumerator<{elementType}>").WithSimplifierAnnotation())),
                default(SyntaxList<TypeParameterConstraintClauseSyntax>),
                CreateImplMembers().ToSyntaxList());

            IEnumerable<MemberDeclarationSyntax> CreateImplMembers()
            {
                yield return FieldDeclaration(Modifiers.Private(), IdentifierName("Enumerator"), "_e");

                yield return ConstructorDeclaration(
                    Modifiers.Internal(),
                    Identifier("EnumeratorImpl"),
                    ParameterList(Parameter(elementType, identifier)),
                    Block(
                        SimpleAssignmentStatement(
                            IdentifierName("_e"),
                            ObjectCreationExpression(
                                IdentifierName("Enumerator"),
                                ArgumentList(Argument(IdentifierName(identifier)))))));

                yield return PropertyDeclaration(
                    Modifiers.Public(),
                    elementType,
                    Identifier(WellKnownMemberNames.CurrentPropertyName),
                    AccessorList(
                        GetAccessorDeclaration(
                            Block(
                                ReturnStatement(SimpleMemberAccessExpression(IdentifierName("_e"), IdentifierName(WellKnownMemberNames.CurrentPropertyName)))))));

                yield return PropertyDeclaration(
                    default(SyntaxList<AttributeListSyntax>),
                    default(SyntaxTokenList),
                    CSharpTypeFactory.ObjectType(),
                    ExplicitInterfaceSpecifier(ParseName("System.Collections.IEnumerator").WithSimplifierAnnotation()),
                    Identifier(WellKnownMemberNames.CurrentPropertyName),
                    AccessorList(
                        GetAccessorDeclaration(
                            Block(
                                ReturnStatement(SimpleMemberAccessExpression(IdentifierName("_e"), IdentifierName(WellKnownMemberNames.CurrentPropertyName)))))));

                yield return MethodDeclaration(
                    Modifiers.Public(),
                    CSharpTypeFactory.BoolType(),
                    Identifier(WellKnownMemberNames.MoveNextMethodName),
                    ParameterList(),
                    Block(
                        ReturnStatement(SimpleMemberInvocationExpression(IdentifierName("_e"), IdentifierName(WellKnownMemberNames.MoveNextMethodName)))));

                yield return MethodDeclaration(
                    default(SyntaxList<AttributeListSyntax>),
                    default(SyntaxTokenList),
                    VoidType(),
                    ExplicitInterfaceSpecifier(ParseName("System.Collections.IEnumerator").WithSimplifierAnnotation()),
                    Identifier("Reset"),
                    default(TypeParameterListSyntax),
                    ParameterList(),
                    default(SyntaxList<TypeParameterConstraintClauseSyntax>),
                    Block(
                        ReturnStatement(SimpleMemberAccessExpression(IdentifierName("_e"), IdentifierName("Reset")))),
                    default(ArrowExpressionClauseSyntax));

                yield return MethodDeclaration(
                    default(SyntaxList<AttributeListSyntax>),
                    default(SyntaxTokenList),
                    VoidType(),
                    ExplicitInterfaceSpecifier(ParseName("System.IDisposable").WithSimplifierAnnotation()),
                    Identifier("Dispose"),
                    default(TypeParameterListSyntax),
                    ParameterList(),
                    default(SyntaxList<TypeParameterConstraintClauseSyntax>),
                    Block(),
                    default(ArrowExpressionClauseSyntax));
            }
        }
    }
}
