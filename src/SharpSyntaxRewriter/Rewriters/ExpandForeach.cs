// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;

using SharpSyntaxRewriter.Extensions;
using SharpSyntaxRewriter.Rewriters.Types;

namespace SharpSyntaxRewriter.Rewriters
{
    public class ExpandForeach : SymbolicRewriter
    {
        public override string Name()
        {
            return "<expand `foreach'>";
        }

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            return node.WithBody((BlockSyntax)node.Body.Accept(this));
        }

        public SyntaxNode VisitCommonForEachStatementSyntax(CommonForEachStatementSyntax node,
                                                            ITypeSymbol collectionTySym,
                                                            StatementSyntax stmt)
        {
            if (collectionTySym is IDynamicTypeSymbol)
            {
                collectionTySym = _semaModel.Compilation.GetSpecialType(
                    SpecialType.System_Collections_IEnumerable);
            }

            IdentifierNameSyntax nameNode_GetEnumerator;
            ExpressionSyntax exprNode_MoveNext;
            if (node.AwaitKeyword.Value == null)
            {
                nameNode_GetEnumerator =
                    SyntaxFactory.IdentifierName("GetEnumerator");
                exprNode_MoveNext =
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName(IterVarName(node)),
                                SyntaxFactory.Token(SyntaxKind.DotToken),
                                SyntaxFactory.IdentifierName("MoveNext")));
            }
            else
            {
                nameNode_GetEnumerator =
                    SyntaxFactory.IdentifierName("GetAsyncEnumerator");
                exprNode_MoveNext =
                    SyntaxFactory.AwaitExpression(
                        SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName(IterVarName(node)),
                                SyntaxFactory.Token(SyntaxKind.DotToken),
                                SyntaxFactory.IdentifierName("MoveNextAsync"))));
            }

            var enumDecl = SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.IdentifierName("var"),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(
                            SyntaxFactory.Identifier(IterVarName(node)),
                            null,
                            SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.Token(SyntaxKind.EqualsToken),
                                SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.ParenthesizedExpression(
                                            SyntaxFactory.CastExpression(
                                                SyntaxFactory.ParseTypeName(
                                                    collectionTySym.ContextualDisplay(
                                                        node.SpanStart, _semaModel)),
                                                node.Expression)),
                                        SyntaxFactory.Token(SyntaxKind.DotToken),
                                        nameNode_GetEnumerator)))))));

            var node_P = (StatementSyntax)node.Statement.Accept(this);

            var whileStmt =
                SyntaxFactory.WhileStatement(
                    SyntaxFactory.Token(SyntaxKind.WhileKeyword),
                    SyntaxFactory.Token(SyntaxKind.OpenParenToken),
                    exprNode_MoveNext,
                    SyntaxFactory.Token(SyntaxKind.CloseParenToken),
                    SyntaxFactory.Block(
                        stmt.WithTrailingTrivia(
                            SyntaxFactory.TriviaList()
                                .AddRange(node.CloseParenToken.TrailingTrivia)
                                .AddRange(node.Statement.GetLeadingTrivia())),
                        node_P.WithoutTrailingTrivia()));

            var blockNode =
                SyntaxFactory.Block(enumDecl, whileStmt)
                    .WithLeadingTrivia(
                        SyntaxFactory.TriviaList().AddRange(node.GetLeadingTrivia()))
                    .WithTrailingTrivia(node_P.GetTrailingTrivia());

            return blockNode;
        }

        public override SyntaxNode VisitForEachStatement(ForEachStatementSyntax node)
        {
            var foreachInfo = _semaModel.GetForEachStatementInfo(node);
            var castTySym = _semaModel.GetTypeInfo(node.Type).Type;
            var declStmt =
                SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(
                        node.Type,
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(
                                node.Identifier,
                                null,
                                SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.Token(SyntaxKind.EqualsToken),
                                    SyntaxFactory.CastExpression(
                                        SyntaxFactory.ParseTypeName(
                                            castTySym.ContextualDisplay(node.SpanStart, _semaModel)),
                                        SyntaxFactory.CastExpression(
                                            SyntaxFactory.ParseTypeName(
                                                foreachInfo.ElementType
                                                           .ContextualDisplay(node.SpanStart, _semaModel)),
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.IdentifierName(IterVarName(node)),
                                                SyntaxFactory.Token(SyntaxKind.DotToken),
                                                SyntaxFactory.IdentifierName("Current")))))))));

            return VisitCommonForEachStatementSyntax(
                        node,
                        foreachInfo.GetEnumeratorMethod.ContainingType,
                        declStmt);
        }

        public override SyntaxNode VisitForEachVariableStatement(ForEachVariableStatementSyntax node)
            // This is a weirdly named AST node, see: https://github.com/dotnet/roslyn/issues/35809
        {
            var foreachInfo = _semaModel.GetForEachStatementInfo(node);
            var exprStmt =
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        node.Variable,
                        // The "double cast" isn't necessary in deconstruction, see:
                        // https://github.com/dotnet/roslyn/issues/35880
                        SyntaxFactory.CastExpression(
                            SyntaxFactory.ParseTypeName(
                                foreachInfo.ElementType
                                           .ContextualDisplay(node.SpanStart, _semaModel)),
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName(IterVarName(node)),
                                SyntaxFactory.Token(SyntaxKind.DotToken),
                                SyntaxFactory.IdentifierName("Current")))));

            return VisitCommonForEachStatementSyntax(
                        node,
                        foreachInfo.GetEnumeratorMethod.ContainingType,
                        exprStmt);
        }

        private static string IterVarName(CommonForEachStatementSyntax node)
        {
            Debug.Assert(node != null);

            var linePos = node.GetLocation().GetLineSpan().StartLinePosition;
            // In the specification, the implicit enumerator variable is `e';
            // making it similar here to faciliate the analogy.
            return "e_L" + linePos.Line + "C" + linePos.Character;
        }

        public override SyntaxNode VisitQueryExpression(QueryExpressionSyntax node)
        {
            return node;
        }
    }
}
