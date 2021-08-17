// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;
using System.Linq;

namespace SharpSyntaxRewriter.Extensions
{
    public enum ResolutionAccuracy
    {
        Exact,
        Transitive
    }

    public static class ExpressionSyntaxExtensions
    {
        public static ExpressionSyntax Stripped(this ExpressionSyntax exprNode)
        {
            return exprNode switch
            {
                ParenthesizedExpressionSyntax encExpr => Stripped(encExpr.Expression),
                AwaitExpressionSyntax awaitExpr => Stripped(awaitExpr.Expression),
                CheckedExpressionSyntax checkExpr => Stripped(checkExpr.Expression),
                RefExpressionSyntax refExpr => Stripped(refExpr.Expression),
                ThrowExpressionSyntax throwExpr => Stripped(throwExpr.Expression),
                _ => exprNode,
            };
        }

        public static ITypeSymbol ResolveType(this ExpressionSyntax exprNode,
                                              SemanticModel semaModel)
        {
            Debug.Assert(semaModel != null);

            var tySym = semaModel.GetTypeInfo(exprNode).ConvertedType;

            if (tySym is ITypeParameterSymbol tyParmSym
                    && exprNode is SimpleNameSyntax nameNode)
            {
                return tyParmSym.SpecializedFor(nameNode);
            }
            return tySym;
        }

        public static ISymbol ResolveSymbol(this ExpressionSyntax exprNode,
                                            SemanticModel semaModel,
                                            ResolutionAccuracy accuracy)
        {
            Debug.Assert(semaModel != null);

            var sym = semaModel.GetSymbolInfo(exprNode).Symbol;
            if (sym != null)
                return sym;

            sym = semaModel.GetMemberGroup(exprNode).FirstOrDefault();
            if (sym != null)
                return sym;

            // In case one is wondering... `GetIndexerGroup' isn't
            // to be considered here (that API may be misleading):
            // https://github.com/dotnet/roslyn/issues/44719.

            var op = semaModel.GetOperation(exprNode);
            if (op == null)
                return null;

            return accuracy == ResolutionAccuracy.Exact
                    ? op.ImmediateTargetSymbol()
                    : op.TargetSymbol();
        }
    }
}
