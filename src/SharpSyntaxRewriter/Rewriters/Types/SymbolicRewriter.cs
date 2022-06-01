// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

#define DEBUG_INACCURATE_REWRITES

using System;
using System.Diagnostics;
using System.Text;
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
        private bool __reliableSemaModel;

        public virtual SyntaxTree Apply(SyntaxTree tree,
                                        SemanticModel semaModel,
                                        bool reliableSemaModel = true)
        {
            Debug.Assert(semaModel != null);
            _semaModel = semaModel;
            __reliableSemaModel = reliableSemaModel;

            return base.RewriteTree(tree);
        }

        private bool __wasRewriteAcurate = true;
        public bool WasRewriteAccurate => __wasRewriteAcurate;

        protected void SymbolIsInvalid(ISymbol sym)
        {
            StringBuilder sb = new("invalid symbol");
            foreach (var loc in sym?.Locations)
            {
                if (loc.IsInMetadata || !loc.IsInSource)
                {
                    sb.Append($" (metadata or unknown: {loc.Kind})");
                    continue;
                }

                sb.Append(loc.SourceTree.FilePath);
                sb.Append(' ');
                sb.Append(loc.SourceSpan);
            }

#if DEBUG_INACCURATE_REWRITES
            Console.WriteLine(sb.ToString());
#endif

            __wasRewriteAcurate = false;
            if (__reliableSemaModel)
                throw new UnexpectedInaccurateRewriteException(sb.ToString());
        }

        protected bool ValidateSymbol(ISymbol sym)
        {
            if (sym == null || sym.Kind == SymbolKind.ErrorType)
            {
                SymbolIsInvalid(sym);
                return false;
            }
            return true;
        }

        protected bool ValidateSymbol(ITypeSymbol tySym)
        {
            if (tySym == null || tySym.TypeKind == TypeKind.Error)
            {
                SymbolIsInvalid(tySym);
                return false;
            }
            return true;
        }

        protected bool ValidateSymbol(IMethodSymbol methSym)
        {
            if (methSym == null
                    || methSym.ReturnType == null
                    || methSym.ReturnType.TypeKind == TypeKind.Error)
            {
                SymbolIsInvalid(methSym);
                return false;
            }
            return true;
        }

        public bool IsExpressionTreeVisit(AnonymousFunctionExpressionSyntax node)
        {
            return node.ResultType(_semaModel,
                                   TypeFormation.PossiblyConverted)
                       .IsExpressionTree();
        }
    }
}
