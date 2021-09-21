// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;

using SharpSyntaxRewriter.Constants;
using SharpSyntaxRewriter.Utilities;

namespace SharpSyntaxRewriter.Extensions
{
    public static class INamedTypeSymbolExtensions
    {
        private static readonly string[] __names_SystemThreadingTasks = {
                "Tasks",
                "Threading",
                Namespace.SYSTEM };

        public static bool IsVoidTask(this INamedTypeSymbol namedTySym)
        {
            return !namedTySym.IsGenericType
                       & IsTask(namedTySym);
        }

        public static bool IsTask(this INamedTypeSymbol namedTySym)
        {
            return FullNameMatcher.MatchByFullName(
                namedTySym,
                "Task",
                __names_SystemThreadingTasks);
        }
    }
}
