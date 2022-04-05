// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;

using SharpSyntaxRewriter.Rewriters.Types;
using SharpSyntaxRewriter.Utilities;

namespace SharpSyntaxRewriter.Rewriters
{
    public class UnparameterizeRecordDeclaration : Rewriter
    {
        public const string ID = "<unparameterize record declaration>";

        public override string Name()
        {
            return ID;
        }

        public override SyntaxNode VisitRecordDeclaration(RecordDeclarationSyntax node)
        {
            if (ModifiersChecker.Has_abstract(node.Modifiers))
                return node;

            if (node.ParameterList == null
                    || !node.ParameterList.Parameters.Any())
                return node;

            var node_P = node.WithModifiers(
                                 SyntaxFactory.TokenList(
                                     node.Modifiers.Where(m => !m.IsKind(SyntaxKind.ReadOnlyKeyword))))
                             .WithParameterList(null)
                             .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None));

            var ctorDecl =
                SyntaxFactory.ConstructorDeclaration(node.Identifier)
                    .WithModifiers(node_P.Modifiers)
                    .WithParameterList(node.ParameterList.WithoutTrivia())
                    .WithBody(SyntaxFactory.Block().WithoutTrivia())
                    .WithoutTrivia();

            if (node_P.BaseList != null)
            {
                SeparatedSyntaxList<BaseTypeSyntax> basesTy_P = new();
                foreach (var baseTyAndCtor in node_P.BaseList.Types)
                {
                    BaseTypeSyntax baseTy;
                    if (baseTyAndCtor is PrimaryConstructorBaseTypeSyntax ctorDeclTy)
                    {
                        ctorDecl = ctorDecl
                            .WithInitializer(
                                SyntaxFactory.ConstructorInitializer(
                                    SyntaxKind.BaseConstructorInitializer,
                                    ctorDeclTy.ArgumentList));
                        baseTy = SyntaxFactory.SimpleBaseType(ctorDeclTy.Type);
                    }
                    else
                    {
                        baseTy = baseTyAndCtor;
                    }
                    basesTy_P = basesTy_P.Add(baseTy);
                }
                node_P = node_P.WithBaseList(SyntaxFactory.BaseList(basesTy_P));
            }

            SyntaxList<MemberDeclarationSyntax> membDecls_P = new();
            foreach (var parmDecl in node.ParameterList.Parameters)
            {
                var initStmt =
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.ThisExpression(),
                                SyntaxFactory.IdentifierName(parmDecl.Identifier.Text)),
                            SyntaxFactory.IdentifierName(parmDecl.Identifier.Text)));
                ctorDecl = ctorDecl.WithBody(ctorDecl.Body.AddStatements(initStmt));

                var propDecl =
                    SyntaxFactory.PropertyDeclaration(parmDecl.Type, parmDecl.Identifier.Text)
                        .WithModifiers(SyntaxFactory.TokenList(
                            SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                        .WithAccessorList(
                             SyntaxFactory.AccessorList(
                                 SyntaxFactory.List<AccessorDeclarationSyntax>().Add(
                                     SyntaxFactory.AccessorDeclaration(
                                         SyntaxKind.GetAccessorDeclaration)
                                     .WithSemicolonToken(
                                         SyntaxFactory.Token(SyntaxKind.SemicolonToken))).Add(
                                     SyntaxFactory.AccessorDeclaration(
                                         SyntaxKind.InitAccessorDeclaration)
                                     .WithSemicolonToken(
                                         SyntaxFactory.Token(SyntaxKind.SemicolonToken)))));
                membDecls_P = membDecls_P.Add(propDecl);
            }

            membDecls_P = membDecls_P.Add(ctorDecl);

            node_P = node_P
                .WithModifiers(node_P.Modifiers.Remove(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)))
                .WithMembers(membDecls_P.AddRange(node_P.Members))
                .WithIdentifier(
                    node.Identifier
                        .WithTrailingTrivia(
                            node.ParameterList.OpenParenToken.LeadingTrivia.AddRange(
                            node.ParameterList.CloseParenToken.TrailingTrivia)));

            if (!node.SemicolonToken.IsKind(SyntaxKind.None))
            {
                node_P = node_P
                    .WithOpenBraceToken(SyntaxFactory.Token(SyntaxKind.OpenBraceToken)
                        .WithLeadingTrivia(node.SemicolonToken.LeadingTrivia))
                    .WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.CloseBraceToken)
                        .WithTrailingTrivia(node.SemicolonToken.TrailingTrivia));
            }

            return node_P;
        }

        public override SyntaxNode VisitQueryExpression(QueryExpressionSyntax node)
        {
            return node;
        }
    }
}
