// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System;
using System.Linq;

using SharpSyntaxRewriter.Rewriters.Types;

namespace SharpSyntaxRewriter.Rewriters
{
    public class EmplaceGlobalStatement : Rewriter
    {
        public const string ID = "<emplace global statement>";

        public override string Name()
        {
            return ID;
        }

        private readonly List<StatementSyntax> __stmtsNodes = new();

        private SyntaxTriviaList __tyDeclLeadTrivia;

        public override SyntaxNode VisitCompilationUnit(CompilationUnitSyntax node)
        {
            var node_P = (CompilationUnitSyntax)base.VisitCompilationUnit(node);

            if (!__stmtsNodes.Any())
                return node_P;

            var lastStmt = __stmtsNodes.Last();
            __stmtsNodes.Remove(lastStmt);
            __stmtsNodes.Add(lastStmt.WithoutTrailingTrivia());

            var methDecl =
                SyntaxFactory.MethodDeclaration(
                        SyntaxFactory.PredefinedType(
                            SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                        "Main")
                    .WithModifiers(
                        SyntaxFactory.TokenList(
                            SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                            SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                    .WithBody(
                        SyntaxFactory.Block(__stmtsNodes));

            var tyDecl =
                SyntaxFactory.ClassDeclaration("__Program__")
                    .WithModifiers(
                        SyntaxFactory.TokenList(
                            SyntaxFactory.Token(SyntaxKind.InternalKeyword),
                            SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                    .WithMembers(
                        SyntaxFactory.List<MemberDeclarationSyntax>().Add(methDecl))
                    .WithLeadingTrivia(__tyDeclLeadTrivia);

            node_P = node_P.AddMembers(tyDecl).WithTrailingTrivia(lastStmt.GetTrailingTrivia());

            return node_P;
        }

        public override SyntaxNode VisitGlobalStatement(GlobalStatementSyntax node)
        {
            if (!__stmtsNodes.Any())
            {
                __tyDeclLeadTrivia = __tyDeclLeadTrivia.AddRange(node.GetLeadingTrivia());
                node = node.WithoutLeadingTrivia();
            }

            __stmtsNodes.Add(node.Statement);

            return null;
        }

        public override SyntaxNode VisitUsingDirective(UsingDirectiveSyntax node)
        {
            node = node.WithLeadingTrivia(node.GetLeadingTrivia().AddRange(__tyDeclLeadTrivia));
            __tyDeclLeadTrivia = node.GetTrailingTrivia();
            node = node.WithoutTrailingTrivia();

            return node;
        }
    }
}
