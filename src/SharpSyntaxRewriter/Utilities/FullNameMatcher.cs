// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;

namespace SharpSyntaxRewriter.Utilities
{
    public class FullNameMatcher
    {
        private static bool CompareFullName_IgnoreGeneric(ISymbol sym, string[] names)
        {
            // From https://github.com/dotnet/roslyn/blob/b796152aff3a7f872bd70db26cc9f568bbdb14cc/src/Compilers/CSharp/Portable/Symbols/TypeSymbolExtensions.cs#L449

            for (int i = 0; i < names.Length; i++)
            {
                if (sym == null || sym.Name != names[i])
                    return false;
                sym = sym.ContainingSymbol;
            }
            return true;
        }

        public static bool MatchByFullName(ITypeSymbol tySym,
                                           string name,
                                           string[] nsNames)
        {
            // From https://github.com/dotnet/roslyn/blob/b796152aff3a7f872bd70db26cc9f568bbdb14cc/src/Compilers/CSharp/Portable/Symbols/TypeSymbolExtensions.cs#L398

            if (tySym.OriginalDefinition is INamedTypeSymbol namedTySym
                    && namedTySym.Name == name
                    && CompareFullName_IgnoreGeneric(namedTySym.ContainingSymbol, nsNames))
            {
                return true;
            }
            return false;
        }
    }
}
