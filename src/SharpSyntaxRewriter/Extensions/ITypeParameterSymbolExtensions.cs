// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;
using System.Linq;

namespace SharpSyntaxRewriter.Extensions
{
    public static class ITypeParameterSymbolExtensions
    {
        public static ITypeSymbol SpecializedFor(this ITypeParameterSymbol tyParmSym,
                                                 SimpleNameSyntax nameNode)
        {
            Debug.Assert(nameNode != null);

            if (nameNode.Parent is not MemberAccessExpressionSyntax membAccsNode)
                return tyParmSym;

            // Try to match the identifier in the member access expression
            // with the name of a member from any of the constrained types.

            var membName = membAccsNode.Name.Identifier.Text;
            foreach (var tySym in tyParmSym.ConstraintTypes)
            {
                if (tySym.GetMembers().Any((sym) => sym.Name == membName))
                    return tySym;
            }

            return tyParmSym;
        }
    }
}
