// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

using SharpSyntaxRewriter.Adapters;
using SharpSyntaxRewriter.Rewriters.Types;
using SharpSyntaxRewriter.Utilities;

namespace SharpSyntaxRewriter.Rewriters
{
    public class ImposeExplicitReturn : SymbolicRewriter
    {
        public const string ID = "<impose explicit `return'>";

        public override string Name()
        {
            return ID;
        }

        private bool IsMissingReturn(IFunctionSyntaxAdapter funcAdapter,
                                     TypeSyntax retTySpec)
        {
            if (funcAdapter.Body == null
                    || !ReturnTypeInfo.ImpliesVoid(
                            retTySpec,
                            ModifiersChecker.Has_async(funcAdapter.Modifiers), _semaModel))
            {
                return false;
            }

            return !funcAdapter.Body.Statements.Any()
                    || !(funcAdapter.Body.Statements.Last() is ReturnStatementSyntax
                         || funcAdapter.Body.Statements.Last() is ThrowStatementSyntax);
        }

        private static BlockSyntax SynthesizeReturn(BlockSyntax node)
        {
            return node.WithStatements(
                            node.Statements.Add(
                                 SyntaxFactory.ReturnStatement()
                                     .WithLeadingTrivia(node.CloseBraceToken.LeadingTrivia)))
                       .WithCloseBraceToken(node.CloseBraceToken.WithLeadingTrivia());
        }

        private BaseMethodDeclarationSyntax VisitBaseMethodDeclaration<MethodDeclarationSyntaxT>(
                MethodDeclarationSyntaxT node,
                Func<MethodDeclarationSyntaxT, SyntaxNode> visit,
                TypeSyntax retTySpec,
                IFunctionSyntaxAdapter funcAdapter)
            where MethodDeclarationSyntaxT : BaseMethodDeclarationSyntax
        {
            var node_P = (MethodDeclarationSyntaxT)visit(node);

            return IsMissingReturn(funcAdapter, retTySpec)
                       ? node_P.WithBody(SynthesizeReturn(node_P.Body))
                       : node_P;
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            return VisitBaseMethodDeclaration(node,
                                              base.VisitMethodDeclaration,
                                              node.ReturnType,
                                              new AdaptedBaseMethodDeclaration(node));
        }

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            return VisitBaseMethodDeclaration(node,
                                              base.VisitConstructorDeclaration,
                                              null,
                                              new AdaptedBaseMethodDeclaration(node));
        }

        public override SyntaxNode VisitDestructorDeclaration(DestructorDeclarationSyntax node)
        {
            return VisitBaseMethodDeclaration(node,
                                              base.VisitDestructorDeclaration,
                                              null,
                                              new AdaptedBaseMethodDeclaration(node));
        }

        public override SyntaxNode VisitOperatorDeclaration(OperatorDeclarationSyntax node)
        {
            return VisitBaseMethodDeclaration(node,
                                              base.VisitOperatorDeclaration,
                                              node.ReturnType,
                                              new AdaptedBaseMethodDeclaration(node));
        }

        public override SyntaxNode VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node)
        {
            return VisitBaseMethodDeclaration(node,
                                              base.VisitConversionOperatorDeclaration,
                                              null,
                                              new AdaptedBaseMethodDeclaration(node));
        }

        private BasePropertyDeclarationSyntax __propDecl;

        private SyntaxNode VisitBasePropertyDeclaration<PropertyDeclarationT>(
                PropertyDeclarationT node,
                Func<PropertyDeclarationT, SyntaxNode> visit)
            where PropertyDeclarationT : BasePropertyDeclarationSyntax
        {
            __propDecl = node;
            var node_P = visit(node);
            __propDecl = null;

            return node_P;
        }

        public override SyntaxNode VisitIndexerDeclaration(IndexerDeclarationSyntax node)
        {
            return VisitBasePropertyDeclaration(node,
                                                base.VisitIndexerDeclaration);
        }

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            return VisitBasePropertyDeclaration(node,
                                                base.VisitPropertyDeclaration);
        }

        public override SyntaxNode VisitEventDeclaration(EventDeclarationSyntax node)
        {
            return VisitBasePropertyDeclaration(node,
                                                base.VisitEventDeclaration);
        }

        public override SyntaxNode VisitAccessorDeclaration(AccessorDeclarationSyntax node)
        {
            var node_P = (AccessorDeclarationSyntax)base.VisitAccessorDeclaration(node);

            switch (node_P.Kind())
            {
                case SyntaxKind.GetAccessorDeclaration:
                    return node_P;

                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.InitAccessorDeclaration:
                    return IsMissingReturn(
                                   new AdaptedAccessorMethod(node_P, __propDecl),
                                   null)
                               ? node_P.WithBody(SynthesizeReturn(node_P.Body))
                               : node_P;

                default:
                    return node_P;
            }
        }

        public override SyntaxNode VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
        {
            var node_P = (LocalFunctionStatementSyntax)base.VisitLocalFunctionStatement(node);

            return IsMissingReturn(new AdaptedLocalFunction(node_P),
                                   node.ReturnType)
                          ? node_P.WithBody(SynthesizeReturn(node_P.Body))
                          : node_P;
        }

        public SyntaxNode VisitLambdaExpression<LambdaT>(
                LambdaT node,
                Func<LambdaT, SyntaxNode> visit)
            where LambdaT : AnonymousFunctionExpressionSyntax
        {
            var node_P = (LambdaT)visit(node);

            var methSym = _semaModel.GetSymbolInfo(node).Symbol as IMethodSymbol;
            if (!ValidateSymbol(methSym)
                    || !ReturnTypeInfo.ImpliesVoid(methSym.ReturnType,
                                                   methSym.IsAsync))
            {
                return node_P;
            }

            return IsMissingReturn(new AdaptedAnonymousFunction(node_P), null)
                          ? node_P.WithBody(SynthesizeReturn(node_P.Block))
                          : node_P;
        }

        public override SyntaxNode VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
        {
            if (IsExpressionTreeVisit(node))
                return node;

            return VisitLambdaExpression(node,
                                         base.VisitSimpleLambdaExpression);
        }

        public override SyntaxNode VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
        {
            if (IsExpressionTreeVisit(node))
                return node;

            return VisitLambdaExpression(node,
                                         base.VisitParenthesizedLambdaExpression);
        }

        public override SyntaxNode VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node)
        {
            return VisitLambdaExpression(node,
                                         base.VisitAnonymousMethodExpression);
        }

        public override SyntaxNode VisitQueryExpression(QueryExpressionSyntax node)
        {
            return node;
        }
    }
}
