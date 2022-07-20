// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace SharpSyntaxRewriter.Rewriters.Types
{
    public abstract class StatementSynthesizerRewriter : SymbolicRewriter
    {
        private readonly Stack<List<StatementSyntax>> __ctx = new();
        protected Stack<List<StatementSyntax>> _ctx
        {
            get { return __ctx; }
        }

        public override SyntaxNode Visit(SyntaxNode node)
        {
            if (node is StatementSyntax
                    && node.Kind() != SyntaxKind.Block
                    && !(node.Parent is SwitchSectionSyntax))
            {
                _ctx.Push(new List<StatementSyntax>());
                var node_P = (StatementSyntax)base.Visit(node);
                var synthesizedStmts = _ctx.Pop();

                if (synthesizedStmts.Any())
                {
                    return SyntaxFactory.Block(synthesizedStmts)
                                        .AddStatements(node_P.WithTrailingTrivia())
                                        .WithTrailingTrivia(node_P.GetTrailingTrivia());
                }

                return node_P;
            }

            return base.Visit(node);
        }

        private SyntaxList<StatementSyntax> VisitStatements(SyntaxList<StatementSyntax> stmts)
        {
            var replaceStmts = false;
            var stmts_P = SyntaxFactory.List<StatementSyntax>();
            foreach (var stmt in stmts)
            {
                _ctx.Push(new List<StatementSyntax>());
                var stmtP = stmt.Accept(this);
                var synthesizedStmts = _ctx.Pop();

                if (stmtP != stmt || synthesizedStmts.Any())
                {
                    stmts_P = stmts_P.AddRange(SyntaxFactory.List(synthesizedStmts));
                    replaceStmts = true;
                }
                stmts_P = stmts_P.Add((StatementSyntax)stmtP);
            }

            return replaceStmts ? stmts_P
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
            // A `switch' section is "special" in the sense that it may contain
            // multiple statements that are not surrounded by a lexical block.

            var stmts_P = VisitStatements(node.Statements);

            return stmts_P == node.Statements
                    ? node
                    : node.WithStatements(stmts_P);
        }
    }
}
