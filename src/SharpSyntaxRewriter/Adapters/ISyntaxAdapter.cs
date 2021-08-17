// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;

namespace SharpSyntaxRewriter.Adapters
{
    public interface ISyntaxAdapter
    {
        SyntaxNode AdaptedSyntax { get; }
    }
}
