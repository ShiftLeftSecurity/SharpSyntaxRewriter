// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Linq;
using System.Diagnostics;

using SharpSyntaxRewriter.Rewriters.Types;
using SharpSyntaxRewriter.Rewriters.Helpers;

namespace SharpSyntaxRewriter.Rewriters
{
    public class UncoalesceCoalescedNull : SymbolicRewriter
    {
        public override string Name()
        {
            return "<uncoalesce coalesced `null'>";
        }

        [Conditional("DEBUG_REWRITE")]
        private void DEBUG_OPERAND(string oprnd, SyntaxNode node, ITypeSymbol tySym)
        {
            if (tySym == null)
            {
                Console.WriteLine($"\n{Name()}\n\t{oprnd} type is `null'\n");
                return;
            }

            Console.WriteLine($"\n{Name()}\n" +
                              $"\t{oprnd} node: {node}\n" +
                              $"\t        type: {tySym}\n" +
                              $"\t            : {tySym.TypeKind}\n" +
                              $"\t            : {tySym.SpecialType}\n" +
                              $"\t            : " + (tySym.IsReferenceType ? "reference-type\n" : "value-type\n"));
        }

        [Conditional("DEBUG_REWRITE")]
        private void DEBUG_CONVERSION(string desc, CommonConversion conv)
        {
            Console.WriteLine($"\n{Name()}\n\t{desc} is `null'\n" +
                              $"\t\texists  : {conv.Exists}\n" +
                              $"\t\tidentity: {conv.IsIdentity}\n" +
                              $"\t\timplicit: {conv.IsImplicit}\n" +
                              $"\t\tuser-def: {conv.IsUserDefined}\n" +
                              $"\t\tref-kind: {conv.IsReference}\n" +
                              $"\t\tmethod  : {conv.MethodSymbol?.Name}\n" +
                              $"\t\tnumeric : {conv.IsNumeric}");
        }

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            return node.WithBody((BlockSyntax)node.Body.Accept(this));
        }

        public override SyntaxNode VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            var node_P = node.WithLeft((ExpressionSyntax)node.Left.Accept(this))
                             .WithRight((ExpressionSyntax)node.Right.Accept(this));

            if (node_P.Kind() != SyntaxKind.CoalesceExpression)
                return node_P;

            var lhsTySym = _semaModel.GetTypeInfo(node.Left).ConvertedType;
            var lhsNode = node_P.Left;

            // Observe the subtlety: a non-implicitly convertible type is allowed
            // on the RHS of a null coalescing expression (since it's the first
            // operand that determines the result type), but not on the RHS
            // of a ternary operation. That's why we obtain the actual RHS type,
            // instead of converted one.
            var rhsTySym = _semaModel.GetTypeInfo(node.Right).Type;
            var rhsNode = node_P.Right;

            var resTySym = _semaModel.GetTypeInfo(node).ConvertedType;

            DEBUG_OPERAND("LHS", lhsNode, lhsTySym);
            DEBUG_OPERAND("RHS", rhsNode, rhsTySym);

            var lhsTySym_P = ImplicitlyConvertibleType(lhsTySym, resTySym);
            if (lhsTySym_P == null)
            {
                lhsNode = ExplicitCast(lhsNode, resTySym);
            }
            else if (lhsTySym_P.Name != lhsTySym.Name)
            {
                lhsNode =
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        lhsNode.WithoutTrivia(),
                        SyntaxFactory.IdentifierName("Value"));
            }

            if (rhsTySym != null)
            {
                // The RHS might may not type (e.g., when it's a <throw expression>).
                var rhsTySym_P = ImplicitlyConvertibleType(rhsTySym, resTySym);
                if (rhsTySym_P == null)
                {
                    rhsNode = ExplicitCast(rhsNode, resTySym);
                }
                else if (lhsTySym_P != null)
                {
                    var conv =
                        _semaModel.Compilation
                                  .ClassifyCommonConversion(rhsTySym_P,
                                                            lhsTySym_P);
                    if (!conv.IsIdentity && conv.MethodSymbol != null)
                        rhsNode = ExplicitCast(rhsNode, lhsTySym_P);

                    DEBUG_CONVERSION($"conversion RHS->LHS: {rhsTySym_P} to {lhsTySym_P}", conv);
                }
            }

            var compExpr =
                SyntaxFactory.BinaryExpression(
                        SyntaxKind.NotEqualsExpression,
                        node_P.Left.WithoutTrivia(), // The left node without a possible `Value' suffix.
                        UnambiguousComparisonRHS(lhsNode, lhsTySym, lhsTySym_P))
                .WithLeadingTrivia(node.Left.GetLeadingTrivia())
                .WithTrailingTrivia(
                    node.Left.GetTrailingTrivia().AddRange(
                        node.OperatorToken.GetAllTrivia()));

            return SyntaxFactory.ConditionalExpression(
                        compExpr,
                        (ExpressionSyntax)RemoveEveryTrivia__.Go(lhsNode),
                        rhsNode.WithTriviaFrom(node.Right));
        }

        private CastExpressionSyntax ExplicitCast(ExpressionSyntax node,
                                                  ITypeSymbol tySym,
                                                  ExpressionSyntax baseNode = null)
        {
            var tyName = tySym.ToMinimalDisplayString(
                _semaModel,
                baseNode != null ? baseNode.SpanStart : node.SpanStart);

            return
                SyntaxFactory.CastExpression(
                    SyntaxFactory.ParseTypeName(tyName),
                    node);
        }

        private ITypeSymbol ImplicitlyConvertibleType(ITypeSymbol oprndTySym,
                                                      ITypeSymbol resTySym)
        {
            var conv = _semaModel.Compilation
                                 .ClassifyCommonConversion(oprndTySym, resTySym);

            DEBUG_CONVERSION($"conversion: {oprndTySym} to {resTySym}", conv);

            if (conv.IsImplicit)
                return oprndTySym;

            Debug.Assert(conv.Exists);

            if (oprndTySym is INamedTypeSymbol namedTySym
                    && namedTySym.OriginalDefinition != null
                    && namedTySym.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
#if DEBUG_REWRITE
                Console.WriteLine($"\ttype is `System.Nullable<{namedTySym.TypeArguments.First()}>'");
#endif

                oprndTySym = namedTySym.TypeArguments.First();
                return ImplicitlyConvertibleType(oprndTySym, resTySym);
            }

            // TODO: Handle user-defined conversions.

            return null;
        }

        private ExpressionSyntax UnambiguousComparisonRHS(ExpressionSyntax node,
                                                          ITypeSymbol exactTySym,
                                                          ITypeSymbol convTySym)
        {
            bool maybeAmbig = false;
            var tySym = convTySym ?? exactTySym;
            foreach (var membTySym in tySym.GetMembers())
            {
                if (membTySym is IMethodSymbol methSym
                        && methSym.MethodKind == MethodKind.UserDefinedOperator
                        && methSym.MetadataName == WellKnownMemberNames.InequalityOperatorName)
                {
                    maybeAmbig = true;
                    break;
                }
            }

            ExpressionSyntax rhsNode =
                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
            if (maybeAmbig)
                rhsNode = ExplicitCast(rhsNode, exactTySym, node);

            return rhsNode;
        }

        public override SyntaxNode VisitQueryExpression(QueryExpressionSyntax node)
        {
            return node;
        }
    }
}
