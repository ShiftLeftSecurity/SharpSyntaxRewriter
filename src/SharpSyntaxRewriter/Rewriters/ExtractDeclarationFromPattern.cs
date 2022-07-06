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

            Debug.Assert(__exprs.Any());
            var expr = __exprs.Peek();

            var declStmt =
                SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(
                        node.Type,
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(
                                varDesig.Identifier.WithoutTrivia(),
                                null,
                                SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.CastExpression(
                                        node.Type,
                                        expr))))));

            _ctx.Peek().Add(declStmt);

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

        public override SyntaxNode VisitSwitchExpression(SwitchExpressionSyntax node)
        {
            __exprs.Push(node.GoverningExpression);
            var node_P = (SwitchExpressionSyntax)base.VisitSwitchExpression(node);
            __exprs.Pop();

            return node_P;
        }

        public override SyntaxNode VisitSwitchExpressionArm(SwitchExpressionArmSyntax node)
        {
            if (node.Pattern is not DeclarationPatternSyntax declPatt
                    || declPatt.Designation is not SingleVariableDesignationSyntax)
            {
                return node;
            }

            var node_P = (SwitchExpressionArmSyntax)base.VisitSwitchExpressionArm(node);

            return node_P.WithPattern(SyntaxFactory.ConstantPattern(declPatt.Type));
        }
    }
}

