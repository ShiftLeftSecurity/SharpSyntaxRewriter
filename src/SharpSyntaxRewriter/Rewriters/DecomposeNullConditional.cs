// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using SharpSyntaxRewriter.Extensions;
using SharpSyntaxRewriter.Rewriters.Types;
using SharpSyntaxRewriter.Utilities;

namespace SharpSyntaxRewriter.Rewriters
{
    public class DecomposeNullConditional : SymbolicRewriter
    {
        public const string ID = "<decompose `null' conditional>";

        public override string Name()
        {
            return ID;
        }

        private ExpressionSyntax JoinExpressions(ExpressionSyntax prefixExpr,
                                                 ExpressionSyntax suffixExpr)
        {
            if (prefixExpr == null)
                return suffixExpr;

            ExpressionSyntax expr;
            switch (suffixExpr)
            {
                case MemberBindingExpressionSyntax membBindExpr:
                    expr =
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            prefixExpr,
                            membBindExpr.Name);
                    break;

                case ElementBindingExpressionSyntax elemBindExpr:
                    expr =
                        SyntaxFactory.ElementAccessExpression(
                            prefixExpr,
                            elemBindExpr.ArgumentList);
                    break;

                case MemberAccessExpressionSyntax membAccExpr:
                    membAccExpr = membAccExpr.WithExpression(
                        (ExpressionSyntax)membAccExpr.Expression.Accept(this));
                    prefixExpr = JoinExpressions(prefixExpr, membAccExpr.Expression);
                    expr =
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            prefixExpr,
                            membAccExpr.Name);
                    break;

                case ElementAccessExpressionSyntax elemAccExpr:
                    elemAccExpr = elemAccExpr.WithArgumentList(
                        (BracketedArgumentListSyntax)elemAccExpr.ArgumentList.Accept(this));
                    prefixExpr = JoinExpressions(prefixExpr, elemAccExpr.Expression);
                    expr =
                        SyntaxFactory.ElementAccessExpression(
                            prefixExpr,
                            elemAccExpr.ArgumentList);
                    break;

                case InvocationExpressionSyntax callExpr:
                    callExpr = callExpr.WithArgumentList(
                        (ArgumentListSyntax)callExpr.ArgumentList.Accept(this));
                    prefixExpr = JoinExpressions(prefixExpr, callExpr.Expression);
                    expr =
                        SyntaxFactory.InvocationExpression(
                            prefixExpr,
                            callExpr.ArgumentList);
                    break;

                default:
                    Debug.Fail($"prefix:{prefixExpr} suffix:{suffixExpr}");
                    return null;
            }

            return expr;
        }

        private static ExpressionSyntax SynthesizeComparisonExpression(SyntaxKind kind, ExpressionSyntax expr)
        {
            return SyntaxFactory.BinaryExpression(
                    kind,
                    SyntaxFactory.CastExpression(
                        SyntaxFactory.PredefinedType(
                            SyntaxFactory.Token(SyntaxKind.ObjectKeyword)),
                        expr.WithoutTrivia()),
                    SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));
        }

        private Stack<ExpressionSyntax> GatherPrefixes(__DecompositionInfo ctx)
        {
            var prefixExprs = new Stack<ExpressionSyntax>();

            ExpressionSyntax fullPrefixExpr = null;
            foreach (var p in ctx.Prefixes.Reverse())
            {
                fullPrefixExpr = JoinExpressions(fullPrefixExpr, p);
                prefixExprs.Push(fullPrefixExpr);
            }

            return prefixExprs;
        }

        private ExpressionSyntax DecomposeIntoExpression(__DecompositionInfo ctx, ExpressionSyntax expr)
        {
            var prefixExprs = GatherPrefixes(ctx);

            var prefixExpr = prefixExprs.Pop();
            var condExpr =
                SyntaxFactory.ConditionalExpression(
                    SyntaxFactory.ParenthesizedExpression(
                        SynthesizeComparisonExpression(SyntaxKind.EqualsExpression, prefixExpr)),
                    ctx.WhenNull.Pop(),
                    expr);

            while (prefixExprs.Any())
            {
                prefixExpr = prefixExprs.Pop();
                condExpr =
                    SyntaxFactory.ConditionalExpression(
                    SyntaxFactory.ParenthesizedExpression(
                        SynthesizeComparisonExpression(SyntaxKind.EqualsExpression, prefixExpr)),
                    ctx.WhenNull.Pop(),
                    condExpr);
            }

            return condExpr;
        }

        private StatementSyntax DecomposeIntoStatement(__DecompositionInfo ctx, ExpressionSyntax expr)
        {
            var prefixExprs = GatherPrefixes(ctx);

            var prefixExpr = prefixExprs.Pop();
            var ifStmt =
                SyntaxFactory.IfStatement(
                    SynthesizeComparisonExpression(SyntaxKind.NotEqualsExpression, prefixExpr),
                    SyntaxFactory.ExpressionStatement(expr));

            while (prefixExprs.Any())
            {
                prefixExpr = prefixExprs.Pop();
                ifStmt =
                    SyntaxFactory.IfStatement(
                        SynthesizeComparisonExpression(SyntaxKind.NotEqualsExpression, prefixExpr),
                        ifStmt);
            }

            return ifStmt;
        }

        private class __DecompositionInfo
        {
            public Stack<ExpressionSyntax> Prefixes { get; set; } = new();

            public Stack<ExpressionSyntax> WhenNull { get; set; } = new();
        }

        private readonly Stack<__DecompositionInfo> __ctx = new();

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            if (node.ExpressionBody is not null)
            {
                return node.WithExpressionBody((ArrowExpressionClauseSyntax)node.ExpressionBody.Accept(this));
            }

            return node.WithBody((BlockSyntax)node.Body.Accept(this));
        }

        public override SyntaxNode VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node)
        {
            var prefixTySym = _semaModel.GetTypeInfo(node.Expression).ConvertedType;
            if (!ValidateSymbol(prefixTySym))
                return node;

            var exprTySym = _semaModel.GetTypeInfo(node.WhenNotNull).ConvertedType;
            if (!ValidateSymbol(exprTySym))
                return node;

            var prefixExpr_P = (ExpressionSyntax)node.Expression.Accept(this);
            prefixExpr_P =
                prefixExpr_P.WithoutTrivia()
                          .WithTrailingTrivia(
                            node.Expression
                                .GetTrailingTrivia()
                                    .AddRange(node.OperatorToken.LeadingTrivia)
                                    .AddRange(node.OperatorToken.TrailingTrivia));

            if (prefixTySym.IsValueType)
            {
                prefixExpr_P =
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        prefixExpr_P,
                        SyntaxFactory.IdentifierName("Value"));
            }

            __ctx.Peek().Prefixes.Push(prefixExpr_P);

            var suffixExpr_P = (ExpressionSyntax)node.WhenNotNull.Accept(this);

            var node_P = JoinExpressions(prefixExpr_P, suffixExpr_P);

            ExpressionSyntax falseExpr =
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.NullLiteralExpression);

            if (exprTySym.IsValueType)
            {
                if (exprTySym.OriginalDefinition != null
                        && exprTySym.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                {
                    exprTySym = ((INamedTypeSymbol)exprTySym).TypeArguments[0];
                }

                falseExpr =
                    SyntaxFactory.CastExpression(
                        SyntaxFactory.NullableType(
                            SyntaxFactory.ParseTypeName(
                                exprTySym.ToMinimalDisplayString(_semaModel, node.SpanStart))),
                        falseExpr);
            }

            __ctx.Peek().WhenNull.Push(falseExpr);

            return node_P;
        }

        public override SyntaxNode Visit(SyntaxNode node)
        {
            bool decomp;
            if (node != null
                    && node.IsKind(SyntaxKind.ConditionalAccessExpression)
                    && !(node.Parent.IsKind(SyntaxKind.ConditionalAccessExpression)
                        || node.Parent.IsKind(SyntaxKind.ExpressionStatement)
                        || node.Parent.IsKind(SyntaxKind.ParenthesizedLambdaExpression)
                        || node.Parent.IsKind(SyntaxKind.SimpleLambdaExpression)))
            {
                decomp = true;
                __ctx.Push(new __DecompositionInfo());
            }
            else
                decomp = false;

            var node_P = base.Visit(node);

            if (decomp && !node.Parent.IsKind(SyntaxKind.ExpressionStatement))
            {
                return DecomposeIntoExpression(__ctx.Pop(), (ExpressionSyntax)node_P);
            }

            return node_P;
        }

        public override SyntaxNode VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            bool decomp;
            if (node.Expression.Kind() == SyntaxKind.ConditionalAccessExpression)
            {
                decomp = true;
                __ctx.Push(new __DecompositionInfo());
            }
            else
                decomp = false;

            var node_P = (ExpressionStatementSyntax)base.VisitExpressionStatement(node);

            if (decomp)
            {
                var stmt = DecomposeIntoStatement(__ctx.Pop(), node_P.Expression);
                return stmt.WithTriviaFrom(node);
            }

            return node_P;
        }

        public SyntaxNode VisitLambdaExpressionCommon(LambdaExpressionSyntax node)
        {
            bool decomp;
            if (node.Body.Kind() == SyntaxKind.ConditionalAccessExpression)
            {
                decomp = true;
                __ctx.Push(new __DecompositionInfo());
            }
            else
                decomp = false;

            var node_P = node.WithBody((CSharpSyntaxNode)node.Body.Accept(this));

            if (decomp)
            {
                var sym = node.ResolvedSymbol(_semaModel,
                                              ResolutionAccuracy.Approximate);
                if (ValidateSymbol(sym)
                        && sym is IMethodSymbol methSym
                        && ReturnTypeInfo.ImpliesVoid(methSym.ReturnType, methSym.IsAsync))
                {
                    var stmt = DecomposeIntoStatement(__ctx.Pop(), (ExpressionSyntax)node_P.Body);
                    return node_P.WithBody(SyntaxFactory.Block(stmt));
                }

                var expr = DecomposeIntoExpression(__ctx.Pop(), (ExpressionSyntax)node_P.Body);
                return node_P.WithBody(expr);
            }

            return node_P;
        }

        public override SyntaxNode VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
        {
            return VisitLambdaExpressionCommon(node);
        }

        public override SyntaxNode VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
        {
            return VisitLambdaExpressionCommon(node);
        }

        public override SyntaxNode VisitQueryExpression(QueryExpressionSyntax node)
        {
            return node;
        }
    }
}
