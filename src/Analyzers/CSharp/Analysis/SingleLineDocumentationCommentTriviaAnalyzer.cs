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
                                if (info.IsContentEmptyOrWhitespace)
                                    context.ReportDiagnosticIfNotSuppressed(DiagnosticDescriptors.AddSummaryToDocumentationComment, info.Element);

                                containsSummaryElement = true;
                                break;
                            }
                        case XmlElementKind.Code:
                        case XmlElementKind.Example:
                        case XmlElementKind.Remarks:
                        case XmlElementKind.Returns:
                        case XmlElementKind.Value:
                            {
                                if (info.IsContentEmptyOrWhitespace)
                                    context.ReportDiagnosticIfNotSuppressed(DiagnosticDescriptors.UnusedElementInDocumentationComment, info.Element);

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
                && !containsContentElement)
            {
                context.ReportDiagnosticIfNotSuppressed(DiagnosticDescriptors.AddSummaryElementToDocumentationComment, documentationComment);
            }

            SyntaxNode parent = documentationComment.ParentTrivia.Token.Parent;

            bool unusedElement = !context.IsAnalyzerSuppressed(DiagnosticDescriptors.UnusedElementInDocumentationComment);
            bool reorderParams = !context.IsAnalyzerSuppressed(DiagnosticDescriptors.ReorderElementsInDocumentationComment);
            bool addParam = !context.IsAnalyzerSuppressed(DiagnosticDescriptors.AddParamElementToDocumentationComment);
            bool addTypeParam = !context.IsAnalyzerSuppressed(DiagnosticDescriptors.AddTypeParamElementToDocumentationComment);

            if (addParam
                || reorderParams
                || unusedElement)
            {
                SeparatedSyntaxList<ParameterSyntax> parameters = ParameterListInfo.Create(parent).Parameters;

                if (parameters.Any())
                {
                    if (addParam)
                        AnalyzeParam(context, documentationComment, parameters);

                    if (reorderParams || unusedElement)
                    {
                        Analyze(context, documentationComment, parameters, XmlElementKind.Param, IndexOf);
                    }
                }
            }

            if (addTypeParam
                || reorderParams
                || unusedElement)
            {
                SeparatedSyntaxList<TypeParameterSyntax> typeParameters = TypeParameterListInfo.Create(parent).Parameters;

                if (typeParameters.Any())
                {
                    if (addTypeParam)
                        AnalyzeTypeParam(context, documentationComment, typeParameters);

                    if (reorderParams || unusedElement)
                    {
                        Analyze(context, documentationComment, typeParameters, XmlElementKind.TypeParam, IndexOf);
                    }
                }
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
                        && elementInfo.IsElementKind(XmlElementKind.Param))
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
                        && elementInfo.IsElementKind(XmlElementKind.TypeParam))
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

        private static void Analyze<TNode>(
            SyntaxNodeAnalysisContext context,
            DocumentationCommentTriviaSyntax documentationComment,
            SeparatedSyntaxList<TNode> nodes,
            XmlElementKind kind,
            Func<SeparatedSyntaxList<TNode>, string, int> indexOf) where TNode : SyntaxNode
        {
            XmlElementSyntax firstElement = null;

            int firstIndex = -1;

            foreach (XmlNodeSyntax xmlNode in documentationComment.Content)
            {
                XmlElementInfo elementInfo = SyntaxInfo.XmlElementInfo(xmlNode);

                if (!elementInfo.Success)
                    continue;

                if (!elementInfo.IsElementKind(kind))
                {
                    firstIndex = -1;
                    continue;
                }

                var element = (XmlElementSyntax)elementInfo.Element;

                string name = element.GetAttributeValue("name");

                if (name == null)
                {
                    firstIndex = -1;
                    continue;
                }

                int index = indexOf(nodes, name);

                if (index == -1)
                {
                    context.ReportDiagnosticIfNotSuppressed(DiagnosticDescriptors.UnusedElementInDocumentationComment, element);
                }
                else if (firstIndex == -1)
                {
                    firstElement = element;
                }
                else if (index < firstIndex)
                {
                    context.ReportDiagnosticIfNotSuppressed(DiagnosticDescriptors.ReorderElementsInDocumentationComment, firstElement);
                }

                firstIndex = index;
            }
        }

        private static int IndexOf(SeparatedSyntaxList<ParameterSyntax> parameters, string name)
        {
            for (int i = 0; i < parameters.Count; i++)
            {
                if (string.Equals(parameters[i].Identifier.ValueText, name, StringComparison.Ordinal))
                    return i;
            }

            return -1;
        }

        private static int IndexOf(SeparatedSyntaxList<TypeParameterSyntax> typeParameters, string name)
        {
            for (int i = 0; i < typeParameters.Count; i++)
            {
                if (string.Equals(typeParameters[i].Identifier.ValueText, name, StringComparison.Ordinal))
                    return i;
            }

            return -1;
        }
    }
}
