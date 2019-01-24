// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using static Roslynator.Logger;

namespace Roslynator
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal sealed class AnalyzerAssembly : IEquatable<AnalyzerAssembly>
    {
        private ImmutableArray<DiagnosticAnalyzer> _analyzers;
        private ImmutableArray<DiagnosticDescriptor> _supportedDiagnostics;
        private ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>> _supportedDiagnosticsByPrefix;
        private ImmutableArray<CodeFixProvider> _fixers;
        private ImmutableArray<string> _fixableDiagnosticIds;
        private ImmutableDictionary<string, ImmutableArray<string>> _fixableDiagnosticIdsByPrefix;

        private AnalyzerAssembly(
            Assembly assembly,
            ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>> analyzersByLanguage,
            ImmutableDictionary<string, ImmutableArray<CodeFixProvider>> fixersByLanguage,
            bool includeAnalyzers,
            bool includeFixers)
        {
            Assembly = assembly;

            AnalyzersByLanguage = analyzersByLanguage;
            FixersByLanguage = fixersByLanguage;

            IncludeAnalyzers = includeAnalyzers;
            IncludeFixers = includeFixers;
        }

        public Assembly Assembly { get; }

        internal string FullName => Assembly.FullName;

        public bool IncludeAnalyzers { get; }

        public bool IncludeFixers { get; }

        public bool HasAnalyzers => AnalyzersByLanguage.Count > 0;

        public bool HasFixers => FixersByLanguage.Count > 0;

        internal bool IsEmpty => !HasAnalyzers && !HasFixers;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay => FullName;

        public ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>> AnalyzersByLanguage { get; }

        public ImmutableArray<DiagnosticAnalyzer> Analyzers
        {
            get
            {
                if (_analyzers.IsDefault)
                    ImmutableInterlocked.InterlockedInitialize(ref _analyzers, LoadAnalyzers());

                return _analyzers;
            }
        }

        public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                if (_supportedDiagnostics.IsDefault)
                    ImmutableInterlocked.InterlockedInitialize(ref _supportedDiagnostics, LoadSupportedDiagnostics());

                return _supportedDiagnostics;
            }
        }

        public ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>> SupportedDiagnosticsByPrefix
        {
            get
            {
                if (_supportedDiagnosticsByPrefix == null)
                    Interlocked.CompareExchange(ref _supportedDiagnosticsByPrefix, LoadSupportedDiagnosticsByPrefix(), null);

                return _supportedDiagnosticsByPrefix;
            }
        }

        public ImmutableDictionary<string, ImmutableArray<CodeFixProvider>> FixersByLanguage { get; }

        public ImmutableArray<CodeFixProvider> Fixers
        {
            get
            {
                if (_fixers.IsDefault)
                    ImmutableInterlocked.InterlockedInitialize(ref _fixers, LoadFixers());

                return _fixers;
            }
        }

        public ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                if (_fixableDiagnosticIds.IsDefault)
                    ImmutableInterlocked.InterlockedInitialize(ref _fixableDiagnosticIds, LoadFixableDiagnosticIds());

                return _fixableDiagnosticIds;
            }
        }

        public ImmutableDictionary<string, ImmutableArray<string>> FixableDiagnosticIdsByPrefix
        {
            get
            {
                if (_fixableDiagnosticIdsByPrefix == null)
                    Interlocked.CompareExchange(ref _fixableDiagnosticIdsByPrefix, LoadFixableDiagnosticIdsByPrefix(), null);

                return _fixableDiagnosticIdsByPrefix;
            }
        }

        private ImmutableArray<DiagnosticAnalyzer> LoadAnalyzers()
        {
            if (!IncludeAnalyzers)
                return ImmutableArray<DiagnosticAnalyzer>.Empty;

            return AnalyzersByLanguage
                .SelectMany(f => f.Value)
                .Distinct()
                .ToImmutableArray();
        }

        private ImmutableArray<DiagnosticDescriptor> LoadSupportedDiagnostics()
        {
            return Analyzers
                .SelectMany(f => f.SupportedDiagnostics)
                .Distinct(DiagnosticDescriptorComparer.Id)
                .OrderBy(f => f, DiagnosticDescriptorComparer.Id)
                .ToImmutableArray();
        }

        private ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>> LoadSupportedDiagnosticsByPrefix()
        {
            return SupportedDiagnostics
                .GroupBy(f => f, DiagnosticDescriptorComparer.IdPrefix)
                .ToImmutableDictionary(f => DiagnosticIdPrefix.GetPrefix(f.Key.Id), f => f.ToImmutableArray());
        }

        private ImmutableArray<CodeFixProvider> LoadFixers()
        {
            if (!IncludeFixers)
                return ImmutableArray<CodeFixProvider>.Empty;

            return FixersByLanguage
               .SelectMany(f => f.Value)
               .Distinct()
               .ToImmutableArray();
        }

        private ImmutableArray<string> LoadFixableDiagnosticIds()
        {
            return Fixers
                .SelectMany(f => f.FixableDiagnosticIds)
                .Distinct()
                .OrderBy(f => f)
                .ToImmutableArray();
        }

        private ImmutableDictionary<string, ImmutableArray<string>> LoadFixableDiagnosticIdsByPrefix()
        {
            return FixableDiagnosticIds
                .GroupBy(f => f, DiagnosticIdComparer.Prefix)
                .ToImmutableDictionary(f => DiagnosticIdPrefix.GetPrefix(f.Key), f => f.ToImmutableArray());
        }

        public static AnalyzerAssembly Load(
            Assembly analyzerAssembly,
            bool loadAnalyzers = true,
            bool loadFixers = true,
            string language = null)
        {
            Debug.Assert(loadAnalyzers || loadFixers);

            Dictionary<string, ImmutableArray<DiagnosticAnalyzer>.Builder> analyzers = null;
            Dictionary<string, ImmutableArray<CodeFixProvider>.Builder> fixers = null;

            try
            {
                foreach (System.Reflection.TypeInfo typeInfo in analyzerAssembly.DefinedTypes)
                {
                    if (loadAnalyzers
                        && !typeInfo.IsAbstract
                        && typeInfo.IsSubclassOf(typeof(DiagnosticAnalyzer)))
                    {
                        DiagnosticAnalyzerAttribute attribute = typeInfo.GetCustomAttribute<DiagnosticAnalyzerAttribute>();

                        if (attribute != null)
                        {
                            var analyzer = (DiagnosticAnalyzer)Activator.CreateInstance(typeInfo.AsType());

                            if (analyzers == null)
                                analyzers = new Dictionary<string, ImmutableArray<DiagnosticAnalyzer>.Builder>();

                            foreach (string language2 in attribute.Languages)
                            {
                                if (language == null
                                    || language == language2)
                                {
                                    if (!analyzers.TryGetValue(language2, out ImmutableArray<DiagnosticAnalyzer>.Builder value))
                                        analyzers[language2] = ImmutableArray.CreateBuilder<DiagnosticAnalyzer>();

                                    analyzers[language2].Add(analyzer);
                                }
                            }
                        }
                    }
                    else if (loadFixers
                        && !typeInfo.IsAbstract
                        && typeInfo.IsSubclassOf(typeof(CodeFixProvider)))
                    {
                        ExportCodeFixProviderAttribute attribute = typeInfo.GetCustomAttribute<ExportCodeFixProviderAttribute>();

                        if (attribute != null)
                        {
                            var fixer = (CodeFixProvider)Activator.CreateInstance(typeInfo.AsType());

                            if (fixers == null)
                                fixers = new Dictionary<string, ImmutableArray<CodeFixProvider>.Builder>();

                            foreach (string language2 in attribute.Languages)
                            {
                                if (language == null
                                    || language == language2)
                                {
                                    if (!fixers.TryGetValue(language2, out ImmutableArray<CodeFixProvider>.Builder value))
                                        fixers[language2] = ImmutableArray.CreateBuilder<CodeFixProvider>();

                                    fixers[language2].Add(fixer);
                                }
                            }
                        }
                    }
                }
            }
            catch (ReflectionTypeLoadException)
            {
                WriteLine($"Cannot load types from assembly '{analyzerAssembly.FullName}'", ConsoleColor.DarkGray, Verbosity.Diagnostic);
            }

            return new AnalyzerAssembly(
                analyzerAssembly,
                analyzers?.ToImmutableDictionary(f => f.Key, f => f.Value.ToImmutableArray()) ?? ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>>.Empty,
                fixers?.ToImmutableDictionary(f => f.Key, f => f.Value.ToImmutableArray()) ?? ImmutableDictionary<string, ImmutableArray<CodeFixProvider>>.Empty,
                includeAnalyzers: loadAnalyzers,
                includeFixers: loadFixers);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(FullName);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as AnalyzerAssembly);
        }

        public bool Equals(AnalyzerAssembly other)
        {
            return other != null
                && StringComparer.Ordinal.Equals(FullName, other.FullName);
        }
    }
}
