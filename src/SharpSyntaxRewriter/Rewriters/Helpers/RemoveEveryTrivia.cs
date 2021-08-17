// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SharpSyntaxRewriter.Rewriters.Helpers
{
    internal class RemoveEveryTrivia__ : CSharpSyntaxRewriter
    {
        public static SyntaxNode Go(CSharpSyntaxNode node)
        {
            return node.Accept(new RemoveEveryTrivia__());
        }

        public override SyntaxNode Visit(SyntaxNode node)
        {
            var node_P = base.Visit(node);
            if (node_P != null)
                node_P = node_P.WithoutTrivia();
            return node_P;
        }

        public override SyntaxTrivia VisitTrivia(SyntaxTrivia _)
        {
            return default;
        }
    }
}
