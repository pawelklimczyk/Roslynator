// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using static Roslynator.Logger;

namespace Roslynator.CommandLine
{
    internal class AnalyzeAssemblyCommand
    {
        public AnalyzeAssemblyCommand(string language = null)
        {
            Language = language;
        }

        public string Language { get; }

        public CommandResult Execute(AnalyzeAssemblyCommandLineOptions options)
        {
            var assemblies = new HashSet<Assembly>();

            AnalyzerAssemblyInfo[] analyzerAssemblies = options.GetPaths()
                .SelectMany(path => AnalyzerAssemblyLoader.LoadFrom(
                    path: path,
                    loadAnalyzers: !options.NoAnalyzers,
                    loadFixers: !options.NoFixers,
                    language: Language))
                .OrderBy(f => f.AnalyzerAssembly.Assembly.GetName().Name)
                .ThenBy(f => f.FilePath)
                .ToArray();

            for (int i = 0; i < analyzerAssemblies.Length; i++)
            {
                AnalyzerAssembly analyzerAssembly = analyzerAssemblies[i].AnalyzerAssembly;

                if (assemblies.Add(analyzerAssembly.Assembly))
                {
                    WriteLine(analyzerAssembly.FullName, ConsoleColor.Cyan, Verbosity.Minimal);

                    if (ShouldWrite(Verbosity.Normal))
                    {
                        WriteAnalyzerAssembly(analyzerAssemblies[i]);

                        if (i < analyzerAssemblies.Length - 1)
                            WriteLine(Verbosity.Normal);
                    }
                }
                else
                {
                    Write(analyzerAssembly.FullName, ConsoleColor.DarkGray, Verbosity.Minimal);
                    WriteLine($" [{analyzerAssemblies[i].FilePath}]", ConsoleColor.DarkGray, Verbosity.Minimal);
                }
            }

            if (ShouldWrite(Verbosity.Detailed)
                && analyzerAssemblies.Length > 1)
            {
                ImmutableArray<DiagnosticAnalyzer> analyzers = (!options.NoAnalyzers)
                    ? analyzerAssemblies.SelectMany(f => f.AnalyzerAssembly.Analyzers).ToImmutableArray()
                    : ImmutableArray<DiagnosticAnalyzer>.Empty;

                ImmutableArray<CodeFixProvider> fixers = (!options.NoFixers)
                    ? analyzerAssemblies.SelectMany(f => f.AnalyzerAssembly.Fixers).ToImmutableArray()
                    : ImmutableArray<CodeFixProvider>.Empty;

                WriteLine(Verbosity.Detailed);
                WriteDiagnostics(analyzers, fixers, assemblyQualifiedName: true);
            }

            WriteLine(Verbosity.Minimal);
            WriteLine($"{assemblies.Count} analyzer {((assemblies.Count == 1) ? "assembly" : "assemblies")} found", ConsoleColor.Green, Verbosity.Minimal);
            WriteLine(Verbosity.Minimal);

            return CommandResult.Success;
        }

        private static void WriteAnalyzerAssembly(AnalyzerAssemblyInfo analyzerAssemblyInfo)
        {
            AnalyzerAssembly analyzerAssembly = analyzerAssemblyInfo.AnalyzerAssembly;

            WriteLine(Verbosity.Normal);
            WriteLine($"  Location:             {analyzerAssemblyInfo.FilePath}", Verbosity.Normal);

            (int maxLength, int maxDigitLength) = CalculateMaxLength(analyzerAssembly);

            if (analyzerAssembly.HasAnalyzers)
            {
                WriteLine($"  DiagnosticAnalyzers:  {analyzerAssembly.Analyzers.Length.ToString().PadLeft(maxDigitLength)}", Verbosity.Normal);

                foreach (KeyValuePair<string, ImmutableArray<DiagnosticAnalyzer>> kvp in analyzerAssembly.AnalyzersByLanguage.OrderBy(f => f.Key))
                {
                    WriteLine($"    {GetShortLanguageName(kvp.Key).PadRight(maxLength - 2)}{kvp.Value.Length.ToString().PadLeft(maxDigitLength)}", Verbosity.Normal);
                }

                WriteLine($"  SupportedDiagnostics: {analyzerAssembly.SupportedDiagnostics.Length.ToString().PadLeft(maxDigitLength)}", Verbosity.Normal);

                foreach (KeyValuePair<string, ImmutableArray<DiagnosticDescriptor>> kvp in analyzerAssembly.SupportedDiagnosticsByPrefix
                    .OrderBy(f => f.Key))
                {
                    WriteLine($"    {kvp.Key.PadRight(maxLength - 2)}{kvp.Value.Length.ToString().PadLeft(maxDigitLength)}", Verbosity.Normal);
                }
            }

            if (analyzerAssembly.HasFixers)
            {
                WriteLine($"  CodeFixProviders:     {analyzerAssembly.Fixers.Length.ToString().PadLeft(maxDigitLength)}", Verbosity.Normal);

                foreach (KeyValuePair<string, ImmutableArray<CodeFixProvider>> kvp in analyzerAssembly.FixersByLanguage.OrderBy(f => f.Key))
                {
                    WriteLine($"    {GetShortLanguageName(kvp.Key).PadRight(maxLength - 2)}{kvp.Value.Length.ToString().PadLeft(maxDigitLength)}", Verbosity.Normal);
                }

                WriteLine($"  FixableDiagnosticIds: {analyzerAssembly.FixableDiagnosticIds.Length}", Verbosity.Normal);

                foreach (KeyValuePair<string, ImmutableArray<string>> kvp in analyzerAssembly.FixableDiagnosticIdsByPrefix
                    .OrderBy(f => f.Key))
                {
                    WriteLine($"    {kvp.Key.PadRight(maxLength - 2)}{kvp.Value.Length.ToString().PadLeft(maxDigitLength)}", Verbosity.Normal);
                }
            }

            if (ShouldWrite(Verbosity.Detailed))
            {
                if (analyzerAssembly.HasAnalyzers)
                {
                    WriteLine(Verbosity.Detailed);
                    WriteDiagnosticAnalyzers(analyzerAssembly.Analyzers);
                }

                if (analyzerAssembly.HasFixers)
                {
                    WriteLine(Verbosity.Detailed);
                    WriteCodeFixProviders(analyzerAssembly.Fixers);
                }

                WriteLine(Verbosity.Detailed);
                WriteDiagnostics(analyzerAssembly.Analyzers, analyzerAssembly.Fixers);
            }
        }

        private static (int maxLength, int maxDigitLength) CalculateMaxLength(AnalyzerAssembly analyzerAssembly)
        {
            int maxLength = 22;
            int maxDigitLength = 0;

            if (analyzerAssembly.HasAnalyzers)
            {
                maxLength = Math.Max(maxLength, analyzerAssembly.AnalyzersByLanguage.Max(f => GetShortLanguageName(f.Key).Length + 3));
                maxLength = Math.Max(maxLength, analyzerAssembly.SupportedDiagnosticsByPrefix.Max(f => f.Key.Length + 3));

                maxDigitLength = Math.Max(maxDigitLength, analyzerAssembly.Analyzers.Length.ToString().Length);
                maxDigitLength = Math.Max(maxDigitLength, analyzerAssembly.SupportedDiagnostics.Length.ToString().Length);
            }

            if (analyzerAssembly.HasFixers)
            {
                maxLength = Math.Max(maxLength, analyzerAssembly.FixersByLanguage.Max(f => GetShortLanguageName(f.Key).Length + 3));
                maxLength = Math.Max(maxLength, analyzerAssembly.FixableDiagnosticIdsByPrefix.Max(f => f.Key.Length + 3));

                maxDigitLength = Math.Max(maxDigitLength, analyzerAssembly.Fixers.Length.ToString().Length);
                maxDigitLength = Math.Max(maxDigitLength, analyzerAssembly.FixableDiagnosticIds.Length.ToString().Length);
            }

            return (maxLength, maxDigitLength);
        }

        private static void WriteDiagnosticAnalyzers(IEnumerable<DiagnosticAnalyzer> analyzers)
        {
            WriteLine("  DiagnosticAnalyzers:", Verbosity.Detailed);

            foreach (DiagnosticAnalyzer analyzer in analyzers.OrderBy(f => f.GetType(), TypeComparer.NamespaceThenName))
            {
                Type type = analyzer.GetType();

                DiagnosticAnalyzerAttribute attribute = type.GetCustomAttribute<DiagnosticAnalyzerAttribute>();

                WriteLine($"    {type.FullName}", Verbosity.Detailed);

                if (ShouldWrite(Verbosity.Diagnostic))
                {
                    WriteLine($"      Languages:            {string.Join(", ", attribute.Languages.Select(f => GetShortLanguageName(f)).OrderBy(f => f))}", ConsoleColor.DarkGray, Verbosity.Diagnostic);
                    WriteLine($"      SupportedDiagnostics: {string.Join(", ", analyzer.SupportedDiagnostics.Select(f => f.Id).Distinct().OrderBy(f => f))}", ConsoleColor.DarkGray, Verbosity.Diagnostic);
                }
            }
        }

        private static void WriteCodeFixProviders(IEnumerable<CodeFixProvider> fixers)
        {
            WriteLine("  CodeFixProviders:", Verbosity.Detailed);

            foreach (CodeFixProvider fixer in fixers.OrderBy(f => f.GetType(), TypeComparer.NamespaceThenName))
            {
                Type type = fixer.GetType();

                ExportCodeFixProviderAttribute attribute = type.GetCustomAttribute<ExportCodeFixProviderAttribute>();

                WriteLine($"    {type.FullName}", Verbosity.Detailed);

                if (ShouldWrite(Verbosity.Diagnostic))
                {
                    WriteLine($"      Languages:            {string.Join(", ", attribute.Languages.Select(f => GetShortLanguageName(f)).OrderBy(f => f))}", ConsoleColor.DarkGray, Verbosity.Diagnostic);
                    WriteLine($"      FixableDiagnosticIds: {string.Join(", ", fixer.FixableDiagnosticIds.Distinct().OrderBy(f => f))}", ConsoleColor.DarkGray, Verbosity.Diagnostic);

                    Write("      FixAllProvider:       ", ConsoleColor.DarkGray, Verbosity.Diagnostic);

                    FixAllProvider fixAllProvider = fixer.GetFixAllProvider();

                    if (fixAllProvider != null)
                    {
                        WriteLine($"{fixAllProvider.GetType().FullName} ({string.Join(", ", fixAllProvider.GetSupportedFixAllScopes().Select(f => f.ToString()).OrderBy(f => f))})", ConsoleColor.DarkGray, Verbosity.Diagnostic);
                    }
                    else
                    {
                        WriteLine("-", ConsoleColor.DarkGray, Verbosity.Diagnostic);
                    }
                }
            }
        }

        private static void WriteDiagnostics(
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            ImmutableArray<CodeFixProvider> fixers,
            bool assemblyQualifiedName = false)
        {
            Dictionary<string, DiagnosticDescriptor> diagnosticIds = analyzers
                .SelectMany(f => f.SupportedDiagnostics)
                .Distinct(DiagnosticDescriptorComparer.Id)
                .OrderBy(f => f, DiagnosticDescriptorComparer.Id)
                .ToDictionary(f => f.Id, f => f);

            foreach (CodeFixProvider fixer in fixers)
            {
                foreach (string diagnosticId in fixer.FixableDiagnosticIds)
                {
                    if (!diagnosticIds.ContainsKey(diagnosticId))
                        diagnosticIds[diagnosticId] = null;
                }
            }

            Dictionary<string, IEnumerable<DiagnosticAnalyzer>> analyzersById = analyzers
                .SelectMany(analyzer => analyzer.SupportedDiagnostics.Select(descriptor => (analyzer, descriptor)))
                .GroupBy(f => f.descriptor.Id)
                .ToDictionary(g => g.Key, g => g.Select(f => f.analyzer).Distinct());

            Dictionary<string, IEnumerable<CodeFixProvider>> fixersById = fixers
                .SelectMany(fixer => fixer.FixableDiagnosticIds.Select(diagnosticId => (fixer, diagnosticId)))
                .GroupBy(f => f.diagnosticId)
                .ToDictionary(g => g.Key, g => g.Select(f => f.fixer).Distinct());

            WriteLine("  Diagnostics:", Verbosity.Detailed);

            foreach (KeyValuePair<string, DiagnosticDescriptor> kvp in diagnosticIds.OrderBy(f => f.Key))
            {
                string diagnosticId = kvp.Key;
                DiagnosticDescriptor descriptor = kvp.Value;

                if (descriptor == null)
                {
                    WriteLine($"    {diagnosticId}", Verbosity.Detailed);
                }
                else
                {
                    string title = descriptor.Title?.ToString();
                    string messageFormat = descriptor.MessageFormat?.ToString();

                    if (string.IsNullOrEmpty(title))
                        title = messageFormat;

                    WriteLine($"    {diagnosticId} {title}", Verbosity.Detailed);

                    if (ShouldWrite(Verbosity.Diagnostic))
                    {
                        if (title != messageFormat)
                            WriteLine($"      MessageFormat:       {messageFormat}", ConsoleColor.DarkGray, Verbosity.Diagnostic);

                        WriteLine($"      Category:            {descriptor.Category}", ConsoleColor.DarkGray, Verbosity.Diagnostic);
                        WriteLine($"      DefaultSeverity:     {descriptor.DefaultSeverity}", ConsoleColor.DarkGray, Verbosity.Diagnostic);
                        WriteLine($"      IsEnabledByDefault:  {descriptor.IsEnabledByDefault}", ConsoleColor.DarkGray, Verbosity.Diagnostic);

                        string description = descriptor.Description?.ToString();

                        if (!string.IsNullOrEmpty(description))
                            WriteLine($"      Description:         {description}", ConsoleColor.DarkGray, Verbosity.Diagnostic);

                        if (!string.IsNullOrEmpty(descriptor.HelpLinkUri))
                            WriteLine($"      HelpLinkUri:         {descriptor.HelpLinkUri}", ConsoleColor.DarkGray, Verbosity.Diagnostic);

                        if (descriptor.CustomTags?.Any() == true)
                            WriteLine($"      CustomTags:          {string.Join(", ", descriptor.CustomTags)}", ConsoleColor.DarkGray, Verbosity.Diagnostic);
                    }
                }

                if (ShouldWrite(Verbosity.Diagnostic))
                {
                    if (analyzersById.TryGetValue(diagnosticId, out IEnumerable<DiagnosticAnalyzer> analyzers2))
                    {
                        Write("      DiagnosticAnalyzers: ", ConsoleColor.DarkGray, Verbosity.Diagnostic);

                        WriteTypes(analyzers2.Select(f => f.GetType()));
                    }

                    if (fixersById.TryGetValue(diagnosticId, out IEnumerable<CodeFixProvider> fixers2))
                    {
                        Write("      CodeFixProviders:    ", ConsoleColor.DarkGray, Verbosity.Diagnostic);

                        WriteTypes(fixers2.Select(f => f.GetType()));
                    }
                }
            }

            void WriteTypes(IEnumerable<Type> types)
            {
                using (IEnumerator<Type> en = types.OrderBy(f => f, TypeComparer.NamespaceThenName).GetEnumerator())
                {
                    if (en.MoveNext())
                    {
                        while (true)
                        {
                            string name = (assemblyQualifiedName) ? en.Current.AssemblyQualifiedName : en.Current.FullName;

                            WriteLine(name, ConsoleColor.DarkGray, Verbosity.Diagnostic);

                            if (en.MoveNext())
                            {
                                Write("                           ", ConsoleColor.DarkGray, Verbosity.Diagnostic);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }

        private static string GetShortLanguageName(string languageName)
        {
            switch (languageName)
            {
                case LanguageNames.CSharp:
                case LanguageNames.FSharp:
                    return languageName;
                case LanguageNames.VisualBasic:
                    return "VB";
            }

            Debug.Fail(languageName);

            return languageName;
        }
    }
}
