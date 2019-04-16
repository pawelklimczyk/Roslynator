﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// <auto-generated>

using System;
using Microsoft.CodeAnalysis;

namespace Roslynator.CodeAnalysis.CSharp
{
    public static partial class DiagnosticDescriptors
    {
        /// <summary>ROS0001</summary>
        public static readonly DiagnosticDescriptor UsePropertySyntaxNodeSpanStart = new DiagnosticDescriptor(
            id:                 DiagnosticIdentifiers.UsePropertySyntaxNodeSpanStart, 
            title:              "Use property SyntaxNode.SpanStart.", 
            messageFormat:      "Use property SyntaxNode.SpanStart.", 
            category:           DiagnosticCategories.Performance, 
            defaultSeverity:    DiagnosticSeverity.Info, 
            isEnabledByDefault: true, 
            description:        null, 
            helpLinkUri:        $"{HelpLinkUriRoot}{DiagnosticIdentifiers.UsePropertySyntaxNodeSpanStart}", 
            customTags:         Array.Empty<string>());

        /// <summary>ROS0002</summary>
        public static readonly DiagnosticDescriptor UsePropertySyntaxNodeRawKind = new DiagnosticDescriptor(
            id:                 DiagnosticIdentifiers.UsePropertySyntaxNodeRawKind, 
            title:              "Use property SyntaxNode.RawKind.", 
            messageFormat:      "Use property SyntaxNode.RawKind.", 
            category:           DiagnosticCategories.Performance, 
            defaultSeverity:    DiagnosticSeverity.Info, 
            isEnabledByDefault: true, 
            description:        null, 
            helpLinkUri:        $"{HelpLinkUriRoot}{DiagnosticIdentifiers.UsePropertySyntaxNodeRawKind}", 
            customTags:         Array.Empty<string>());

        /// <summary>ROS0003</summary>
        public static readonly DiagnosticDescriptor RedundantConditionalAccess = new DiagnosticDescriptor(
            id:                 DiagnosticIdentifiers.RedundantConditionalAccess, 
            title:              "Redundant conditional access.", 
            messageFormat:      "Redundant conditional access.", 
            category:           DiagnosticCategories.Performance, 
            defaultSeverity:    DiagnosticSeverity.Info, 
            isEnabledByDefault: true, 
            description:        null, 
            helpLinkUri:        $"{HelpLinkUriRoot}{DiagnosticIdentifiers.RedundantConditionalAccess}", 
            customTags:         WellKnownDiagnosticTags.Unnecessary);

        public static readonly DiagnosticDescriptor RedundantConditionalAccessFadeOut = RedundantConditionalAccess.CreateFadeOut();

        /// <summary>ROS0004</summary>
        public static readonly DiagnosticDescriptor UsePatternMatching = new DiagnosticDescriptor(
            id:                 DiagnosticIdentifiers.UsePatternMatching, 
            title:              "Use pattern matching.", 
            messageFormat:      "Use pattern matching.", 
            category:           DiagnosticCategories.Usage, 
            defaultSeverity:    DiagnosticSeverity.Info, 
            isEnabledByDefault: true, 
            description:        null, 
            helpLinkUri:        $"{HelpLinkUriRoot}{DiagnosticIdentifiers.UsePatternMatching}", 
            customTags:         Array.Empty<string>());

        /// <summary>ROS0005</summary>
        public static readonly DiagnosticDescriptor CallAnyInsteadOfUsingCount = new DiagnosticDescriptor(
            id:                 DiagnosticIdentifiers.CallAnyInsteadOfUsingCount, 
            title:              "Call 'Any' instead of using 'Count'.", 
            messageFormat:      "Call 'Any' instead of using 'Count'.", 
            category:           DiagnosticCategories.Performance, 
            defaultSeverity:    DiagnosticSeverity.Info, 
            isEnabledByDefault: true, 
            description:        null, 
            helpLinkUri:        $"{HelpLinkUriRoot}{DiagnosticIdentifiers.CallAnyInsteadOfUsingCount}", 
            customTags:         Array.Empty<string>());

        /// <summary>ROS0006</summary>
        public static readonly DiagnosticDescriptor UnnecessaryNullCheck = new DiagnosticDescriptor(
            id:                 DiagnosticIdentifiers.UnnecessaryNullCheck, 
            title:              "Unnecessary null check.", 
            messageFormat:      "Unnecessary null check.", 
            category:           DiagnosticCategories.Performance, 
            defaultSeverity:    DiagnosticSeverity.Info, 
            isEnabledByDefault: true, 
            description:        null, 
            helpLinkUri:        $"{HelpLinkUriRoot}{DiagnosticIdentifiers.UnnecessaryNullCheck}", 
            customTags:         WellKnownDiagnosticTags.Unnecessary);

    }
}