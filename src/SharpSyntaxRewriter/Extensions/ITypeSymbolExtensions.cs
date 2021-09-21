// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;

using SharpSyntaxRewriter.Constants;
using SharpSyntaxRewriter.Utilities;

namespace SharpSyntaxRewriter.Extensions
{
    public static class ITypeSymbolExtensions
    {
        public static ITypeSymbol UnderlyingType(this ITypeSymbol tySym)
        {
            if (tySym is IArrayTypeSymbol arrTySym)
                return UnderlyingType(arrTySym.ElementType);

            if (tySym is IPointerTypeSymbol ptrTySym)
                return UnderlyingType(ptrTySym.PointedAtType);

            return tySym;
        }

        private static readonly string[] __names_SystemLinqExpressions = {
                "Expressions",
                "Linq",
                Namespace.SYSTEM };

        public static bool IsExpressionTree(this ITypeSymbol tySym)
        {
            return FullNameMatcher.MatchByFullName(tySym, "Expression", __names_SystemLinqExpressions);
        }
    }
}
