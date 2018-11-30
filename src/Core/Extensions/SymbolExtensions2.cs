// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Roslynator
{
    //TODO: 
    internal static class SymbolExtensions2
    {
        //TODO: make public
        public static TSymbol FindMember<TSymbol>(
            this INamedTypeSymbol typeSymbol,
            Func<TSymbol, bool> predicate = null,
            bool includeBaseTypes = false) where TSymbol : ISymbol
        {
            if (typeSymbol == null)
                throw new ArgumentNullException(nameof(typeSymbol));

            return FindMemberImpl(typeSymbol, name: null, predicate, includeBaseTypes);
        }

        //TODO: make public
        public static TSymbol FindMember<TSymbol>(
            this INamedTypeSymbol typeSymbol,
            string name,
            Func<TSymbol, bool> predicate = null,
            bool includeBaseTypes = false) where TSymbol : ISymbol
        {
            if (typeSymbol == null)
                throw new ArgumentNullException(nameof(typeSymbol));

            return FindMemberImpl(typeSymbol, name, predicate, includeBaseTypes);
        }

        private static TSymbol FindMemberImpl<TSymbol>(
            this INamedTypeSymbol typeSymbol,
            string name,
            Func<TSymbol, bool> predicate = null,
            bool includeBaseTypes = false) where TSymbol : ISymbol
        {
            ImmutableArray<INamedTypeSymbol> members;

            do
            {
                members = (name != null)
                    ? typeSymbol.GetTypeMembers(name)
                    : typeSymbol.GetTypeMembers();

                TSymbol symbol = Roslynator.SymbolExtensions.FindMemberImpl(members, predicate);

                if (symbol != null)
                    return symbol;

                if (!includeBaseTypes)
                    break;

                typeSymbol = typeSymbol.BaseType;
            }
            while (typeSymbol != null);

            return default;
        }

        //TODO: make public
        internal static INamedTypeSymbol FindTypeMember(
            this INamedTypeSymbol typeSymbol,
            Func<INamedTypeSymbol, bool> predicate = null,
            bool includeBaseTypes = false)
        {
            if (typeSymbol == null)
                throw new ArgumentNullException(nameof(typeSymbol));

            return FindTypeMemberImpl(typeSymbol, name: null, arity: null, predicate, includeBaseTypes);
        }

        //TODO: make public
        internal static INamedTypeSymbol FindTypeMember(
            this INamedTypeSymbol typeSymbol,
            string name,
            Func<INamedTypeSymbol, bool> predicate = null,
            bool includeBaseTypes = false)
        {
            if (typeSymbol == null)
                throw new ArgumentNullException(nameof(typeSymbol));

            if (name == null)
                throw new ArgumentNullException(nameof(name));

            return FindTypeMemberImpl(typeSymbol, name, arity: null, predicate, includeBaseTypes);
        }

        internal static INamedTypeSymbol FindTypeMember(
            this INamedTypeSymbol typeSymbol,
            string name,
            int  arity,
            Func<INamedTypeSymbol, bool> predicate = null,
            bool includeBaseTypes = false)
        {
            if (typeSymbol == null)
                throw new ArgumentNullException(nameof(typeSymbol));

            if (name == null)
                throw new ArgumentNullException(nameof(name));

            return FindTypeMemberImpl(typeSymbol, name, arity, predicate, includeBaseTypes);
        }

        private static INamedTypeSymbol FindTypeMemberImpl(
            this INamedTypeSymbol typeSymbol,
            string name,
            int? arity,
            Func<INamedTypeSymbol, bool> predicate = null,
            bool includeBaseTypes = false)
        {
            ImmutableArray<INamedTypeSymbol> members;

            do
            {
                if (name != null)
                {
                    if (arity != null)
                    {
                        members = typeSymbol.GetTypeMembers(name, arity.Value);
                    }
                    else
                    {
                        members = typeSymbol.GetTypeMembers(name);
                    }
                }
                else
                {
                    members = typeSymbol.GetTypeMembers();
                }

                INamedTypeSymbol symbol = Roslynator.SymbolExtensions.FindMemberImpl(members, predicate);

                if (symbol != null)
                    return symbol;

                if (!includeBaseTypes)
                    break;

                typeSymbol = typeSymbol.BaseType;
            }
            while (typeSymbol != null);

            return null;
        }
    }
}
