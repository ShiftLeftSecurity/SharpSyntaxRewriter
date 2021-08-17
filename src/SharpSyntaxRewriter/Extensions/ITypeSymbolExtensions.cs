// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;

using SharpSyntaxRewriter.Constants;

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

        private static bool CheckFullName(ISymbol sym, string[] names)
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

        private static bool IsTypeByFullName(ITypeSymbol tySym,
                                             string name,
                                             string[] nsNames)
        {
            // From https://github.com/dotnet/roslyn/blob/b796152aff3a7f872bd70db26cc9f568bbdb14cc/src/Compilers/CSharp/Portable/Symbols/TypeSymbolExtensions.cs#L398

            if (tySym.OriginalDefinition is INamedTypeSymbol namedTySym
                    && namedTySym.Name == name
                    && CheckFullName(namedTySym.ContainingSymbol, nsNames))
            {
                return true;
            }
            return false;
        }

        private static readonly string[] __names_SystemLinqExpressions = {
                "Expressions",
                "Linq",
                Namespace.SYSTEM };

        public static bool IsExpressionTree(this ITypeSymbol tySym)
        {
            return IsTypeByFullName(tySym, "Expression", __names_SystemLinqExpressions);
        }

        private static readonly string[] __names_SystemThreadingTasks = {
                "Tasks",
                "Threading",
                Namespace.SYSTEM };

        public static bool IsTask(this ITypeSymbol tySym)
        {
            return IsTypeByFullName(tySym, "Task", __names_SystemThreadingTasks);
        }
    }
}
