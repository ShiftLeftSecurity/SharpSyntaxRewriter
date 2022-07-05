// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpSyntaxRewriter.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

using SharpSyntaxRewriter.Rewriters.Types;
using System.Diagnostics;

namespace SharpSyntaxRewriter.Rewriters
{
    public class ExtractDeclarationFromPattern : StatementSynthesizerRewriter
    {
        public const string ID = "<extract declaration from pattern>";

        public override string Name()
        {
            return ID;
        }

        public override SyntaxNode VisitDeclarationPattern(DeclarationPatternSyntax node)
        {
            Debug.Assert(node.Designation is SingleVariableDesignationSyntax);
            var varDesig = (SingleVariableDesignationSyntax)node.Designation;

            var declStmt =
                SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(
                        node.Type,
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(
                                varDesig.Identifier.WithoutTrivia(),
                                null,
                                SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.DefaultExpression(node.Type))))));

            Debug.Assert(__exprs.Any());
            var expr = __exprs.Peek();

            var ifStmt =
                SyntaxFactory.IfStatement(
                    SyntaxFactory.BinaryExpression(
                        SyntaxKind.IsExpression,
                        expr,
                        node.Type),
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            SyntaxFactory.IdentifierName(varDesig.Identifier),
                            SyntaxFactory.CastExpression(
                                node.Type,
                                expr))));

            _ctx.Peek().Add(declStmt);
            _ctx.Peek().Add(ifStmt);

            return node;
        }

        private readonly Stack<ExpressionSyntax> __exprs = new();

        public override SyntaxNode VisitIsPatternExpression(IsPatternExpressionSyntax node)
        {
            if (node.Pattern is not DeclarationPatternSyntax declPatt
                    || declPatt.Designation is not SingleVariableDesignationSyntax)
            {
                return node;
            }

            __exprs.Push(node.Expression);
            var _ = (IsPatternExpressionSyntax)base.VisitIsPatternExpression(node);
            __exprs.Pop();

            return SyntaxFactory.BinaryExpression(
                        SyntaxKind.IsExpression,
                        node.Expression,
                        declPatt.Type);
        }
    }
}

