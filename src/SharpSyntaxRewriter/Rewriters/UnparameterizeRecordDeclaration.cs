// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

            var node_P = node.WithParameterList(null)
                             .WithSemicolonToken(
                                 SyntaxFactory.Token(SyntaxKind.None));

            var ctorDecl =
                SyntaxFactory.ConstructorDeclaration(node.Identifier)
                    .WithModifiers(node.Modifiers)
                    .WithParameterList(node.ParameterList.WithoutTrivia())
                    .WithBody(SyntaxFactory.Block().WithoutTrivia())
                    .WithoutTrivia();

            if (node_P.BaseList != null)
            {
                SeparatedSyntaxList<BaseTypeSyntax> bases_P = new();
                foreach (var b in node_P.BaseList.Types)
                {
                    var baseTy = b;
                    if (baseTy is PrimaryConstructorBaseTypeSyntax baseCtorDecl)
                    {
                        ctorDecl =
                            ctorDecl.WithInitializer(
                                SyntaxFactory.ConstructorInitializer(
                                    SyntaxKind.BaseConstructorInitializer,
                                    baseCtorDecl.ArgumentList));
                        baseTy =
                            SyntaxFactory.SimpleBaseType(baseCtorDecl.Type);
                    }
                    bases_P = bases_P.Add(baseTy);
                }
                node_P = node_P.WithBaseList(SyntaxFactory.BaseList(bases_P));
            }

            var membDecls = node_P.Members;
            if (membDecls.Any())
            {
                var membDecl = membDecls.Last();
                membDecls = membDecls.Replace(membDecl, membDecl.WithoutTrailingTrivia());
                ctorDecl = ctorDecl.WithTrailingTrivia(membDecl.GetTrailingTrivia());
            }
            membDecls = membDecls.Add(ctorDecl);

            foreach (var parmDecl in node.ParameterList.Parameters)
            {
                var propDecl =
                    SyntaxFactory.PropertyDeclaration(parmDecl.Type, parmDecl.Identifier)
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
                var membDecl = membDecls.Last();
                membDecls = membDecls
                    .Replace(membDecl, membDecl.WithoutTrailingTrivia())
                    .Add(propDecl.WithTrailingTrivia(membDecl.GetTrailingTrivia()));
            }

            node_P = node_P.WithMembers(membDecls);

            if (!node.SemicolonToken.IsKind(SyntaxKind.None))
            {
                node_P = node_P
                    .WithOpenBraceToken(SyntaxFactory.Token(SyntaxKind.OpenBraceToken)
                        .WithLeadingTrivia(node.SemicolonToken.LeadingTrivia))
                    .WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.CloseBraceToken)
                        .WithTrailingTrivia(node.SemicolonToken.TrailingTrivia));
            }

            node_P = node_P
                .WithIdentifier(
                    node.Identifier
                        .WithTrailingTrivia(
                            node.ParameterList.OpenParenToken.LeadingTrivia.AddRange(
                            node.ParameterList.CloseParenToken.TrailingTrivia)));

            return node_P;
        }

        public override SyntaxNode VisitQueryExpression(QueryExpressionSyntax node)
        {
            return node;
        }
    }
}
