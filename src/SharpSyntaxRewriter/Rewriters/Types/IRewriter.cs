// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;

namespace SharpSyntaxRewriter.Rewriters.Types
{
    public interface IRewriter
    {
        string Name();

        bool IsPurelySyntactic();

        SyntaxTree RewriteTree(SyntaxTree tree);
    }
}