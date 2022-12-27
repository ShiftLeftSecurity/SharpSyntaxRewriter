// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;
using System.Linq;

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

        public static ITypeSymbol NonNullableType(this ITypeSymbol tySym)
        {
            if (tySym is INamedTypeSymbol namedTySym
                    && namedTySym.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
                    && namedTySym.TypeArguments.Any())
            {
                return namedTySym.TypeArguments[0];
            }

            if (tySym.IsReferenceType)
                return tySym.WithNullableAnnotation(NullableAnnotation.NotAnnotated);

            return tySym;
        }

        public static ITypeSymbol UnderlyingNonNullableType(this ITypeSymbol tySym)
        {
            return tySym.UnderlyingType().NonNullableType();
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
