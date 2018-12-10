// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Roslynator.CSharp.Refactorings.ExpandLinqMethodOperation
{
    internal sealed class ExtractFirstOrDefaultToLocalFunctionRefactoring : ExtractLinqToLocalFunctionRefactoring
    {
        public ExtractFirstOrDefaultToLocalFunctionRefactoring(
            Document document,
            InvocationExpressionSyntax invocationExpression,
            SyntaxNode body,
            SyntaxNode containingBody,
            ITypeSymbol elementTypeSymbol,
            ImmutableArray<ISymbol> capturedSymbols,
            SemanticModel semanticModel) : base(document, invocationExpression, body, containingBody, elementTypeSymbol, capturedSymbols, semanticModel)
        {
        }

        public override string MethodName
        {
            get { return "FirstOrDefault"; }
        }

        protected override ReturnStatementSyntax GetFirstReturnStatement()
        {
            return ReturnStatement(IdentifierName(Parameter.Identifier.ValueText));
        }

        protected override ReturnStatementSyntax GetLastReturnStatement()
        {
            ExpressionSyntax expression = ElementTypeSymbol.GetDefaultValueSyntax(Document.GetDefaultSyntaxOptions(), ElementType);

            return ReturnStatement(expression);
        }

        protected override TypeSyntax GetReturnType()
        {
            return ElementType;
        }
    }
}
