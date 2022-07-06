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

        private readonly Stack<ExpressionSyntax> __exprs = new();
        private readonly Stack<DeclarationPatternSyntax> __patterns = new();
        private readonly Stack<HashSet<string>> __declaredNames = new();

        public override SyntaxNode VisitDeclarationPattern(DeclarationPatternSyntax node)
        {
            Debug.Assert(node.Designation is SingleVariableDesignationSyntax);
            var varIdent = ((SingleVariableDesignationSyntax)node.Designation).Identifier;
            var varName = varIdent.Text;

            if (__declaredNames.Any())
            {
                if (__declaredNames.Peek().Contains(varName))
                    return node;

                __declaredNames.Peek().Add(varName);
            }

            Debug.Assert(__exprs.Any());
            var expr = __exprs.Peek();

            var type = __patterns.Any()
                ? SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword))
                : node.Type;

            var declStmt =
                SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(
                        type,
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(
                                varIdent.WithoutTrivia(),
                                null,
                                SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.CastExpression(
                                        type,
                                        expr))))));

            _ctx.Peek().Add(declStmt);

            return node;
        }

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
            /*
             * A `switch' expression may contain `switch' "arms" with declaration patterns
             * whose declared name are the same; but we don't want to replicate these declarations.
             */
            __declaredNames.Push(new HashSet<string>());

            __exprs.Push(node.GoverningExpression);
            var node_P = (SwitchExpressionSyntax)base.VisitSwitchExpression(node);
            __exprs.Pop();

            __declaredNames.Pop();

            return node_P;
        }

        public override SyntaxNode VisitSwitchExpressionArm(SwitchExpressionArmSyntax node)
        {
            if (node.Pattern is not DeclarationPatternSyntax declPatt
                    || declPatt.Designation is not SingleVariableDesignationSyntax)
            {
                return node;
            }

            __patterns.Push(declPatt);
            var node_P = (SwitchExpressionArmSyntax)base.VisitSwitchExpressionArm(node);
            __patterns.Pop();

            return node_P.WithPattern(SyntaxFactory.ConstantPattern(declPatt.Type));
        }

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (!__patterns.Any())
                return node;

            var pattern = __patterns.Peek();

            Debug.Assert(pattern.Designation is SingleVariableDesignationSyntax);
            var varDesign = (SingleVariableDesignationSyntax)pattern.Designation;

            if (varDesign.Identifier.Text != node.Identifier.Text)
                return node;

            return SyntaxFactory.CastExpression(pattern.Type, node);
        }
    }
}

