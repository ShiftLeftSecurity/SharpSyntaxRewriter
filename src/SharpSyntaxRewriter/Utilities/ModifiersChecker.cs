// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;

namespace SharpSyntaxRewriter.Utilities
{
    public static class ModifiersChecker
    {
        private static bool HasByKind(SyntaxTokenList modifiers, SyntaxKind kind)
        {
            return modifiers.Any(m => m.IsKind(kind));
        }

        public static bool Has_async(SyntaxTokenList modifiers)
        {
            return HasByKind(modifiers, SyntaxKind.AsyncKeyword);
        }

        public static bool Has_static(SyntaxTokenList modifiers)
        {
            return HasByKind(modifiers, SyntaxKind.StaticKeyword);
        }

        public static bool Has_partial(SyntaxTokenList modifiers)
        {
            return HasByKind(modifiers, SyntaxKind.PartialKeyword);
        }

        public static bool Has_abstract(SyntaxTokenList modifiers)
        {
            return HasByKind(modifiers, SyntaxKind.AbstractKeyword);
        }

        public static bool Has_readonly(SyntaxTokenList modifiers)
        {
            return HasByKind(modifiers, SyntaxKind.ReadOnlyKeyword);
        }
    }
}
