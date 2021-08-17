// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SharpSyntaxRewriter.Rewriters.Types
{
    public abstract class BaseRewriter : CSharpSyntaxRewriter, IRewriter
    {
        public abstract string Name();

        public abstract bool IsPurelySyntactic();

        public virtual SyntaxTree RewriteTree(SyntaxTree tree)
        {
            return Visit(tree.GetRoot()).SyntaxTree;
        }

        private int __freshNameID;
        protected string FreshName(string prefix)
        {
            return prefix + "_" + __freshNameID++;
        }

        // DESGIN: This is out of place... re-think.
        public virtual void Reset() { }
    }
}