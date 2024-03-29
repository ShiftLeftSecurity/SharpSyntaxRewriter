﻿// Copyright 2021 ShiftLeft, Inc.
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
        private readonly Stack<PatternSyntax> __patterns = new();
        private readonly Stack<HashSet<string>> __declaredNames = new();

        private void __MaybeTurnDesignationIntoDeclaration(
            SingleVariableDesignationSyntax varDesig,
            TypeSyntax varDesigTy)
        {
            var varIdent = varDesig.Identifier;
            var varName = varIdent.Text;

            if (__declaredNames.Any())
            {
                if (__declaredNames.Peek().Contains(varName))
                    return;
                __declaredNames.Peek().Add(varName);
            }

            Debug.Assert(__exprs.Any());
            var expr = __exprs.Peek();

            TypeSyntax varTy;
            ExpressionSyntax varExpr;
            if (__patterns.Any())
            {
                varTy = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword));
                varExpr = expr;
            }
            else
            {
                varTy = varDesigTy;
                varExpr =
                    SyntaxFactory.CastExpression(
                        SyntaxFactory.PredefinedType(
                            SyntaxFactory.Token(SyntaxKind.ObjectKeyword)),
                        expr);
            }

            var declStmt =
                SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(
                        varTy,
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(
                                varIdent.WithoutTrivia(),
                                null,
                                SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.CastExpression(
                                        varTy,
                                        varExpr))))));

            _ctx.Peek().Add(declStmt);
        }

        public override SyntaxNode VisitRecursivePattern(RecursivePatternSyntax node)
        {
            Debug.Assert(node.Designation is SingleVariableDesignationSyntax);
            var varDesig = (SingleVariableDesignationSyntax)node.Designation;

            __MaybeTurnDesignationIntoDeclaration(varDesig, node.Type);

            return node;
        }

        public override SyntaxNode VisitDeclarationPattern(DeclarationPatternSyntax node)
        {
            Debug.Assert(node.Designation is SingleVariableDesignationSyntax);
            var varDesig = (SingleVariableDesignationSyntax)node.Designation;

            __MaybeTurnDesignationIntoDeclaration(varDesig, node.Type);

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
             * whose name are the same; but we don't want to replicate these declarations.
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
            PatternSyntax patt_P;
            switch (node.Pattern)
            {
                case DeclarationPatternSyntax declPatt:
                    if (declPatt.Designation is not SingleVariableDesignationSyntax)
                        return node;
                    patt_P =
                        SyntaxFactory.ConstantPattern(declPatt.Type);
                    break;

                case RecursivePatternSyntax recPatt:
                    if (recPatt.Designation is not SingleVariableDesignationSyntax)
                        return node;
                    patt_P =
                        SyntaxFactory.RecursivePattern(
                            recPatt.Type,
                            recPatt.PositionalPatternClause,
                            recPatt.PropertyPatternClause,
                            null);
                    break;

                default:
                    return node;
            }

            __patterns.Push(node.Pattern);
            var node_P = (SwitchExpressionArmSyntax)base.VisitSwitchExpressionArm(node);
            __patterns.Pop();

            return node_P.WithPattern(patt_P);
        }

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (!__patterns.Any())
                return node;

            var pattern = __patterns.Peek();

            Debug.Assert(pattern is DeclarationPatternSyntax
                            || pattern is RecursivePatternSyntax);

            SingleVariableDesignationSyntax varDesig;
            TypeSyntax pattType;
            switch (pattern)
            {
                case DeclarationPatternSyntax declPatt:
                    if (declPatt.Designation is not SingleVariableDesignationSyntax)
                        return node;

                    varDesig = (SingleVariableDesignationSyntax)declPatt.Designation;
                    pattType = declPatt.Type;
                    break;

                case RecursivePatternSyntax recPatt:
                    if (recPatt.Designation is not SingleVariableDesignationSyntax)
                        return node;

                    varDesig = (SingleVariableDesignationSyntax)recPatt.Designation;
                    pattType = recPatt.Type;
                    break;

                default:
                    return node;
            }

            if (varDesig.Identifier.Text != node.Identifier.Text)
                return node;

            return SyntaxFactory.CastExpression(
                pattType,
                SyntaxFactory.CastExpression(
                    SyntaxFactory.PredefinedType(
                        SyntaxFactory.Token(SyntaxKind.ObjectKeyword)),
                    node));
        }
    }
}

