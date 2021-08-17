// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;

namespace SharpSyntaxRewriter.Rewriters.Types
{
    public abstract class Rewriter : BaseRewriter
    {
        public override bool IsPurelySyntactic()
        {
            return true;
        }

        public virtual SyntaxTree Apply(SyntaxTree tree)
        {
            return base.RewriteTree(tree);
        }
    }
}