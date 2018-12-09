// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslynator.CSharp.Syntax;

namespace Roslynator.CSharp.Analysis
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SingleLineDocumentationCommentTriviaAnalyzer : BaseDiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(
                    DiagnosticDescriptors.AddSummaryToDocumentationComment,
                    DiagnosticDescriptors.AddSummaryElementToDocumentationComment,
                    DiagnosticDescriptors.AddParamElementToDocumentationComment,
                    DiagnosticDescriptors.AddTypeParamElementToDocumentationComment,
                    DiagnosticDescriptors.UnusedElementInDocumentationComment);
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            base.Initialize(context);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(startContext =>
            {
                if (!startContext.IsAnalyzerSuppressed(DiagnosticDescriptors.AddSummaryToDocumentationComment)
                    || !startContext.IsAnalyzerSuppressed(DiagnosticDescriptors.AddSummaryElementToDocumentationComment)
                    || !startContext.IsAnalyzerSuppressed(DiagnosticDescriptors.AddParamElementToDocumentationComment)
                    || !startContext.IsAnalyzerSuppressed(DiagnosticDescriptors.AddTypeParamElementToDocumentationComment)
                    || !startContext.IsAnalyzerSuppressed(DiagnosticDescriptors.UnusedElementInDocumentationComment))
                {
                    startContext.RegisterSyntaxNodeAction(AnalyzeSingleLineDocumentationCommentTrivia, SyntaxKind.SingleLineDocumentationCommentTrivia);
                }
            });
        }

        private static void AnalyzeSingleLineDocumentationCommentTrivia(SyntaxNodeAnalysisContext context)
        {
            var documentationComment = (DocumentationCommentTriviaSyntax)context.Node;

            if (!documentationComment.IsPartOfMemberDeclaration())
                return;

            bool containsInheritDoc = false;
            bool containsIncludeOrExclude = false;
            bool containsSummaryElement = false;
            bool containsContentElement = false;
            bool isFirst = true;

            CancellationToken cancellationToken = context.CancellationToken;

            foreach (XmlNodeSyntax xmlNode in documentationComment.Content)
            {
                cancellationToken.ThrowIfCancellationRequested();

                XmlElementInfo info = SyntaxInfo.XmlElementInfo(xmlNode);

                if (info.Success)
                {
                    switch (info.GetElementKind())
                    {
                        case XmlElementKind.Include:
                        case XmlElementKind.Exclude:
                            {
                                if (isFirst)
                                    containsIncludeOrExclude = true;

                                break;
                            }
                        case XmlElementKind.InheritDoc:
                            {
                                containsInheritDoc = true;
                                break;
                            }
                        case XmlElementKind.Content:
                            {
                                containsContentElement = true;
                                break;
                            }
                        case XmlElementKind.Summary:
                            {
                                if (!context.IsAnalyzerSuppressed(DiagnosticDescriptors.AddSummaryToDocumentationComment)
                                    && info.IsContentEmptyOrWhitespace)
                                {
                                    context.ReportDiagnostic(DiagnosticDescriptors.AddSummaryToDocumentationComment, info.Element);
                                }

                                containsSummaryElement = true;
                                break;
                            }
                        case XmlElementKind.Code:
                        case XmlElementKind.Example:
                        case XmlElementKind.Remarks:
                        case XmlElementKind.Returns:
                        case XmlElementKind.Value:
                            {
                                if (!context.IsAnalyzerSuppressed(DiagnosticDescriptors.UnusedElementInDocumentationComment)
                                    && info.IsContentEmptyOrWhitespace)
                                {
                                    context.ReportDiagnostic(DiagnosticDescriptors.UnusedElementInDocumentationComment, info.Element);
                                }

                                break;
                            }
                    }

                    if (isFirst)
                    {
                        isFirst = false;
                    }
                    else
                    {
                        containsIncludeOrExclude = false;
                    }
                }
            }

            if (containsInheritDoc
                || containsIncludeOrExclude)
            {
                return;
            }

            if (!containsSummaryElement
                && !containsContentElement
                && !context.IsAnalyzerSuppressed(DiagnosticDescriptors.AddSummaryElementToDocumentationComment))
            {
                context.ReportDiagnostic(DiagnosticDescriptors.AddSummaryElementToDocumentationComment, documentationComment);
            }

            SyntaxNode parent = documentationComment.ParentTrivia.Token.Parent;

            if (!context.IsAnalyzerSuppressed(DiagnosticDescriptors.AddParamElementToDocumentationComment))
            {
                SeparatedSyntaxList<ParameterSyntax> parameters = ParameterListInfo.Create(parent).Parameters;

                AnalyzeParam(context, documentationComment, parameters);
            }

            if (!context.IsAnalyzerSuppressed(DiagnosticDescriptors.AddTypeParamElementToDocumentationComment))
            {
                SeparatedSyntaxList<TypeParameterSyntax> typeParameters = TypeParameterListInfo.Create(parent).Parameters;

                AnalyzeTypeParam(context, documentationComment, typeParameters);
            }
        }

        private static void AnalyzeParam(SyntaxNodeAnalysisContext context, DocumentationCommentTriviaSyntax documentationComment, SeparatedSyntaxList<ParameterSyntax> parameters)
        {
            foreach (ParameterSyntax parameter in parameters)
            {
                bool isMissing = true;

                foreach (XmlNodeSyntax xmlNode in documentationComment.Content)
                {
                    XmlElementInfo elementInfo = SyntaxInfo.XmlElementInfo(xmlNode);

                    if (elementInfo.Success
                        && !elementInfo.IsEmptyElement
                        && elementInfo.GetElementKind() == XmlElementKind.Param)
                    {
                        var element = (XmlElementSyntax)elementInfo.Element;

                        string value = element.GetAttributeValue("name");

                        if (value != null
                            && string.Equals(parameter.Identifier.ValueText, value, StringComparison.Ordinal))
                        {
                            isMissing = false;
                            break;
                        }
                    }
                }

                if (isMissing)
                {
                    context.ReportDiagnostic(DiagnosticDescriptors.AddParamElementToDocumentationComment, documentationComment);
                    return;
                }
            }
        }

        private static void AnalyzeTypeParam(SyntaxNodeAnalysisContext context, DocumentationCommentTriviaSyntax documentationComment, SeparatedSyntaxList<TypeParameterSyntax> typeParameters)
        {
            foreach (TypeParameterSyntax typeParameter in typeParameters)
            {
                bool isMissing = true;

                foreach (XmlNodeSyntax xmlNode in documentationComment.Content)
                {
                    XmlElementInfo elementInfo = SyntaxInfo.XmlElementInfo(xmlNode);

                    if (elementInfo.Success
                        && !elementInfo.IsEmptyElement
                        && elementInfo.GetElementKind() == XmlElementKind.TypeParam)
                    {
                        var element = (XmlElementSyntax)elementInfo.Element;

                        string value = element.GetAttributeValue("name");

                        if (value != null
                            && string.Equals(typeParameter.Identifier.ValueText, value, StringComparison.Ordinal))
                        {
                            isMissing = false;
                            break;
                        }
                    }
                }

                if (isMissing)
                {
                    context.ReportDiagnostic(DiagnosticDescriptors.AddTypeParamElementToDocumentationComment, documentationComment);
                    return;
                }
            }
        }
    }
}
