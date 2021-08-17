// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SharpSyntaxRewriter.Extensions
{
    public static class AccessibilityExtensions
    {
        public static SyntaxToken ToRewriteToken(this Accessibility access)
        {
            switch (access)
            {
                case Accessibility.Private:
                    return SyntaxFactory.Token(SyntaxKind.PrivateKeyword);

                case Accessibility.Protected:
                    return SyntaxFactory.Token(SyntaxKind.ProtectedKeyword);

                case Accessibility.ProtectedAndInternal:
                case Accessibility.ProtectedOrInternal:
                case Accessibility.Internal:
                case Accessibility.Public:
                case Accessibility.NotApplicable:
                default:
                    return SyntaxFactory.Token(SyntaxKind.InternalKeyword);
            }
        }
    }
}
