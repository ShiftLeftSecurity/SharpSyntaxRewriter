// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;

using SharpSyntaxRewriter.Constants;
using SharpSyntaxRewriter.Rewriters.Types;

namespace SharpSyntaxRewriter.Rewriters
{
    public class ImposeThisPrefix : SymbolicRewriter
    {
        public override string Name()
        {
            return "<impose `this' prefix>";
        }

        public override SyntaxNode VisitNameColon(NameColonSyntax node)
        {
            return node;
        }

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            return node.WithBody((BlockSyntax)node.Body.Accept(this));
        }

        public override SyntaxNode VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            return node.WithInitializer(
                (EqualsValueClauseSyntax)node.Initializer?.Accept(this));
        }

        public override SyntaxNode VisitInitializerExpression(InitializerExpressionSyntax node)
        {
            if (node.Kind() != SyntaxKind.ObjectInitializerExpression
                    && node.Kind() != SyntaxKind.WithInitializerExpression)
            {
                return base.VisitInitializerExpression(node);
            }

            var nodeExprs_P = SyntaxFactory.SeparatedList<ExpressionSyntax>();
            var nodeExprs = node.Expressions;
            var i = 0;
            for (; i < nodeExprs.Count; ++i)
            {
                var expr = nodeExprs[i];
                ExpressionSyntax expr_P;
                if (expr is AssignmentExpressionSyntax assgExpr)
                {
                    var rhsExpr_P = (ExpressionSyntax)
                        assgExpr.Right
                                .Accept(this)
                                .WithTriviaFrom(assgExpr.Right);
                    expr_P = assgExpr.WithRight(rhsExpr_P).WithTriviaFrom(assgExpr);
                }
                else
                {
                    expr_P = (ExpressionSyntax)
                        expr.Accept(this).WithTriviaFrom(expr);
                }

                nodeExprs_P = nodeExprs_P.Add(expr_P);

                if (i > 0)
                {
                    var commaTk = nodeExprs.GetSeparator(i - 1);
                    nodeExprs_P =
                        nodeExprs_P.ReplaceSeparator(nodeExprs_P.GetSeparator(i - 1),
                                                     commaTk);
                }
            }

            if (i > 0 && nodeExprs.SeparatorCount == nodeExprs.Count)
            {
                var commaTk = nodeExprs.GetSeparators().Last();
                node = node.WithCloseBraceToken(
                                node.CloseBraceToken
                                    .WithLeadingTrivia(
                                        commaTk.LeadingTrivia
                                               .AddRange(commaTk.TrailingTrivia)
                                               .AddRange(node.CloseBraceToken.LeadingTrivia)));
            }

            return node.WithExpressions(nodeExprs_P);
        }

        public override SyntaxNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            return node.ToString()
                       .StartsWith(Keyword.THIS, StringComparison.Ordinal)
                ? node
                : node.WithExpression((ExpressionSyntax)node.Expression.Accept(this));
        }

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            return VisitSimpleNameCommon(node);
        }

        public override SyntaxNode VisitGenericName(GenericNameSyntax node)
        {
            return VisitSimpleNameCommon(node);
        }

        private SyntaxNode VisitSimpleNameCommon(SimpleNameSyntax node)
        {
            var blockNodes = node.Ancestors().OfType<BlockSyntax>();
            if (!blockNodes.Any())
                return node;

            var sym = _semaModel.GetSymbolInfo(node).Symbol;
            if (sym == null || sym.IsStatic)
                return node;

            switch (sym)
            {
                case IFieldSymbol fldSym:
                    if (fldSym.CorrespondingTupleField != null)
                        return node;
                    break;

                case IPropertySymbol:
                    break;

                case IMethodSymbol methSym:
                    if (methSym.MethodKind == MethodKind.LocalFunction)
                        return node;
                    break;

                default:
                    return node;
            }

            return SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.ThisExpression(),
                            SyntaxFactory.Token(SyntaxKind.DotToken),
                            node.WithoutLeadingTrivia())
                        .WithTriviaFrom(node);
        }

        public override SyntaxNode VisitQueryExpression(QueryExpressionSyntax node)
        {
            return node;
        }
    }
}
