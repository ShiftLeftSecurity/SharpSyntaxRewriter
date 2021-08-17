// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace SharpSyntaxRewriter.Adapters
{
    public interface IFunctionSyntaxAdapter : ISyntaxAdapter
    {
        SyntaxTokenList Modifiers { get; }

        /*
         * There's a reason to have a `List' of `ParameterSyntax' rather
         * than a `ParameterListSyntax': the "simple" lambda node doesn't
         * contain the former alternative as a child, and creating one by
         * by hand isn't an option (as that would require a change in the
         * entire syntax tree).
         */
        List<ParameterSyntax> ParameterList { get; }

        BlockSyntax Body { get; }
        ExpressionSyntax ExpressionBody { get; }
    }
}
