// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;
using System.Diagnostics;

namespace SharpSyntaxRewriter.Extensions
{
    public static class ISymbolExtensions
    {
        public static string ContextualDisplay(this ISymbol sym,
                                               int spanPos,
                                               SemanticModel semaModel)
        {
            return sym.ToMinimalDisplayString(
                    semaModel,
                    spanPos,
                    SymbolDisplayFormat
                        .MinimallyQualifiedFormat
                        .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Included));
        }

        public static string CanonicalDisplay(this ISymbol sym)
        {
            var fmt =
                new SymbolDisplayFormat(
                    typeQualificationStyle:
                        SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                    memberOptions:
                        SymbolDisplayMemberOptions.IncludeContainingType |
                        SymbolDisplayMemberOptions.IncludeExplicitInterface,
                    parameterOptions:
                        SymbolDisplayParameterOptions.IncludeType |
                        SymbolDisplayParameterOptions.IncludeOptionalBrackets |
                        SymbolDisplayParameterOptions.IncludeExtensionThis,
                    genericsOptions:
                        SymbolDisplayGenericsOptions.IncludeTypeParameters,
                    extensionMethodStyle:
                        SymbolDisplayExtensionMethodStyle.StaticMethod
                );

            return sym.ToDisplayString(fmt);
        }

        public static ITypeSymbol ValueType(this ISymbol sym)
        {
            switch (sym)
            {
                case IDiscardSymbol anySym:
                    return anySym.Type;

                case IEventSymbol evtSym:
                    return evtSym.Type;

                case IFieldSymbol fldSym:
                    return fldSym.Type;

                case ILocalSymbol varSym:
                    return varSym.Type;

                case IMethodSymbol methSym:
                    return methSym.ReturnType;

                case IParameterSymbol parmSym:
                    return parmSym.Type;

                case IPropertySymbol propSym:
                    return propSym.Type;

                default:
                    Debug.Fail($"unhandled symbol: {sym}");
                    return null;
            }
        }
    }
}
