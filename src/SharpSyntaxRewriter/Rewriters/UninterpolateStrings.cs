// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;

using SharpSyntaxRewriter.Rewriters.Types;

namespace SharpSyntaxRewriter.Rewriters
{
    public class UninterpolateString : SymbolicRewriter
    {
        public const string ID = "<uninterpolate string>";

        public override string Name()
        {
            return ID;
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

            var convTySym = _semaModel.GetTypeInfo(node).ConvertedType;
            if (!ValidateSymbol(convTySym))
                return node_P;

            var fmtArgs =
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(
                        SyntaxFactory.LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            SyntaxFactory.Literal(node_P.Contents.ToString()))));

            foreach (var interp in interpolations)
                fmtArgs = fmtArgs.Add(SyntaxFactory.Argument(interp));

            InvocationExpressionSyntax callExpr;
            var conv = _semaModel.ClassifyConversion(node, convTySym);
            if (!conv.Exists)
            {
                if (convTySym.SpecialType == SpecialType.System_String)
                {
                    callExpr = Invocation(ResultTypeName.String, fmtArgs);
                }
                else
                {
                    ResultTypeName resTyName = ResultTypeName.FormattableString;

                    // Account for .NET6's custom string interpolation handlers.
                    // https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/tutorials/interpolated-string-handler#implement-the-handler-pattern
                    foreach (var attr in convTySym.GetAttributes())
                    {
                        if (attr.AttributeClass != null
                                && (attr.AttributeClass.ToDisplayString() ==
                                    "System.Runtime.CompilerServices.InterpolatedStringHandlerAttribute"))
                        {
                            resTyName = ResultTypeName.String;
                            break;
                        }
                    }

                    callExpr = Invocation(resTyName, fmtArgs);
                }
            }
            else if (conv.IsIdentity)
            {
                callExpr = Invocation(ResultTypeName.String, fmtArgs);
            }
            else if (conv.IsInterpolatedString)
            {
                callExpr = Invocation(ResultTypeName.FormattableString, fmtArgs);
            }
            else
            {
                // There must exist a user-defined conversion.
                var tySym = _semaModel.GetTypeInfo(node).Type;
                if (!ValidateSymbol(tySym))
                    return node_P;

                if (tySym.SpecialType == SpecialType.System_String)
                    callExpr = Invocation(ResultTypeName.String, fmtArgs);
                else
                    callExpr = Invocation(ResultTypeName.FormattableString, fmtArgs);
            }

            return callExpr.WithTriviaFrom(node);
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
