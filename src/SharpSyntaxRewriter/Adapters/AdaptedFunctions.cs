// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Diagnostics;

namespace SharpSyntaxRewriter.Adapters
{
    public abstract class ParameterProvider
    {
        internal static List<ParameterSyntax> Parameters__(ParameterListSyntax parmList)
        {
            List<ParameterSyntax> parms = new();
            if (parmList != null)
                parms.AddRange(parmList.Parameters);
            return parms;
        }
    }

    public class AdaptedBaseMethodDeclaration
        : ParameterProvider
        , IFunctionSyntaxAdapter
    {
        public SyntaxNode AdaptedSyntax { get; }

        public AdaptedBaseMethodDeclaration(BaseMethodDeclarationSyntax node) { AdaptedSyntax = node; }

        public BaseMethodDeclarationSyntax Cast => (BaseMethodDeclarationSyntax)AdaptedSyntax;
        public SyntaxTokenList Modifiers => Cast.Modifiers;
        public BlockSyntax Body => Cast.Body;
        public ExpressionSyntax ExpressionBody => Cast.ExpressionBody?.Expression;
        public List<ParameterSyntax> ParameterList => Parameters__(Cast.ParameterList);
    }

    public class AdaptedLocalFunction
        : ParameterProvider
        , IFunctionSyntaxAdapter
    {
        public SyntaxNode AdaptedSyntax { get; }

        public AdaptedLocalFunction(LocalFunctionStatementSyntax node) { AdaptedSyntax = node; }

        public LocalFunctionStatementSyntax Cast => (LocalFunctionStatementSyntax)AdaptedSyntax;
        public SyntaxTokenList Modifiers => Cast.Modifiers;
        public BlockSyntax Body => Cast.Body;
        public ExpressionSyntax ExpressionBody => Cast.ExpressionBody?.Expression;
        public List<ParameterSyntax> ParameterList => Parameters__(Cast.ParameterList);
    }

    public class AdaptedAnonymousFunction
        : ParameterProvider
        , IFunctionSyntaxAdapter
    {
        public SyntaxNode AdaptedSyntax { get; }

        public AdaptedAnonymousFunction(AnonymousFunctionExpressionSyntax node) { AdaptedSyntax = node; }

        public AnonymousFunctionExpressionSyntax Cast => (AnonymousFunctionExpressionSyntax)AdaptedSyntax;
        public SyntaxTokenList Modifiers => Cast.Modifiers;
        public BlockSyntax Body => Cast.Block;
        public ExpressionSyntax ExpressionBody => Cast.ExpressionBody;
        public List<ParameterSyntax> ParameterList
        {
            get
            {
                switch (Cast)
                {
                    case AnonymousMethodExpressionSyntax anonMeth:
                        return Parameters__(anonMeth.ParameterList);

                    case ParenthesizedLambdaExpressionSyntax lamb:
                        return Parameters__(lamb.ParameterList);

                    case SimpleLambdaExpressionSyntax simpLamb:
                        return new List<ParameterSyntax>() { simpLamb.Parameter };

                    default:
                        Debug.Fail("unhandled");
                        return null;
                }
            }
        }
    }

    public class AdaptedAccessorMethod : IFunctionSyntaxAdapter
    {
        public SyntaxNode AdaptedSyntax { get; }

        public AdaptedAccessorMethod(AccessorDeclarationSyntax node,
                                     BasePropertyDeclarationSyntax propNode = null)
        {
            AdaptedSyntax = node;
            __propNode = propNode;
        }

        private readonly BasePropertyDeclarationSyntax __propNode;

        public AccessorDeclarationSyntax Cast => (AccessorDeclarationSyntax)AdaptedSyntax;
        public SyntaxTokenList Modifiers => __propNode.Modifiers;
        public BlockSyntax Body => Cast.Body;
        public ExpressionSyntax ExpressionBody => Cast.ExpressionBody?.Expression;
        public List<ParameterSyntax> ParameterList => new();
    }
}
