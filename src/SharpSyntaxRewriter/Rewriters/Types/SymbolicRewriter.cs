// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using SharpSyntaxRewriter.Extensions;

namespace SharpSyntaxRewriter.Rewriters.Types
{
    public abstract class SymbolicRewriter : BaseRewriter
    {
        public override bool IsPurelySyntactic()
        {
            return false;
        }

        protected SemanticModel _semaModel { get; private set; }

        public virtual SyntaxTree Apply(SyntaxTree tree, SemanticModel semaModel)
        {
            Debug.Assert(semaModel != null);
            _semaModel = semaModel;

            return base.RewriteTree(tree);
        }

        public bool IsExpressionTreeVisit(AnonymousFunctionExpressionSyntax node)
        {
            return node.ResultType(_semaModel,
                                   TypeFormation.PossiblyConverted)
                       .IsExpressionTree();
        }
    }
}
