// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

#define DEBUG_INACCURATE_REWRITES

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using SharpSyntaxRewriter.Extensions;
using SharpSyntaxRewriter.Rewriters.Exceptions;

namespace SharpSyntaxRewriter.Rewriters.Types
{
    public abstract class SymbolicRewriter : BaseRewriter
    {
        public override bool IsPurelySyntactic()
        {
            return false;
        }

        protected SemanticModel _semaModel { get; private set; }

        private bool __expectAccurateRewrite;

        public virtual SyntaxTree Apply(SyntaxTree tree,
                                        SemanticModel semaModel,
                                        bool expectAccurateRewrite = true)
        {
            Debug.Assert(semaModel != null);
            _semaModel = semaModel;

            __expectAccurateRewrite = expectAccurateRewrite;

            return base.RewriteTree(tree);
        }

        private bool __wasRewriteAcurate = true;
        public bool WasRewriteAccurate => __wasRewriteAcurate;

        protected void NodeWithoutSymbol(SyntaxNode node)
        {
#if DEBUG_INACCURATE_REWRITES
            Console.WriteLine($"innacurate rewrite in node: {node}");
#endif

            __wasRewriteAcurate = false;
            if (__expectAccurateRewrite)
                throw new UnexpectedInaccurateRewriteException(node?.ToString());
        }

        public bool IsExpressionTreeVisit(AnonymousFunctionExpressionSyntax node)
        {
            return node.ResultType(_semaModel,
                                   TypeFormation.PossiblyConverted)
                       .IsExpressionTree();
        }
    }
}
