// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

using SharpSyntaxRewriter.Rewriters.Types;
using SharpSyntaxRewriter.Utilities;

namespace SharpSyntaxRewriter.Rewriters
{
    public class EnsureVisibleConstructor : Rewriter
    {
        public const string ID = "<ensure visible constructor>";

        public override string Name()
        {
            return ID;
        }

        /*
         * The first item in the tuple flags a `static' constructor; the second,
         * an instance constructor.
         */
        private readonly Stack<(bool, bool)> __ctx = new();

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            // Preserve flags already in context, as a type may have multiple constructors.
            var (static_, instance) = __ctx.Pop();
            if (ModifiersChecker.Has_static(node.Modifiers))
                static_ = true;
            else
                instance = true;
            __ctx.Push((static_, instance));

            return node;
        }

        private TypeDeclarationSyntax VisitTypeDeclaration<TypeDeclarationT>(
                TypeDeclarationT node,
                Func<TypeDeclarationT, SyntaxNode> visit)
            where TypeDeclarationT : TypeDeclarationSyntax
        {
            /*
             * With the current design, there isn't an easy way to ensure constructor
             * existence due to partial classes: we'd need to inspect all associated
             * trees to avoid inconsistencies. I guess the best would be to conceive
             * an "aggregate partial classes" rewriter and work on the aggregated tree.
             */
            if (ModifiersChecker.Has_partial(node.Modifiers))
                return node;

            __ctx.Push((false, false));
            var node_P = (TypeDeclarationSyntax)visit(node);
            var (static_, instance)= __ctx.Pop();

            if (static_ && instance)
                return node_P;

            var ctorDecls = new List<ConstructorDeclarationSyntax>();
            if (!instance
                    && !ModifiersChecker.Has_static(node_P.Modifiers))
            {
                ctorDecls.Add(
                    SynthesizeConstructor(node.Identifier, SyntaxKind.PublicKeyword));
            }
            if (!static_)
            {
                ctorDecls.Add(
                    SynthesizeConstructor(node.Identifier, SyntaxKind.StaticKeyword));
            }

            if (ctorDecls.Any())
            {
                node_P = node_P
                    .WithOpenBraceToken(
                        node.OpenBraceToken.WithTrailingTrivia(
                        SyntaxFactory.TriviaList()));

                var ctorDecl = ctorDecls.Last()
                    .WithTrailingTrivia(
                        node.OpenBraceToken.TrailingTrivia);
                ctorDecls.RemoveAt(ctorDecls.Count - 1);
                ctorDecls.Add(ctorDecl);

                var membDecls = SyntaxFactory.List<MemberDeclarationSyntax>(ctorDecls);
                membDecls = membDecls.AddRange(node_P.Members);
                node_P = node_P.WithMembers(membDecls);
            }

            return node_P;
        }

        public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node)
        {
            return node;
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            return VisitTypeDeclaration(node, base.VisitClassDeclaration);
        }

        public override SyntaxNode VisitRecordDeclaration(RecordDeclarationSyntax node)
        {
            // Taken care by rewriter `UnparameterizeRecordDeclaration'.
            return node;
        }

        private static ConstructorDeclarationSyntax SynthesizeConstructor(
                SyntaxToken identTk,
                SyntaxKind modifier)
        {
            return
                SyntaxFactory.ConstructorDeclaration(
                    SyntaxFactory.List<AttributeListSyntax>(),
                    SyntaxFactory.TokenList(
                        SyntaxFactory.Token(modifier)),
                    identTk.WithoutTrivia(),
                    SyntaxFactory.ParameterList().WithoutTrivia(),
                    null,
                    SyntaxFactory.Block());
        }

        public override SyntaxNode VisitQueryExpression(QueryExpressionSyntax node)
        {
            return node;
        }
    }
}