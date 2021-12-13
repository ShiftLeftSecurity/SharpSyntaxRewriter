// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpSyntaxRewriter.Extensions;
using System.Collections.Generic;
using System.Linq;

using SharpSyntaxRewriter.Rewriters.Types;

namespace SharpSyntaxRewriter.Rewriters
{
    public class InitializeOutArgument : StatementSynthesizerRewriter
    {
        public const string ID = "<initialize `out' argument>";

        public override string Name()
        {
            return ID;
        }

        public override SyntaxNode VisitArgument(ArgumentSyntax node)
        {
            var node_P = (ArgumentSyntax)base.VisitArgument(node);

            var expr = node.Expression.Stripped();
            if (expr is DeclarationExpressionSyntax
                    || node.RefKindKeyword.Kind() != SyntaxKind.OutKeyword
                    || (expr is IdentifierNameSyntax identExpr
                            && identExpr.Identifier.Text == "_"))
            {
                return node_P;
            }

            var initStmt =
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        node.Expression,
                        SyntaxFactory.LiteralExpression(
                            SyntaxKind.DefaultLiteralExpression)));

            _ctx.Peek().Add(initStmt);

            return node_P;
        }

        public override SyntaxNode VisitDeclarationExpression(DeclarationExpressionSyntax node)
        {
            if (node.Parent is not ArgumentSyntax)
                return node;

            // An underscore `_' designation doesn't need to be relocated, and
            // a parenthesized designation (i.e., a tuple) doesn't seem to be
            // supported. See https://github.com/dotnet/csharplang/issues/611).

            if (node.Designation is not SingleVariableDesignationSyntax varDesig)
                return node;

            var tySpec = node.Type;
            if (tySpec.IsVar)
            {
                var tySym = _semaModel.GetTypeInfo(tySpec).Type;
                tySpec = SyntaxFactory.ParseTypeName(
                    tySym.ToMinimalDisplayString(_semaModel, node.SpanStart));
            }

            var declStmt =
                SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(
                        tySpec,
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(
                                varDesig.Identifier.WithoutTrivia(),
                                null,
                                SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.DefaultExpression(tySpec))))));

            _ctx.Peek().Add(declStmt);

            return SyntaxFactory.IdentifierName(varDesig.Identifier);
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
