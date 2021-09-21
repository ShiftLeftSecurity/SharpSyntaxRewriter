// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

using SharpSyntaxRewriter.Extensions;

namespace SharpSyntaxRewriter.Utilities
{
    public static class ReturnTypeInfo
    {
        /*
         * Return whether the type symbol, when used as a return type,
         * "implies" a void result (an absent value).
         *
         * https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/classes#method-body
         * https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/statements#the-return-statement
         */
        public static bool ImpliesVoid(ITypeSymbol retTySym, bool isAsync)
        {
            if (retTySym is INamedTypeSymbol namedTySym)
            {
                return namedTySym.SpecialType == SpecialType.System_Void
                        || (isAsync && namedTySym.IsVoidTask());
            }
            return false;
        }

        /*
         * Return whether the type syntax, when used as a return type,
         * "implies" a void result (an absent value).
         * If a semantic model is provided, symbolic data will be used
         * to decide; otherwise, only syntax.
         *
         * https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/classes#method-body
         * https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/statements#the-return-statement
         */
        public static bool ImpliesVoid(TypeSyntax retTySpec,
                                       bool isAsync,
                                       SemanticModel model = null)
        {
            if (retTySpec == null)
                return true;

            if (model != null)
            {
                var tySym = model.GetTypeInfo(retTySpec).ConvertedType;
                return ImpliesVoid(tySym, isAsync);
            }

            if (retTySpec is PredefinedTypeSyntax predTySpec)
                return predTySpec.Keyword.IsKind(SyntaxKind.VoidKeyword);

            if (retTySpec is NameSyntax namedTySpec)
            {
                var namedTy = namedTySpec.ToString();
                return isAsync && (namedTy == "System.Threading.Tasks.Task"
                                    || namedTy == "Threading.Tasks.Task"
                                    || namedTy == "Tasks.Task"
                                    || namedTy == "Task");
            }

            return false;
        }
    }
}
