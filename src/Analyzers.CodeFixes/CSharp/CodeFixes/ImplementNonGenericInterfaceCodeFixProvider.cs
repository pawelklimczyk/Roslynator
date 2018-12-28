// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslynator.CodeFixes;

namespace Roslynator.CSharp.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ImplementNonGenericInterfaceCodeFixProvider))]
    [Shared]
    public class ImplementNonGenericInterfaceCodeFixProvider : BaseCodeFixProvider
    {
        private const string IComparableCompareText = @"
public int CompareTo(object obj)
{
    if (obj == null)
    {
        return 1;
    }

    if (obj is T x)
    {
        return CompareTo(x);
    }

    throw new global::System.ArgumentException($""An argument must be '{nameof(T)}'."", nameof(obj));
}
";

        private const string IComparerCompareText = @"
public int Compare(object x, object y)
{
    if (x == y)
    {
        return 0;
    }

    if (x == null)
    {
        return -1;
    }

    if (y == null)
    {
        return 1;
    }

    if (x is T a
        && y is T b)
    {
        return Compare(a, b);
    }

    if (x is global::System.IComparable ic)
    {
        return ic.CompareTo(y);
    }

    throw new global::System.ArgumentException(""An object must implement IComparable."", nameof(x));
}
";

        private const string IEqualityComparerEqualsText = @"
new public bool Equals(object x, object y)
{
    if (x == y)
    {
        return true;
    }

    if (x == null || y == null)
    {
        return false;
    }

    if (x is T a
        && y is T b)
    {
        return Equals(a, b);
    }

    return x.Equals(y);
}
";

        private const string IEqualityComparerGetHashCodeText = @"
public int GetHashCode(object obj)
{
    if (obj == null)
    {
        throw new global::System.ArgumentNullException(nameof(obj));
    }

    if (obj is T x)
    {
        return GetHashCode(x);
    }

    return obj.GetHashCode();
}
";

        private static readonly Lazy<MethodDeclarationSyntax> _lazyIComparableCompare = new Lazy<MethodDeclarationSyntax>(() => CreateMethodDeclaration(IComparableCompareText));
        private static readonly Lazy<MethodDeclarationSyntax> _lazyIComparerCompare = new Lazy<MethodDeclarationSyntax>(() => CreateMethodDeclaration(IComparerCompareText));
        private static readonly Lazy<MethodDeclarationSyntax> _lazyIEqualityComparerEquals = new Lazy<MethodDeclarationSyntax>(() => CreateMethodDeclaration(IEqualityComparerEqualsText));
        private static readonly Lazy<MethodDeclarationSyntax> _lazyIEqualityComparerGetHashCode = new Lazy<MethodDeclarationSyntax>(() => CreateMethodDeclaration(IEqualityComparerGetHashCodeText));

        private static MethodDeclarationSyntax CreateMethodDeclaration(string text)
        {
            CompilationUnitSyntax compilationUnit = SyntaxFactory.ParseCompilationUnit($@"class C<T>
{{
    {text}
}}");

            var classDeclaration = (ClassDeclarationSyntax)compilationUnit.Members[0];

            var methodDeclaration = (MethodDeclarationSyntax)classDeclaration.Members[0];

            methodDeclaration = (MethodDeclarationSyntax)AddSimplifierAnnotationRewriter.Instance.VisitMethodDeclaration(methodDeclaration);

            return methodDeclaration.WithFormatterAnnotation();
        }

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(DiagnosticIdentifiers.ImplementNonGenericInterface); }
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode root = await context.GetSyntaxRootAsync().ConfigureAwait(false);

            if (!TryFindFirstAncestorOrSelf(root, context.Span, out TypeDeclarationSyntax typeDeclaration))
                return;

            Diagnostic diagnostic = context.Diagnostics[0];

            Document document = context.Document;

            string interfaceName = diagnostic.Properties["InterfaceName"];

            CodeAction codeAction = CodeAction.Create(
                $"Implement {interfaceName}",
                ct => RefactorAsync(document, typeDeclaration, interfaceName, ct),
                GetEquivalenceKey(diagnostic));

            context.RegisterCodeFix(codeAction, diagnostic);
        }

        private static async Task<Document> RefactorAsync(
            Document document,
            TypeDeclarationSyntax typeDeclaration,
            string interfaceName,
            CancellationToken cancellationToken)
        {
            SemanticModel semanticModel = await document.GetSemanticModelAsync().ConfigureAwait(false);

            INamedTypeSymbol symbol = semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken);

            ImmutableArray<INamedTypeSymbol> interfaces = symbol.Interfaces;

            TypeDeclarationSyntax newTypeDeclaration = typeDeclaration;

            TypeSyntax interfaceType = null;

            switch (interfaceName)
            {
                case "IComparable":
                    {
                        TypeSyntax type = interfaces
                            .First(f => f.HasMetadataName(MetadataNames.System_IComparable_T))
                            .TypeArguments
                            .Single().ToTypeSyntax()
                            .WithSimplifierAnnotation();

                        var rewriter = new AddTypeNameRewriter(type);

                        MethodDeclarationSyntax methodDeclaration = _lazyIComparableCompare.Value;

                        methodDeclaration = (MethodDeclarationSyntax)rewriter.VisitMethodDeclaration(methodDeclaration);

                        newTypeDeclaration = MemberDeclarationInserter.Default.Insert(typeDeclaration, methodDeclaration);

                        interfaceType = SyntaxFactory.ParseTypeName("global::System.IComparable").WithSimplifierAnnotation();
                        break;
                    }
                case "IComparer":
                    {
                        TypeSyntax type = interfaces
                            .First(f => f.HasMetadataName(MetadataNames.System_Collections_Generic_IComparer_T))
                            .TypeArguments
                            .Single()
                            .ToTypeSyntax()
                            .WithSimplifierAnnotation();

                        var rewriter = new AddTypeNameRewriter(type);

                        MethodDeclarationSyntax methodDeclaration = _lazyIComparerCompare.Value;

                        methodDeclaration = (MethodDeclarationSyntax)rewriter.VisitMethodDeclaration(methodDeclaration);

                        newTypeDeclaration = MemberDeclarationInserter.Default.Insert(typeDeclaration, methodDeclaration);

                        interfaceType = SyntaxFactory.ParseTypeName("global::System.Collections.IComparer").WithSimplifierAnnotation();
                        break;
                    }
                case "IEqualityComparer":
                    {
                        TypeSyntax type = interfaces
                            .First(f => f.HasMetadataName(MetadataNames.System_Collections_Generic_IEqualityComparer_T))
                            .TypeArguments
                            .Single()
                            .ToTypeSyntax()
                            .WithSimplifierAnnotation();

                        var rewriter = new AddTypeNameRewriter(type);

                        MethodDeclarationSyntax equalsMethod = _lazyIEqualityComparerEquals.Value;

                        equalsMethod = (MethodDeclarationSyntax)rewriter.VisitMethodDeclaration(equalsMethod);

                        newTypeDeclaration = MemberDeclarationInserter.Default.Insert(typeDeclaration, equalsMethod);

                        MethodDeclarationSyntax getHashCodeMethod = _lazyIEqualityComparerGetHashCode.Value;

                        getHashCodeMethod = (MethodDeclarationSyntax)rewriter.VisitMethodDeclaration(getHashCodeMethod);

                        newTypeDeclaration = MemberDeclarationInserter.Default.Insert(newTypeDeclaration, getHashCodeMethod);

                        interfaceType = SyntaxFactory.ParseTypeName("global::System.Collections.IEqualityComparer").WithSimplifierAnnotation();
                        break;
                    }
                default:
                    {
                        throw new InvalidOperationException();
                    }
            }

            SyntaxKind kind = newTypeDeclaration.Kind();

            if (kind == SyntaxKind.ClassDeclaration)
            {
                var classDeclaration = (ClassDeclarationSyntax)newTypeDeclaration;

                newTypeDeclaration = classDeclaration.AddBaseListTypes(SyntaxFactory.SimpleBaseType(interfaceType));
            }
            else if (kind == SyntaxKind.StructDeclaration)
            {
                var structDeclaration = (StructDeclarationSyntax)newTypeDeclaration;

                newTypeDeclaration = structDeclaration.AddBaseListTypes(SyntaxFactory.SimpleBaseType(interfaceType));
            }

            return await document.ReplaceNodeAsync(typeDeclaration, newTypeDeclaration, cancellationToken).ConfigureAwait(false);
        }

        private class AddSimplifierAnnotationRewriter : CSharpSyntaxRewriter
        {
            public static AddSimplifierAnnotationRewriter Instance { get; } = new AddSimplifierAnnotationRewriter();

            public override SyntaxNode VisitQualifiedName(QualifiedNameSyntax node)
            {
                return node.WithSimplifierAnnotation();
            }
        }

        private class AddTypeNameRewriter : CSharpSyntaxRewriter
        {
            private readonly TypeSyntax _type;

            public AddTypeNameRewriter(TypeSyntax type)
            {
                _type = type;
            }

            public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
            {
                if (node.Identifier.ValueText == "T")
                {
                    return _type.WithTriviaFrom(node);
                }

                return base.VisitIdentifierName(node);
            }
        }
    }
}
