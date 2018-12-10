// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;

namespace Roslynator.Mef
{
    internal class LanguageServices
    {
        private static LanguageServices _default;
        private readonly ImmutableDictionary<string, ImmutableArray<Lazy<ILanguageService, LanguageServiceMetadata>>> _services;

        private ImmutableDictionary<string, ImmutableDictionary<Type, Lazy<ILanguageService, LanguageServiceMetadata>>> _servicesMap
            = ImmutableDictionary<string, ImmutableDictionary<Type, Lazy<ILanguageService, LanguageServiceMetadata>>>.Empty;

        public LanguageServices(MefServices mefServices)
        {
            _services = mefServices
                .GetExports<ILanguageService, LanguageServiceMetadata>()
                .GroupBy(f => f.Metadata.Language)
                .ToImmutableDictionary(f => f.Key, f => f.ToImmutableArray());
        }

        public static LanguageServices Default
        {
            get
            {
                if (_default == null)
                {
                    var services = new LanguageServices(MefServices.Default);
                    Interlocked.CompareExchange(ref _default, services, null);
                }

                return _default;
            }
        }

        public TLanguageService GetService<TLanguageService>(string language)
        {
            if (!_services.TryGetValue(language, out ImmutableArray<Lazy<ILanguageService, LanguageServiceMetadata>> languageServices))
            {
                throw new NotSupportedException($"Language '{language}' is not supported.");
            }

            if (!_servicesMap.TryGetValue(language, out ImmutableDictionary<Type, Lazy<ILanguageService, LanguageServiceMetadata>> map))
            {
                map = ImmutableInterlocked.GetOrAdd(ref _servicesMap, language, ImmutableDictionary<Type, Lazy<ILanguageService, LanguageServiceMetadata>>.Empty);
            }

            Type serviceType = typeof(TLanguageService);

            Lazy<ILanguageService, LanguageServiceMetadata> service = ImmutableInterlocked.GetOrAdd(ref map, serviceType, st =>
            {
                //TODO: AssemblyQualifiedName
                string fullName = st.FullName;
                return languageServices.SingleOrDefault(f => f.Metadata.ServiceType == fullName, shouldThrow: false);
            });

            return (TLanguageService)service?.Value;
        }

        public bool IsSupportedLangugae(string language)
        {
            return _services.ContainsKey(language);
        }

        //TODO: IsWellKnownLanguage?
        public static bool IsWellKnownLanguage(string language)
        {
            switch (language)
            {
                case LanguageNames.CSharp:
                case LanguageNames.VisualBasic:
                    return true;
                default:
                    return false;
            }
        }
    }
}