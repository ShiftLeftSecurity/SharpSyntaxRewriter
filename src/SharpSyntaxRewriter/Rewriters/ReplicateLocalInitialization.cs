// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo, Julian Thome

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpSyntaxRewriter.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using SharpSyntaxRewriter.Rewriters.Types;
using SharpSyntaxRewriter.Rewriters.Helpers;

namespace SharpSyntaxRewriter.Rewriters
{
    // TODO: Inherit from `StatementSynthesizerRewriter'.
    public class ReplicateLocalInitialization : SymbolicRewriter
    {
        public const string ID = "<replicate local initialization>";

        public override string Name()
        {
            return ID;
        }

        private readonly Stack<List<StatementSyntax>> __ctx = new();

        private SyntaxList<StatementSyntax> VisitStatements(SyntaxList<StatementSyntax> stmts)
        {
            var replaceStmts = false;
            var stmts_P = SyntaxFactory.List<StatementSyntax>();
            foreach (var stmt in stmts)
            {
                __ctx.Push(new List<StatementSyntax>());
                var stmt_P = stmt.Accept(this);
                var synthesizedStmts = __ctx.Pop();

                if (stmt_P != null)
                {
                    if (synthesizedStmts.Any())
                        stmt_P = stmt_P.WithoutTrailingTrivia();
                    stmts_P = stmts_P.Add((StatementSyntax)stmt_P);
                }

                if (stmt_P != stmt || synthesizedStmts.Any())
                {
                    if (synthesizedStmts.Any())
                    {
                        synthesizedStmts[synthesizedStmts.Count - 1] =
                            synthesizedStmts.Last()
                                            .WithTrailingTrivia(stmt.GetTrailingTrivia());
                    }
                    stmts_P = stmts_P.AddRange(SyntaxFactory.List(synthesizedStmts));
                    replaceStmts = true;
                }
            }

            return replaceStmts
                        ? stmts_P
                        : stmts;
        }

        public override SyntaxNode VisitBlock(BlockSyntax node)
        {
            var stmts_P = VisitStatements(node.Statements);

            return stmts_P == node.Statements
                    ? node
                    : node.WithStatements(stmts_P);
        }

        public override SyntaxNode VisitSwitchSection(SwitchSectionSyntax node)
        {
            var stmts_P = VisitStatements(node.Statements);

            return stmts_P == node.Statements
                    ? node
                    : node.WithStatements(stmts_P);
        }

        private SyntaxNode VisitUnscopedStatement(SyntaxNode node,
                                                  Func<SyntaxNode> visit,
                                                  Func<SyntaxNode, ExpressionSyntax> exprOf,
                                                  Func<SyntaxNode, ExpressionSyntax, StatementSyntax> createStmt,
                                                  string name)
        {
            __ctx.Push(new List<StatementSyntax>());
            var node_P = visit();
            var newStmts = __ctx.Pop();

            if (!newStmts.Any())
                return node_P;

            var tySym = _semaModel.GetTypeInfo(exprOf(node)).Type;
            if (!ValidateSymbol(tySym))
                return node_P;

            if (exprOf(node_P).Stripped() is AssignmentExpressionSyntax assignExpr)
            {
                var assignExprStmt = SyntaxFactory.ExpressionStatement(assignExpr)
                    .WithoutTrailingTrivia();
                __ctx.Peek().Add(assignExprStmt);

                __ctx.Peek().AddRange(newStmts);

                node_P = node_P.WithoutLeadingTrivia()
                             .WithTrailingTrivia(node.GetTrailingTrivia());
                __ctx.Peek().Add(createStmt(node_P, assignExpr.Left));
            }
            else
            {
                var objName = SyntaxFactory.IdentifierName(FreshName(name));
                var objDecl = SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(
                        SyntaxFactory.ParseTypeName(tySym.ToMinimalDisplayString(_semaModel, node.SpanStart)),
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(
                                objName.Identifier,
                                null,
                                SyntaxFactory.EqualsValueClause(exprOf(node_P))))));

                __ctx.Peek().Add(objDecl);
                __ctx.Peek().AddRange(newStmts);
                __ctx.Peek().Add(createStmt(node_P, objName));
            }

            return null;
        }

        private SyntaxNode VisitDeclarationContainerCommon(Func<SyntaxNode> visit,
                                                           Func<SyntaxNode, StatementSyntax> stmtOf,
                                                           Func<SyntaxNode, BlockSyntax, SyntaxNode> createStmt)
        {
            __ctx.Push(new List<StatementSyntax>());
            var node_P = visit();
            var newStmts = __ctx.Pop();

            if (!newStmts.Any())
                return node_P;

            var stmt = stmtOf(node_P);
            if (stmt is BlockSyntax block)
            {
                newStmts.AddRange(block.Statements.ToArray());
                return createStmt(node_P, block.WithStatements(SyntaxFactory.List(newStmts)));
            }

            newStmts.Add(stmt.WithTrailingTrivia());
            var newBlock = SyntaxFactory.Block(newStmts.ToArray()).WithTrailingTrivia(stmt.GetTrailingTrivia());

            return createStmt(node_P, newBlock);
        }

        public override SyntaxNode VisitUsingStatement(UsingStatementSyntax node)
        {
            return VisitDeclarationContainerCommon(
                    () => (UsingStatementSyntax)base.VisitUsingStatement(node),
                    (n) => ((UsingStatementSyntax)n).Statement,
                    (n, b) => ((UsingStatementSyntax)n).WithStatement(b));
        }

        public override SyntaxNode VisitForStatement(ForStatementSyntax node)
        {
            return VisitDeclarationContainerCommon(
                    () => (ForStatementSyntax)base.VisitForStatement(node),
                    (n) => ((ForStatementSyntax)n).Statement,
                    (n, b) => ((ForStatementSyntax)n).WithStatement(b));
        }

        public override SyntaxNode VisitReturnStatement(ReturnStatementSyntax node)
        {
            return VisitUnscopedStatement(
                node,
                () => (ReturnStatementSyntax)base.VisitReturnStatement(node),
                (n) => ((ReturnStatementSyntax)n).Expression,
                (n, e) => ((ReturnStatementSyntax)n).WithExpression(e),
                "____ret");
        }

        public override SyntaxNode VisitSwitchStatement(SwitchStatementSyntax node)
        {
            return VisitUnscopedStatement(
                node,
                () => (SwitchStatementSyntax)base.VisitSwitchStatement(node),
                (n) => ((SwitchStatementSyntax)n).Expression,
                (n, e) => ((SwitchStatementSyntax)n).WithExpression(e),
                "____sw");
        }

        private readonly Stack<ExpressionSyntax> __qualNameCtx = new();

        public override SyntaxNode VisitInitializerExpression(InitializerExpressionSyntax node)
        {
            if (!node.Expressions.Any())
                return node;

            var storeObj = IdentifyObjectCreationStore(node);
            if (storeObj == null
                    && __qualNameCtx.Any()
                    && node.Parent is AssignmentExpressionSyntax
                    && node.Parent.Parent is InitializerExpressionSyntax)
            {
                storeObj = __qualNameCtx.Peek();
            }

            if (storeObj != null)
                VisitStoredInitializerExpression(SynthesizeStoreExpression(storeObj), node);

            return node;
        }

        private void VisitStoredInitializerExpression(ExpressionSyntax baseExpr, InitializerExpressionSyntax initExpr)
        {
            Debug.Assert(initExpr != null, "expected initializer");

            switch (initExpr.Kind())
            {
                case SyntaxKind.ObjectInitializerExpression:
                    foreach (var expr in initExpr.Expressions)
                    {
                        if (expr is not AssignmentExpressionSyntax assignExpr)
                        {
                            //skip
                            continue;
                        }

                        if (assignExpr.Left is IdentifierNameSyntax identName)
                        {
                            var membAccExpr =
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    baseExpr.WithoutTrivia(),
                                    SyntaxFactory.Token(SyntaxKind.DotToken),
                                    identName.WithoutTrivia());

                            __qualNameCtx.Push(membAccExpr);
                            _ = assignExpr.Right.Accept(this);
                            _ = __qualNameCtx.Pop();

                            if (assignExpr.Right is InitializerExpressionSyntax)
                                continue;

                            var exprStmt =
                                SyntaxFactory.ExpressionStatement(
                                    SyntaxFactory.AssignmentExpression(
                                        SyntaxKind.SimpleAssignmentExpression,
                                        membAccExpr,
                                        assignExpr.Right));
                            __ctx.Peek().Insert(0, (StatementSyntax)RemoveEveryTrivia__.Go(exprStmt));
                        }
                        // TODO: We don't yet support assignment-style for dictionary objects.
                    }
                    break;

                case SyntaxKind.CollectionInitializerExpression:
                    foreach (var expr in initExpr.Expressions)
                    {
                        var exprStmt = ReplicateThroughCall(
                            baseExpr,
                            expr.Kind() == SyntaxKind.ComplexElementInitializerExpression
                                ? ComplexExpressionToArgumentList(expr)
                                : SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SeparatedList(
                                        new List<ArgumentSyntax> {
                                            SyntaxFactory.Argument(expr)})));
                        __ctx.Peek().Add((StatementSyntax)RemoveEveryTrivia__.Go(exprStmt));
                    }
                    break;

                case SyntaxKind.ArrayInitializerExpression:
                    var unwrapped = DoUnwrap(initExpr);
                    foreach (var item in unwrapped)
                    {
                        var idxs = item.Item1;
                        var init = item.Item2;

                        // Generate the index sequence
                        var args = new List<ArgumentSyntax>();
                        foreach (int idx in idxs)
                        {
                            args.Add(
                                SyntaxFactory.Argument(
                                    SyntaxFactory.LiteralExpression(
                                        SyntaxKind.NumericLiteralExpression,
                                        SyntaxFactory.ParseToken($"{idx}"))));
                        }

                        var exprStmt =
                            SyntaxFactory.ExpressionStatement(
                                SyntaxFactory.AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                        SyntaxFactory.ElementAccessExpression(
                                             baseExpr.WithoutTrivia(),
                                             SyntaxFactory.BracketedArgumentList(
                                                SyntaxFactory.SeparatedList(args))),
                                    SyntaxFactory.Token(SyntaxKind.EqualsToken),
                                    init.WithoutTrivia()));
                        __ctx.Peek().Add((StatementSyntax)RemoveEveryTrivia__.Go(exprStmt));
                    }
                    break;

                default:
                    Debug.Fail($"unknown object initializer: {initExpr}");
                    break;
            }
        }

        private SyntaxNode VisitObjectCreationCommon(ExpressionSyntax node,
                                                     InitializerExpressionSyntax initExpr,
                                                     ITypeSymbol objTySym)
        {
            if (initExpr == null
                    || !initExpr.Expressions.Any()
                    || !ValidateSymbol(objTySym))
            {
                return node;
            }

            var store = IdentifyObjectCreationStore(node);
            if (store == null)
                return node;

            var storeTySym = store is ExpressionSyntax
                    ? (store as ExpressionSyntax).ResultType(_semaModel, TypeFormation.PossiblyConverted)
                    : _semaModel.GetDeclaredSymbol(store)?.ValueType();

            if (!ValidateSymbol(storeTySym))
                return node;

            // If the assigned type is different (e.g., a base type) than the type of the object
            // being initialized, the property in question might not be accessible through it.
            // In this case, we create an extra local. See: https://github.com/dotnet/csharplang/issues/2533

            if (!SymbolEqualityComparer.Default
                                       .Equals(objTySym.OriginalDefinition,
                                               storeTySym.OriginalDefinition))
            {
                var node_P = (ExpressionSyntax)RemoveEveryTrivia__.Go(node);

                var objName = SyntaxFactory.IdentifierName(FreshName("____init"));
                var objDecl =
                    SyntaxFactory.LocalDeclarationStatement(
                        SyntaxFactory.VariableDeclaration(
                            SyntaxFactory.IdentifierName("var"),
                            SyntaxFactory.SeparatedList(
                                new List<VariableDeclaratorSyntax> {
                                    SyntaxFactory.VariableDeclarator(
                                        objName.Identifier.WithoutTrivia(),
                                        null,
                                        SyntaxFactory.EqualsValueClause(node_P)) })));

                var assignExpr = SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SynthesizeStoreExpression(store),
                        objName));

                __ctx.Peek().Insert(0, assignExpr);
                VisitStoredInitializerExpression(objName, initExpr);
                __ctx.Peek().Insert(0, objDecl);
            }
            else
                VisitStoredInitializerExpression(SynthesizeStoreExpression(store), initExpr);

            return node;
        }

        public override SyntaxNode VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            var node_P = node.WithArgumentList((ArgumentListSyntax)node.ArgumentList?.Accept(this));

            return VisitObjectCreationCommon(node_P, node_P.Initializer, _semaModel.GetTypeInfo(node).Type);
        }

        public override SyntaxNode VisitArrayCreationExpression(ArrayCreationExpressionSyntax node)
        {
            var node_P = node.WithType((ArrayTypeSyntax)node.Type?.Accept(this));

            return VisitObjectCreationCommon(node_P, node_P.Initializer, _semaModel.GetTypeInfo(node).Type);
        }

        public override SyntaxNode VisitImplicitArrayCreationExpression(ImplicitArrayCreationExpressionSyntax node)
        {
            return VisitObjectCreationCommon(node, node.Initializer, _semaModel.GetTypeInfo(node).Type);
        }

        public override SyntaxNode VisitQueryExpression(QueryExpressionSyntax node)
        {
            return node;
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

        private static SyntaxNode IdentifyObjectCreationStore(ExpressionSyntax node)
        {
            if (node.Parent is AssignmentExpressionSyntax assignExpr
                    && !(node.Parent.Parent is InitializerExpressionSyntax))
                return assignExpr.Left;

            if (node.Parent != null
                    && node.Parent.Parent is VariableDeclaratorSyntax varDecltor)
                return varDecltor;

            return null;
        }

        private static ExpressionSyntax SynthesizeStoreExpression(SyntaxNode node)
        {
            if (node is ExpressionSyntax expr)
                return expr;

            if (node is VariableDeclaratorSyntax varDecltor)
                return SyntaxFactory.IdentifierName(varDecltor.Identifier);

            Debug.Fail("invalid store");

            return null;
        }


        // TODO: Parts below need rework.

        private static StatementSyntax ReplicateThroughCall(ExpressionSyntax coreExpr, ArgumentListSyntax arguments)
        {
            return SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            coreExpr,
                            SyntaxFactory.IdentifierName("Add")),
                        arguments));
        }

        protected List<Tuple<List<int>, ExpressionSyntax>> DoUnwrap(ExpressionSyntax e)
        {
            List<Tuple<List<int>, ExpressionSyntax>> ret = new();
            Stack<int> s = new();
            RecUnwrap(e.WithoutTrivia(), s, ret);
            return ret;
        }

        // This method recursively unwraps the array initializer and computes the indicies.
        private void RecUnwrap(ExpressionSyntax e, Stack<int> l,
                               List<Tuple<List<int>, ExpressionSyntax>> d)
        {
            int oidx = 0;
            int iidx = 0;

            foreach (var exp in e.ChildNodes().OfType<ExpressionSyntax>())
            {
                if (exp is InitializerExpressionSyntax)
                {
                    l.Push(oidx);
                    ++oidx;
                    RecUnwrap(exp.WithoutTrivia(), l, d);
                }
                else
                {
                    l.Push(iidx);
                    ++iidx;
                    d.Add(Tuple.Create(new List<int>(l.ToArray().Reverse()), exp.WithoutTrivia()));
                }
                _ = l.Pop();
            }
        }

        public static ArgumentListSyntax ComplexExpressionToArgumentList(ExpressionSyntax node)
        {
            Debug.Assert(node != null);

            // Translate initializer expression to parameter list:
            // {2,3} -> Dict.Add(2,3}
            var args = SyntaxFactory.ArgumentList();
            foreach (var exp in node.ChildNodes().OfType<ExpressionSyntax>())
                args = args.AddArguments(SyntaxFactory.Argument(exp));

            return args;
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
