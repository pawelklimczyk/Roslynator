// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslynator.CSharp;
using Roslynator.CSharp.Syntax;

namespace Roslynator.CodeAnalysis.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SwitchStatementAnalyzer : BaseDiagnosticAnalyzer
    {
        private static ImmutableHashSet<string> _syntaxKinds;
        private static ImmutableHashSet<string> _syntaxNames;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(DiagnosticDescriptors.UsePatternMatching); }
        }

        public override void Initialize(AnalysisContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            base.Initialize(context);

            context.RegisterCompilationStartAction(startContext =>
            {
                if (_syntaxKinds == null)
                {
                    Compilation compilation = startContext.Compilation;

                    INamedTypeSymbol syntaxNodeSymbol = compilation.GetTypeByMetadataName("Microsoft.CodeAnalysis.CSharp.CSharpSyntaxNode");

                    ImmutableDictionary<string, string> kindsToNames = compilation
                        .GetTypeByMetadataName("Microsoft.CodeAnalysis.CSharp.Syntax.AccessorDeclarationSyntax")
                        .ContainingNamespace
                        .GetTypeMembers()
                        .Where(f => f.TypeKind == TypeKind.Class && !f.IsAbstract && f.InheritsFrom(syntaxNodeSymbol))
                        .Select(f => f.Name)
                        .Where(f => f.EndsWith("Syntax", StringComparison.Ordinal))
                        .ToImmutableDictionary(f => f.Remove(f.Length - 6), f => f);

                    ImmutableHashSet<string>.Builder syntaxKinds = ImmutableHashSet.CreateBuilder<string>();
                    ImmutableHashSet<string>.Builder syntaxNames = ImmutableHashSet.CreateBuilder<string>();

                    foreach (string syntaxKind in Enum.GetValues(typeof(SyntaxKind))
                        .Cast<SyntaxKind>()
                        .Select(f => f.ToString()))
                    {
                        if (kindsToNames.TryGetValue(syntaxKind, out string symbolName))
                        {
                            syntaxKinds.Add(syntaxKind);
                            syntaxNames.Add(symbolName);
                        }
                    }

                    Interlocked.CompareExchange(ref _syntaxKinds, syntaxKinds.ToImmutable(), null);
                    Interlocked.CompareExchange(ref _syntaxNames, syntaxNames.ToImmutable(), null);
                }

                startContext.RegisterSyntaxNodeAction(AnalyzeSwitchStatement, SyntaxKind.SwitchStatement);
            });
        }

        private static void AnalyzeSwitchStatement(SyntaxNodeAnalysisContext context)
        {
            var switchStatement = (SwitchStatementSyntax)context.Node;

            SyntaxList<SwitchSectionSyntax> sections = switchStatement.Sections;

            if (!sections.Any())
                return;

            ExpressionSyntax switchExpression = switchStatement.Expression;

            string name = GetName();

            if (name == null)
                return;

            ITypeSymbol kindSymbol = context.SemanticModel.GetTypeSymbol(switchExpression, context.CancellationToken);

            if (kindSymbol?.HasMetadataName(MetadataNames.Microsoft_CodeAnalysis_CSharp_SyntaxKind) != true)
                return;

            foreach (SwitchSectionSyntax section in sections)
            {
                SwitchLabelSyntax label = section.Labels.SingleOrDefault(shouldThrow: false);

                SyntaxKind labelKind = label.Kind();

                if (labelKind == SyntaxKind.DefaultSwitchLabel)
                    continue;

                if (labelKind != SyntaxKind.CaseSwitchLabel)
                {
                    Debug.Assert(labelKind == SyntaxKind.CasePatternSwitchLabel, labelKind.ToString());
                    return;
                }

                var caseLabel = (CaseSwitchLabelSyntax)label;

                ExpressionSyntax value = caseLabel.Value;

                if (!value.IsKind(SyntaxKind.SimpleMemberAccessExpression))
                    return;

                var memberAccess = (MemberAccessExpressionSyntax)value;

                SimpleNameSyntax simpleName = memberAccess.Name;

                if (!simpleName.IsKind(SyntaxKind.IdentifierName))
                    return;

                var identifierName = (IdentifierNameSyntax)simpleName;

                string kindName = identifierName.Identifier.ValueText;

                if (!_syntaxKinds.Contains(kindName))
                    return;

                SyntaxList<StatementSyntax> statements = section.Statements;

                StatementSyntax statement = statements.FirstOrDefault();

                if (statement == null)
                    return;

                if (statement.IsKind(SyntaxKind.Block))
                {
                    var block = (BlockSyntax)statement;

                    statement = block.Statements.FirstOrDefault();
                }

                if (!statement.IsKind(SyntaxKind.LocalDeclarationStatement))
                    return;

                SingleLocalDeclarationStatementInfo localStatement = SyntaxInfo.SingleLocalDeclarationStatementInfo((LocalDeclarationStatementSyntax)statement);

                if (!localStatement.Success)
                    return;

                if (!localStatement.Value.IsKind(SyntaxKind.CastExpression))
                    return;

                var castExpression = (CastExpressionSyntax)localStatement.Value;

                if (!castExpression.Expression.IsKind(SyntaxKind.IdentifierName))
                    return;

                var localName = (IdentifierNameSyntax)castExpression.Expression;

                if (name != localName.Identifier.ValueText)
                    return;

                TypeSyntax type = castExpression.Type;

                ITypeSymbol syntaxSymbol = context.SemanticModel.GetTypeSymbol(type, context.CancellationToken);

                if (syntaxSymbol.IsErrorType())
                    return;

                string syntaxName = syntaxSymbol.Name;

                if (!_syntaxNames.Contains(syntaxName))
                    return;

                if (kindName.Length != syntaxName.Length - 6)
                    return;

                if (string.Compare(kindName, 0, syntaxName, 0, kindName.Length, StringComparison.Ordinal) != 0)
                    return;

                if (!syntaxSymbol.InheritsFrom(MetadataNames.Microsoft_CodeAnalysis_CSharp_CSharpSyntaxNode))
                    return;
            }

            context.ReportDiagnostic(DiagnosticDescriptors.UsePatternMatching, switchStatement.SwitchKeyword);

            string GetName()
            {
                if (!switchExpression.IsKind(SyntaxKind.InvocationExpression))
                    return null;

                SimpleMemberInvocationExpressionInfo invocationInfo = SyntaxInfo.SimpleMemberInvocationExpressionInfo((InvocationExpressionSyntax)switchExpression);

                if (!invocationInfo.Success)
                    return null;

                if (invocationInfo.Arguments.Any())
                    return null;

                if (invocationInfo.NameText != "Kind")
                    return null;

                if (!invocationInfo.Expression.IsKind(SyntaxKind.IdentifierName))
                    return null;

                var identifierName = (IdentifierNameSyntax)invocationInfo.Expression;

                return identifierName.Identifier.ValueText;
            }
        }
    }
}
