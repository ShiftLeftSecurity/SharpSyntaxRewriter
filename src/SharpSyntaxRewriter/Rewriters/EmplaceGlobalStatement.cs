// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
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

        public override SyntaxNode VisitCompilationUnit(CompilationUnitSyntax node)
        {
            var node_P = (CompilationUnitSyntax)base.VisitCompilationUnit(node);

            if (!__stmtsNodes.Any())
                return node_P;

            var tyDecl =
                SyntaxFactory.ClassDeclaration("<Program>$")
                    .WithMembers(
                        SyntaxFactory.List<MemberDeclarationSyntax>().Add(
                            SyntaxFactory.MethodDeclaration(
                                    SyntaxFactory.ParseTypeName("void"),
                                    "<Main>$")
                                .WithModifiers(
                                    SyntaxFactory.TokenList(
                                        SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                                        SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                                .WithBody(SyntaxFactory.Block(__stmtsNodes))));

            node_P = node_P.AddMembers(tyDecl);

            return node_P;
        }

        public override SyntaxNode VisitGlobalStatement(GlobalStatementSyntax node)
        {
            __stmtsNodes.Add(node.Statement);
            return null;
        }
    }
}
