// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Roslynator.CSharp.Refactorings
{
    internal static class GenerateEnumMemberRefactoring
    {
        public static async Task ComputeRefactoringAsync(RefactoringContext context, EnumDeclarationSyntax enumDeclaration)
        {
            SemanticModel semanticModel = await context.GetSemanticModelAsync().ConfigureAwait(false);

            INamedTypeSymbol enumSymbol = semanticModel.GetDeclaredSymbol(enumDeclaration, context.CancellationToken);

            if (enumSymbol.IsEnumWithFlags())
            {
                List<ulong> values = GetConstantValues(enumSymbol);

                Optional<ulong> optional = FlagsUtility<ulong>.Instance.GetUniquePowerOfTwo(values);

                if (optional.HasValue)
                {
                    context.RegisterRefactoring(
                        "Generate enum member",
                        ct => RefactorAsync(context.Document, enumDeclaration, enumSymbol, optional.Value, ct),
                        RefactoringIdentifiers.GenerateEnumMember);

                    Optional<ulong> optional2 = FlagsUtility<ulong>.Instance.GetUniquePowerOfTwo(values, startFromHighestExistingValue: true);

                    if (optional2.HasValue
                        && optional.Value != optional2.Value)
                    {
                        context.RegisterRefactoring(
                            $"Generate enum member (with value {optional2.Value})",
                            ct => RefactorAsync(context.Document, enumDeclaration, enumSymbol, optional2.Value, ct),
                            EquivalenceKey.Join(RefactoringIdentifiers.GenerateEnumMember, "StartFromHighestExistingValue"));
                    }
                }
            }
            else
            {
                context.RegisterRefactoring(
                    "Generate enum member",
                    ct => RefactorAsync(context.Document, enumDeclaration, enumSymbol, null, ct),
                    RefactoringIdentifiers.GenerateEnumMember);
            }
        }

        private static List<ulong> GetConstantValues(INamedTypeSymbol enumSymbol)
        {
            var values = new List<ulong>();

            foreach (ISymbol member in enumSymbol.GetMembers())
            {
                if (member.Kind == SymbolKind.Field)
                {
                    var fieldSymbol = (IFieldSymbol)member;

                    if (fieldSymbol.HasConstantValue)
                        values.Add(SymbolUtility.GetEnumValueAsUInt64(fieldSymbol.ConstantValue, enumSymbol));
                }
            }

            return values;
        }

        private static Task<Document> RefactorAsync(
            Document document,
            EnumDeclarationSyntax enumDeclaration,
            INamedTypeSymbol enumSymbol,
            ulong? value,
            CancellationToken cancellationToken)
        {
            EqualsValueClauseSyntax equalsValue = null;

            if (value != null)
                equalsValue = EqualsValueClause(ParseExpression(value.Value.ToString(CultureInfo.InvariantCulture)));

            string name = NameGenerator.Default.EnsureUniqueMemberName(DefaultNames.EnumMember, enumSymbol);

            SyntaxToken identifier = Identifier(name).WithRenameAnnotation();

            EnumMemberDeclarationSyntax newEnumMember = EnumMemberDeclaration(
                default(SyntaxList<AttributeListSyntax>),
                identifier,
                equalsValue);

            EnumDeclarationSyntax newNode = enumDeclaration.AddMembers(newEnumMember);

            return document.ReplaceNodeAsync(enumDeclaration, newNode, cancellationToken);
        }
    }
}