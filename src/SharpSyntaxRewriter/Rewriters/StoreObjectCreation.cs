// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

using SharpSyntaxRewriter.Extensions;
using SharpSyntaxRewriter.Rewriters.Types;

namespace SharpSyntaxRewriter.Rewriters
{
    public class StoreObjectCreation : StatementSynthesizerRewriter
    {
        public override string Name()
        {
            return "<store object creation>";
        }

        private SyntaxNode VisitCreationExpression<ExpressionT>(
                ExpressionT node,
                Func<ExpressionT, SyntaxNode> visit,
                TypeSyntax storeTySpec)
            where ExpressionT : ExpressionSyntax
        {
            var node_P = (ExpressionSyntax)visit(node);

            if (!_ctx.Any())
                return node_P;

            // If we're within a declarator, then the variable being declared
            // is a store.
            if (node.Parent.Parent is VariableDeclaratorSyntax)
                return node_P;

            // If we're within an assignment to a local or parameter, then
            // then either of them is a store.
            if (node.Parent is AssignmentExpressionSyntax assgExpr
                    && !(node.Parent.Parent is InitializerExpressionSyntax))
            {
                var sym = assgExpr.Left.ResolveSymbol(_semaModel,
                                                      ResolutionAccuracy.Exact);
                if (sym is ILocalSymbol || sym is IParameterSymbol)
                    return node_P;
            }

            var storeName = FreshName("____obj");
            var storeDecl =
                SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(
                        storeTySpec.WithTrailingTrivia(),
                        SyntaxFactory.SeparatedList(
                            new List<VariableDeclaratorSyntax> {
                                SyntaxFactory.VariableDeclarator(
                                    SyntaxFactory.Identifier(storeName),
                                    null,
                                    SyntaxFactory.EqualsValueClause(
                                        node_P.WithLeadingTrivia()
                                             .WithTrailingTrivia())) })));

            _ctx.Peek().Add(storeDecl.WithLeadingTrivia()
                                     .WithTrailingTrivia());

            // It'd be easier if C# accepted expression statements of arbitrary
            // kind (like C and C++), but it doesn't... we return the original
            // node and ignore it upon visit.
            if (node.Parent is ExpressionStatementSyntax)
                return node;

            return SyntaxFactory.IdentifierName(storeName)
                        .WithLeadingTrivia(node_P.GetLeadingTrivia())
                        .WithTrailingTrivia(node_P.GetTrailingTrivia());
        }

        public override SyntaxNode VisitSwitchExpression(SwitchExpressionSyntax node)
        {
            var tySym = node.ResolveType(_semaModel);

            return VisitCreationExpression(
                        node,
                        base.VisitSwitchExpression,
                        SyntaxFactory.ParseTypeName(
                            tySym.ToMinimalDisplayString(_semaModel, node.SpanStart)));
        }

        public override SyntaxNode VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            return VisitCreationExpression(
                        node,
                        base.VisitObjectCreationExpression,
                        node.Type);
        }

        public override SyntaxNode VisitArrayCreationExpression(ArrayCreationExpressionSyntax node)
        {
            return VisitCreationExpression(
                        node,
                        base.VisitArrayCreationExpression,
                        ArrayTypeSpecWithoutDimension(node.Type));
        }

        public override SyntaxNode VisitStackAllocArrayCreationExpression(StackAllocArrayCreationExpressionSyntax node)
        {
            var tySym = _semaModel.GetTypeInfo(node).Type;

            return VisitCreationExpression(
                        node,
                        base.VisitStackAllocArrayCreationExpression,
                        SyntaxFactory.ParseTypeName(
                            tySym.ToMinimalDisplayString(_semaModel, node.SpanStart)));
        }

        public override SyntaxNode VisitImplicitObjectCreationExpression(ImplicitObjectCreationExpressionSyntax node)
        {
            var tySym = _semaModel.GetTypeInfo(node).Type;

            return VisitCreationExpression(
                        node,
                        base.VisitImplicitObjectCreationExpression,
                        SyntaxFactory.ParseTypeName(
                            tySym.ToMinimalDisplayString(_semaModel, node.SpanStart)));
        }

        public override SyntaxNode VisitImplicitArrayCreationExpression(ImplicitArrayCreationExpressionSyntax node)
        {
            var tySym = _semaModel.GetTypeInfo(node).Type;

            var tySpec = (ArrayTypeSyntax)
                SyntaxFactory.ParseTypeName(
                    tySym.ToMinimalDisplayString(_semaModel, node.SpanStart));

            return VisitCreationExpression(
                        node,
                        base.VisitImplicitArrayCreationExpression,
                        ArrayTypeSpecWithoutDimension(tySpec));
        }

        public override SyntaxNode VisitImplicitStackAllocArrayCreationExpression(ImplicitStackAllocArrayCreationExpressionSyntax node)
        {
            var tySym = _semaModel.GetTypeInfo(node).Type;
            if (tySym == null)
            {
                return base.VisitImplicitStackAllocArrayCreationExpression(node);
            }

            return VisitCreationExpression(
                        node,
                        base.VisitImplicitStackAllocArrayCreationExpression,
                        SyntaxFactory.ParseTypeName(
                            tySym.ToMinimalDisplayString(_semaModel, node.SpanStart)));
        }

        private static ArrayTypeSyntax ArrayTypeSpecWithoutDimension(ArrayTypeSyntax tySpec)
        {
            var rankSpecs = SyntaxFactory.List<ArrayRankSpecifierSyntax>();
            foreach (var r in tySpec.RankSpecifiers)
            {
                var sizeExprs = SyntaxFactory.SeparatedList<ExpressionSyntax>();
                r.Sizes.ToList()
                        .ForEach(_ =>
                            sizeExprs = sizeExprs.Add(SyntaxFactory.OmittedArraySizeExpression()));
                rankSpecs = rankSpecs.Add(SyntaxFactory.ArrayRankSpecifier(sizeExprs));
            }
            return tySpec.WithRankSpecifiers(rankSpecs);
        }

        public override SyntaxNode VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
        {
            if (IsExpressionTreeVisit(node))
                return node;

            return base.VisitSimpleLambdaExpression(node);
        }

        public override SyntaxNode VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
        {
            if (IsExpressionTreeVisit(node))
                return node;

            return base.VisitParenthesizedLambdaExpression(node);
        }

        public override SyntaxNode VisitQueryExpression(QueryExpressionSyntax node)
        {
            return node;
        }

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            return node.WithBody((BlockSyntax)node.Body.Accept(this));
        }

        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            return node;
        }

        public override SyntaxNode VisitEventFieldDeclaration(EventFieldDeclarationSyntax node)
        {
            return node;
        }

        public override SyntaxNode VisitAttribute(AttributeSyntax node)
        {
            return node;
        }
    }
}
