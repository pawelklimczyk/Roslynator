// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Roslynator.CSharp.Refactorings
{
    internal static class AddParameterToInterfaceMemberRefactoring
    {
        public static void ComputeRefactoring(
            RefactoringContext context,
            MethodDeclarationSyntax methodDeclaration,
            SemanticModel semanticModel)
        {
            ComputeRefactoring(
                context,
                methodDeclaration,
                methodDeclaration.Modifiers,
                methodDeclaration.ExplicitInterfaceSpecifier,
                methodDeclaration.Identifier,
                methodDeclaration.ParameterList?.Parameters ?? default,
                semanticModel);
        }

        public static void ComputeRefactoring(
            RefactoringContext context,
            IndexerDeclarationSyntax indexerDeclaration,
            SemanticModel semanticModel)
        {
            ComputeRefactoring(
                context,
                indexerDeclaration,
                indexerDeclaration.Modifiers,
                indexerDeclaration.ExplicitInterfaceSpecifier,
                indexerDeclaration.ThisKeyword,
                indexerDeclaration.ParameterList?.Parameters ?? default,
                semanticModel);
        }

        private static void ComputeRefactoring(
            RefactoringContext context,
            MemberDeclarationSyntax memberDeclaration,
            SyntaxTokenList modifiers,
            ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier,
            SyntaxToken identifier,
            SeparatedSyntaxList<ParameterSyntax> parameters,
            SemanticModel semanticModel)
        {
            if (!parameters.Any())
                return;

            if (modifiers.Contains(SyntaxKind.StaticKeyword))
                return;

            if (explicitInterfaceSpecifier != null)
            {
                ComputeRefactoringForExplicitImplementation(
                    context,
                    memberDeclaration,
                    explicitInterfaceSpecifier,
                    identifier,
                    parameters,
                    semanticModel);
            }
            else
            {
                ComputeRefactoringForImplicitImplementation(
                    context,
                    memberDeclaration,
                    modifiers,
                    parameters,
                    semanticModel);
            }
        }

        private static void ComputeRefactoringForExplicitImplementation(
            RefactoringContext context,
            MemberDeclarationSyntax memberDeclaration,
            ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier,
            SyntaxToken identifier,
            SeparatedSyntaxList<ParameterSyntax> parameters,
            SemanticModel semanticModel)
                        {
            NameSyntax explicitInterfaceName = explicitInterfaceSpecifier?.Name;

            if (explicitInterfaceName == null)
                return;

            var interfaceSymbol = (INamedTypeSymbol)semanticModel.GetTypeSymbol(explicitInterfaceName, context.CancellationToken);

            if (interfaceSymbol?.TypeKind != TypeKind.Interface)
                return;

            if (!(interfaceSymbol.GetSyntaxOrDefault(context.CancellationToken) is InterfaceDeclarationSyntax interfaceDeclaration))
                return;

            ISymbol memberSymbol = semanticModel.GetDeclaredSymbol(memberDeclaration, context.CancellationToken);

            if (memberSymbol == null)
                return;

            Diagnostic diagnostic = semanticModel.GetDiagnostic("CS0539", identifier.Span, context.CancellationToken);

            if (diagnostic?.Location.SourceSpan != identifier.Span)
                return;

            ISymbol interfaceMemberSymbol = FindInterfaceMember(memberSymbol, interfaceSymbol);

            if (interfaceMemberSymbol == null)
                return;

            RegisterRefactoring(context, memberDeclaration, parameters.Last(), interfaceMemberSymbol, semanticModel);
        }

        private static void ComputeRefactoringForImplicitImplementation(
            RefactoringContext context,
            MemberDeclarationSyntax memberDeclaration,
            SyntaxTokenList modifiers,
            SeparatedSyntaxList<ParameterSyntax> parameters,
            SemanticModel semanticModel)
        {
            if (!modifiers.Contains(SyntaxKind.PublicKeyword))
                return;

            BaseListSyntax baseList = GetBaseList(memberDeclaration.Parent);

            if (baseList == null)
                return;

            SeparatedSyntaxList<BaseTypeSyntax> baseTypes = baseList.Types;

            ISymbol memberSymbol = semanticModel.GetDeclaredSymbol(memberDeclaration, context.CancellationToken);

            if (memberSymbol == null)
                return;

            if (memberSymbol.ImplementsInterfaceMember())
                return;

            int count = 0;

            for (int i = 0; i < baseTypes.Count; i++)
            {
                BaseTypeSyntax baseType = baseTypes[i];

                Diagnostic diagnostic = semanticModel.GetDiagnostic("CS0535", baseType.Type.Span, context.CancellationToken);

                if (diagnostic?.Location.SourceSpan != baseType.Type.Span)
                    continue;

                var interfaceSymbol = semanticModel.GetTypeSymbol(baseType.Type, context.CancellationToken) as INamedTypeSymbol;

                if (interfaceSymbol?.TypeKind != TypeKind.Interface)
                    continue;

                if (!(interfaceSymbol.GetSyntaxOrDefault(context.CancellationToken) is InterfaceDeclarationSyntax interfaceDeclaration))
                    continue;

                ISymbol interfaceMemberSymbol = FindInterfaceMember(memberSymbol, interfaceSymbol);

                if (interfaceMemberSymbol != null)
                {
                    RegisterRefactoring(context, memberDeclaration, parameters.Last(), interfaceMemberSymbol, semanticModel);

                    count++;

                    if (count == 10)
                        break;
                }
            }
        }

        private static ISymbol FindInterfaceMember(
            ISymbol memberSymbol,
            INamedTypeSymbol interfaceSymbol)
        {
            switch (memberSymbol.Kind)
            {
                case SymbolKind.Method:
                    {
                        var methodSymbol = (IMethodSymbol)memberSymbol;

                        return FindInterfaceMethod(methodSymbol, interfaceSymbol);
                    }
                case SymbolKind.Property:
                    {
                        var propertySymbol = (IPropertySymbol)memberSymbol;

                        if (propertySymbol.IsIndexer)
                            return FindInterfaceIndexer(propertySymbol, interfaceSymbol);

                        break;
                    }
            }

            return null;
        }

        private static ISymbol FindInterfaceMethod(
            IMethodSymbol methodSymbol,
            INamedTypeSymbol interfaceSymbol)
        {
            ImmutableArray<ISymbol> members = interfaceSymbol.GetMembers();

            for (int i = 0; i < members.Length; i++)
                    {
                ISymbol memberSymbol = members[i];

                if (memberSymbol.Kind != SymbolKind.Method)
                    continue;

                var methodSymbol2 = (IMethodSymbol)memberSymbol;

                if (methodSymbol2.MethodKind != MethodKind.Ordinary)
                    continue;

                if (methodSymbol.MethodKind == MethodKind.ExplicitInterfaceImplementation)
                {
                    int dotIndex = methodSymbol.Name.LastIndexOf('.');

                    if (string.Compare(methodSymbol.Name, dotIndex + 1, methodSymbol2.Name, 0, methodSymbol2.Name.Length) != 0)
                        continue;
                }
                else if (methodSymbol.Name != methodSymbol2.Name)
                {
                    continue;
                }

                if (methodSymbol.TypeParameters.Length != methodSymbol2.TypeParameters.Length)
                    continue;

                ImmutableArray<IParameterSymbol> parameters = methodSymbol.Parameters;
                ImmutableArray<IParameterSymbol> parameters2 = methodSymbol2.Parameters;

                if (parameters.Length != parameters2.Length + 1)
                    continue;

                if (!methodSymbol.ReturnType.Equals(methodSymbol2.ReturnType))
                    continue;

                if (!ParametersEqual(parameters, parameters2))
                    continue;

                return memberSymbol;
            }

            return null;
        }

        private static ISymbol FindInterfaceIndexer(
            IPropertySymbol propertySymbol,
            INamedTypeSymbol interfaceSymbol)
        {
            ImmutableArray<ISymbol> members = interfaceSymbol.GetMembers();

            for (int i = 0; i < members.Length; i++)
                    {
                ISymbol memberSymbol = members[i];

                if (memberSymbol.Kind != SymbolKind.Property)
                    continue;

                var propertySymbol2 = (IPropertySymbol)memberSymbol;

                if (!propertySymbol2.IsIndexer)
                    continue;

                ImmutableArray<IParameterSymbol> parameters = propertySymbol.Parameters;
                ImmutableArray<IParameterSymbol> parameters2 = propertySymbol2.Parameters;

                if (parameters.Length != parameters2.Length + 1)
                    continue;

                if (!propertySymbol.Type.Equals(propertySymbol2.Type))
                    continue;

                if (!ParametersEqual(parameters, parameters2))
                    continue;

                return memberSymbol;
            }

            return null;
        }

        private static bool ParametersEqual(ImmutableArray<IParameterSymbol> parameters, ImmutableArray<IParameterSymbol> parameters2)
        {
            for (int j = 0; j < parameters.Length - 1; j++)
            {
                if (parameters[j].RefKind != parameters2[j].RefKind)
                    return false;

                if (!parameters[j].Type.Equals(parameters2[j].Type))
                    return false;
            }

            return true;
        }

        private static void RegisterRefactoring(
            RefactoringContext context,
            MemberDeclarationSyntax memberDeclaration,
            ParameterSyntax parameter,
            ISymbol interfaceMemberSymbol,
            SemanticModel semanticModel)
        {
            Document document = context.Document;

            string displayName = SymbolDisplay.ToMinimalDisplayString(interfaceMemberSymbol, semanticModel, memberDeclaration.SpanStart, SymbolDisplayFormat.CSharpShortErrorMessageFormat);

            string title = $"Add parameter '{parameter.Identifier.ValueText}' to '{displayName}'";

            string equivalenceKey = EquivalenceKey.Join(RefactoringIdentifiers.AddParameterToInterfaceMember, displayName);

            var interfaceMemberDeclaration = (MemberDeclarationSyntax)interfaceMemberSymbol.GetSyntax();

            if (memberDeclaration.SyntaxTree == interfaceMemberDeclaration.SyntaxTree)
            {
                context.RegisterRefactoring(
                    title,
                    ct =>
                    {
                        MemberDeclarationSyntax newNode = AddParameter(interfaceMemberDeclaration, parameter);

                        return document.ReplaceNodeAsync(interfaceMemberDeclaration, newNode, ct);
                    },
                    equivalenceKey);
            }
            else
            {
                context.RegisterRefactoring(
                    title,
                    ct =>
                    {
                        MemberDeclarationSyntax newNode = AddParameter(interfaceMemberDeclaration, parameter);

                        return document.Solution().ReplaceNodeAsync(interfaceMemberDeclaration, newNode, ct);
                    },
                    equivalenceKey);
            }
        }

        private static MemberDeclarationSyntax AddParameter(MemberDeclarationSyntax memberDeclaration, ParameterSyntax parameter)
        {
            switch (memberDeclaration.Kind())
            {
                case SyntaxKind.MethodDeclaration:
                    {
                        var methodDeclaration = (MethodDeclarationSyntax)memberDeclaration;
                        return methodDeclaration.AddParameterListParameters(parameter);
                    }
                case SyntaxKind.IndexerDeclaration:
                    {
                        var indexerDeclaration = (IndexerDeclarationSyntax)memberDeclaration;
                        return indexerDeclaration.AddParameterListParameters(parameter);
                    }
                default:
                    {
                        throw new InvalidOperationException();
                    }
            }
        }

        private static BaseListSyntax GetBaseList(SyntaxNode node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                    return ((ClassDeclarationSyntax)node).BaseList;
                case SyntaxKind.InterfaceDeclaration:
                    return ((InterfaceDeclarationSyntax)node).BaseList;
                default:
                    return null;
            }
        }
    }
}
