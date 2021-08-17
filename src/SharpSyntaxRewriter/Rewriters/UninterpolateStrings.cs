// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Diagnostics;

using SharpSyntaxRewriter.Rewriters.Types;

namespace SharpSyntaxRewriter.Rewriters
{
    public class UninterpolateString : SymbolicRewriter
    {
        public override string Name()
        {
            return "<uninterpolate string>";
        }

        private readonly Stack<List<ExpressionSyntax>> __ctx = new();

        private enum ResultTypeName
        {
            String,
            FormattableString
        }

        private static InvocationExpressionSyntax Invocation(
            ResultTypeName resTyName,
            SeparatedSyntaxList<ArgumentSyntax> fmtArgs)
        {
            MemberAccessExpressionSyntax fmtFunc;
            switch (resTyName)
            {
                case ResultTypeName.String:
                    fmtFunc =
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.ParseTypeName("string"),
                            SyntaxFactory.IdentifierName("Format"));
                    break;

                case ResultTypeName.FormattableString:
                    fmtFunc =
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName("System"),
                                        SyntaxFactory.IdentifierName("Runtime")),
                                    SyntaxFactory.IdentifierName("CompilerServices")),
                                SyntaxFactory.IdentifierName("FormattableStringFactory")),
                            SyntaxFactory.IdentifierName("Create"));
                    break;

                default:
                    Debug.Fail("unhandled");
                    return null;
            }

            return
                SyntaxFactory.InvocationExpression(
                    fmtFunc,
                    SyntaxFactory.ArgumentList(fmtArgs));
        }

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            return node.WithBody((BlockSyntax)node.Body.Accept(this));
        }

        public override SyntaxNode VisitInterpolatedStringExpression(InterpolatedStringExpressionSyntax node)
        {
            __ctx.Push(new List<ExpressionSyntax>());
            var node_P = (InterpolatedStringExpressionSyntax)base.VisitInterpolatedStringExpression(node);
            var interpolations = __ctx.Pop();

            var fmtArgs =
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(
                        SyntaxFactory.LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            SyntaxFactory.Literal(node_P.Contents.ToString()))));
            interpolations.ForEach(
                i => fmtArgs = fmtArgs.Add(SyntaxFactory.Argument(i)));

            var convTySym = _semaModel.GetTypeInfo(node).ConvertedType;
            var conv = _semaModel.ClassifyConversion(node, convTySym);
            if (conv.IsIdentity)
                return Invocation(ResultTypeName.String, fmtArgs);

            if (conv.IsInterpolatedString)
                return Invocation(ResultTypeName.FormattableString, fmtArgs);

            // There must exist a user-defined conversion.
            var tySym = _semaModel.GetTypeInfo(node).Type;
            if (tySym.SpecialType == SpecialType.System_String)
                return Invocation(ResultTypeName.String, fmtArgs);

            return Invocation(ResultTypeName.FormattableString, fmtArgs);
        }

        public override SyntaxNode VisitInterpolation(InterpolationSyntax node)
        {
            var node_P = (InterpolationSyntax)base.VisitInterpolation(node);

            var interpolations = __ctx.Peek();
            interpolations.Add(node_P.Expression);

            return node_P.WithExpression(
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(interpolations.Count - 1)));
        }

        public override SyntaxNode VisitQueryExpression(QueryExpressionSyntax node)
        {
            return node;
        }
    }
}
