// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

using SharpSyntaxRewriter.Rewriters.Types;

namespace SharpSyntaxRewriter.Rewriters
{
    // WIP
    public class TranslateLinq : Rewriter
    {
        public override string Name()
        {
            return "<translante LINQ>";
        }

        public override SyntaxTree Apply(SyntaxTree tree)
        {
            var extractor = new __QueryContinuationExtractor();
            var node_P = extractor.Visit(tree.GetRoot());
            return Visit(node_P).SyntaxTree;
        }

        private readonly Stack<ExpressionSyntax> __ctx = new();

        private QueryExpressionSyntax PopAsQueryExpression()
        {
            var expr = __ctx.Pop();
            Debug.Assert(expr is QueryExpressionSyntax);
            return (QueryExpressionSyntax)expr;
        }

        private QueryExpressionSyntax PeekAsQueryExpression()
        {
            Debug.Assert(__ctx.Peek() is QueryExpressionSyntax);
            return (QueryExpressionSyntax)__ctx.Peek();
        }

        private readonly Dictionary<string, string> __substitutions = new();

        [Conditional("DEBUG_REWRITE")]
        private void DEBUG_SUBSTITUTIONS<NodeT>(NodeT node)
            where NodeT : SyntaxNode
        {
            Console.WriteLine($"\n[{node.Kind()}] {node}");
            foreach (var s in __substitutions)
                Console.WriteLine($"\t{s.Key} -> {s.Value}");
        }

        public override SyntaxNode VisitQueryExpression(QueryExpressionSyntax node)
        {
            DEBUG_SUBSTITUTIONS(node);

            __freshCnt = 0;
            __substitutions.Clear();

            __ctx.Push(node);
            base.VisitQueryExpression(node);
            var expr = __ctx.Pop();

            if (expr is InvocationExpressionSyntax callExpr)
            {
                var transInfo = new __TranslationInfo(
                        callExpr,
                        new Dictionary<string, string>(__substitutions));
                expr = __mapper.Apply(transInfo);
            }

            return expr;
        }

        public override SyntaxNode VisitQueryContinuation(QueryContinuationSyntax node)
        {
            Debug.Fail("unexpected");
            return node;
        }

        public override SyntaxNode VisitQueryBody(QueryBodySyntax node)
        {
            DEBUG_SUBSTITUTIONS(node);

            return base.VisitQueryBody(node);
        }

        public override SyntaxNode VisitFromClause(FromClauseSyntax node)
        {
            DEBUG_SUBSTITUTIONS(node);

            var nodeExpr_P = (ExpressionSyntax)node.Expression.Accept(this);
            var node_P = node.WithExpression(nodeExpr_P);

            if (node.Type != null)
            {
                node_P = node_P.RemoveNode(node.Type, SyntaxRemoveOptions.KeepNoTrivia);
                var castExpr =
                    SyntaxFactory.GenericName(
                        SyntaxFactory.Identifier("Cast"),
                        SyntaxFactory.TypeArgumentList(
                            SyntaxFactory.SeparatedList<TypeSyntax>().Add(
                                SyntaxFactory.ParseTypeName(node.Type.ToString()))));
                nodeExpr_P =
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            nodeExpr_P,
                            castExpr),
                        SyntaxFactory.ArgumentList());
                node_P = node_P.WithExpression(nodeExpr_P);
            }

            var qryExpr = PeekAsQueryExpression();

            if (node != qryExpr.FromClause
                        && qryExpr.Body.Clauses.Any()
                        && qryExpr.Body.Clauses.First().Kind() == SyntaxKind.FromClause
                        && (qryExpr.Body.Clauses.Count > 1
                            || qryExpr.Body.SelectOrGroup.Kind() != SyntaxKind.SelectClause))
            {
                var ident = FreshIdentifier();
                var identTk = SyntaxFactory.Identifier(ident);
                var nexExpr = AnonymousObjectCreation(
                        ident,
                        qryExpr.FromClause.Identifier.Text,
                        node.Identifier.Text);
                var arg0 = LambdaArgument(qryExpr.FromClause.Identifier.Text, nodeExpr_P);
                var arg1 = LambdaArgument(qryExpr.FromClause.Identifier.Text,
                                          node_P.Identifier.Text,
                                          nexExpr);
                ReplaceQuery(node, "SelectMany", identTk, arg0, arg1);

                return node_P;
            }

            qryExpr = PopAsQueryExpression();
            qryExpr = qryExpr.ReplaceNode(node, node_P);
            __ctx.Push(qryExpr);

            return node_P;
        }

        public override SyntaxNode VisitJoinClause(JoinClauseSyntax node)
        {
            DEBUG_SUBSTITUTIONS(node);

            var inExpr_P = (ExpressionSyntax)node.InExpression.Accept(this);
            var lhsExpr_P = (ExpressionSyntax)node.LeftExpression.Accept(this);
            var rhsExpr_P = (ExpressionSyntax)node.RightExpression.Accept(this);
            var node_P = node.WithInExpression(inExpr_P)
                             .WithLeftExpression(lhsExpr_P)
                             .WithRightExpression(rhsExpr_P);

            var qryExpr = PeekAsQueryExpression();
            if (qryExpr.Body.Clauses.Last() != node
                    || qryExpr.Body.SelectOrGroup.Kind() != SyntaxKind.SelectClause)
            {
                string methName;
                string parmName;
                if (node_P.Into == null)
                {
                    methName = "Join";
                    parmName = node_P.Identifier.Text;
                }
                else
                {
                    methName = "GroupJoin";
                    parmName = node_P.Into.Identifier.Text;
                }

                var ident = FreshIdentifier();
                var identTk = SyntaxFactory.Identifier(ident);
                var arg0 = SyntaxFactory.Argument(inExpr_P);
                var arg1 = LambdaArgument(qryExpr.FromClause.Identifier.Text, lhsExpr_P);
                var arg2 = LambdaArgument(node_P.Identifier.Text, rhsExpr_P);
                var nexExpr = AnonymousObjectCreation(
                        ident,
                        qryExpr.FromClause.Identifier.Text,
                        parmName);
                var arg3 = LambdaArgument(qryExpr.FromClause.Identifier.Text,
                                          parmName,
                                          nexExpr);
                ReplaceQuery(node, methName, identTk, arg0, arg1, arg2, arg3);

                return node_P;
            }

            return node_P;
        }

        public override SyntaxNode VisitJoinIntoClause(JoinIntoClauseSyntax node)
        {
            DEBUG_SUBSTITUTIONS(node);

            // The case `join ... into' is handled inside regular `join' clause.
            return base.VisitJoinIntoClause(node);
        }

        public override SyntaxNode VisitLetClause(LetClauseSyntax node)
        {
            DEBUG_SUBSTITUTIONS(node);

            var nodeExpr_P = (ExpressionSyntax)node.Expression.Accept(this);
            var node_P = node.WithExpression(nodeExpr_P);

            var qryExpr = PeekAsQueryExpression();
            var ident = FreshIdentifier();
            var identTk = SyntaxFactory.Identifier(ident);
            var nexExpr = AnonymousObjectCreation(
                    ident,
                    qryExpr.FromClause.Identifier.Text,
                    node.Identifier.Text,
                    nodeExpr_P);
            var arg0 = LambdaArgument(qryExpr.FromClause.Identifier.Text,
                                      nexExpr);
            ReplaceQuery(node, "Select", identTk, arg0);

            return node_P;
        }

        public override SyntaxNode VisitWhereClause(WhereClauseSyntax node)
        {
            DEBUG_SUBSTITUTIONS(node);

            var nodeCond_P = (ExpressionSyntax)node.Condition.Accept(this);
            var node_P = node.WithCondition(nodeCond_P);

            var qryExpr = PeekAsQueryExpression();
            var arg0 = LambdaArgument(qryExpr.FromClause.Identifier.Text, nodeCond_P);
            ReplaceQuery(node, "Where", PeekAsQueryExpression().FromClause.Identifier, arg0);

            return node_P;
        }

        public override SyntaxNode VisitOrderByClause(OrderByClauseSyntax node)
        {
            DEBUG_SUBSTITUTIONS(node);

            var qryExpr = PopAsQueryExpression();

            var methName = "OrderBy";
            ExpressionSyntax callExpr = qryExpr.FromClause.Expression;
            foreach (var ord in node.Orderings)
            {
                var ordExpr_P = (ExpressionSyntax)ord.Expression.Accept(this);
                var arg = LambdaArgument(qryExpr.FromClause.Identifier.Text, ordExpr_P);
                if (ord.AscendingOrDescendingKeyword.Kind() == SyntaxKind.DescendingKeyword)
                    methName += "Descending";
                callExpr = Invocation(callExpr, methName, arg);
                methName = "ThenBy";
            }

            qryExpr = qryExpr.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
            qryExpr = qryExpr.ReplaceNode(qryExpr.FromClause.Expression, callExpr);

            __ctx.Push(qryExpr);

            return base.VisitOrderByClause(node);
        }

        public override SyntaxNode VisitSelectClause(SelectClauseSyntax node)
        {
            DEBUG_SUBSTITUTIONS(node);

            var nodeExpr_P = (ExpressionSyntax)node.Expression.Accept(this);

            var qryExpr = PopAsQueryExpression();
            ExpressionSyntax callExpr;
            if (qryExpr.Body.Clauses.Count == 1)
            {
                var fstClause = qryExpr.Body.Clauses.First();
                if (fstClause is FromClauseSyntax fromClause)
                {
                    var arg0 = LambdaArgument(qryExpr.FromClause.Identifier.Text,
                                              fromClause.Expression);
                    var arg1 = LambdaArgument(qryExpr.FromClause.Identifier.Text,
                                              fromClause.Identifier.Text,
                                              nodeExpr_P);
                    callExpr = Invocation(qryExpr.FromClause.Expression,
                                          "SelectMany",
                                          arg0, arg1);
                }
                else if (fstClause is JoinClauseSyntax joinClause)
                {
                    string methName;
                    string parmName;
                    if (joinClause.Into == null)
                    {
                        parmName = joinClause.Identifier.Text;
                        methName = "Join";
                    }
                    else
                    {
                        parmName = joinClause.Into.Identifier.Text;
                        methName = "GroupJoin";
                    }

                    var arg0 = LambdaArgument(qryExpr.FromClause.Identifier.Text,
                                              joinClause.LeftExpression);
                    var arg1 = LambdaArgument(joinClause.Identifier.Text,
                                              joinClause.RightExpression);
                    var arg2 = LambdaArgument(qryExpr.FromClause.Identifier.Text,
                                              parmName,
                                              nodeExpr_P);
                    callExpr = Invocation(qryExpr.FromClause.Expression,
                                          methName,
                                          SyntaxFactory.Argument(joinClause.InExpression),
                                          arg0, arg1, arg2);
                }
                else
                {
                    var arg = LambdaArgument(qryExpr.FromClause.Identifier.Text, nodeExpr_P);
                    callExpr = Invocation(qryExpr.FromClause.Expression, "Select", arg);
                }
            }
            else if (nodeExpr_P is IdentifierNameSyntax)
            {
                if (qryExpr.FromClause.Expression is IdentifierNameSyntax
                        && !qryExpr.Body.Clauses.Any())
                {
                    // The degenerate case.
                    var arg = LambdaArgument(qryExpr.FromClause.Identifier.Text, nodeExpr_P);
                    callExpr = Invocation(qryExpr.FromClause.Expression, "Select", arg);
                }
                else
                {
                    callExpr = qryExpr.FromClause.Expression;
                }
            }
            else
            {
                var arg = LambdaArgument(qryExpr.FromClause.Identifier.Text, nodeExpr_P);
                callExpr = Invocation(qryExpr.FromClause.Expression, "Select", arg);
            }

            __ctx.Push(callExpr);

            return node.WithExpression(nodeExpr_P);
        }

        public override SyntaxNode VisitGroupClause(GroupClauseSyntax node)
        {
            DEBUG_SUBSTITUTIONS(node);

            var nodeGrpExpr_P = (ExpressionSyntax)node.GroupExpression.Accept(this);
            var nodeByExpr_P = (ExpressionSyntax)node.ByExpression.Accept(this);

            var qryExpr = PopAsQueryExpression();
            ExpressionSyntax callExpr;
            if (nodeGrpExpr_P is IdentifierNameSyntax ident
                    && ident.ToString() == qryExpr.FromClause.Identifier.Text)
            {
                var arg = LambdaArgument(qryExpr.FromClause.Identifier.Text, nodeByExpr_P);
                callExpr = Invocation(qryExpr.FromClause.Expression,
                                      "GroupBy",
                                      arg);
            }
            else
            {
                var arg0 = LambdaArgument(qryExpr.FromClause.Identifier.Text, nodeByExpr_P);
                var arg1 = LambdaArgument(qryExpr.FromClause.Identifier.Text, nodeGrpExpr_P);
                callExpr = Invocation(qryExpr.FromClause.Expression,
                                      "GroupBy",
                                      arg0, arg1);
            }

            __ctx.Push(callExpr);

            return node;
        }

        public void ReplaceQuery(SyntaxNode node,
                                 string methName,
                                 SyntaxToken identTk,
                                 params ArgumentSyntax[] args)
        {
            var qryExpr = PopAsQueryExpression();

            var callExpr = Invocation(qryExpr.FromClause.Expression, methName, args);
            qryExpr = qryExpr.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
            qryExpr = qryExpr.ReplaceNode(qryExpr.FromClause.Expression, callExpr);
            qryExpr = qryExpr.ReplaceToken(qryExpr.FromClause.Identifier, identTk);

            __ctx.Push(qryExpr);
        }

        private static ArgumentSyntax LambdaArgument(string parmName,
                                                    ExpressionSyntax bodyExpr)
        {
            return SyntaxFactory.Argument(
                       SyntaxFactory.SimpleLambdaExpression(
                           SyntaxFactory.Parameter(SyntaxFactory.Identifier(parmName)),
                           bodyExpr));
        }

        private static ArgumentSyntax LambdaArgument(
                string parm0Name,
                string parm1Name,
                ExpressionSyntax bodyExpr)
        {
            return
                SyntaxFactory.Argument(
                    SyntaxFactory.ParenthesizedLambdaExpression(
                        SyntaxFactory.ParameterList(
                            SyntaxFactory.SeparatedList(new List<ParameterSyntax> {
                                    SyntaxFactory.Parameter(SyntaxFactory.Identifier(parm0Name)),
                                    SyntaxFactory.Parameter(SyntaxFactory.Identifier(parm1Name))})),
                    bodyExpr));
        }

        private static InvocationExpressionSyntax Invocation(
                ExpressionSyntax baseExpr,
                string identName,
                params ArgumentSyntax[] args)
        {
            return SyntaxFactory.InvocationExpression(
                       SyntaxFactory.MemberAccessExpression(
                           SyntaxKind.SimpleMemberAccessExpression,
                           baseExpr,
                           SyntaxFactory.Token(SyntaxKind.DotToken),
                           SyntaxFactory.IdentifierName(identName)),
                       SyntaxFactory.ArgumentList().AddArguments(args));
        }

        private AnonymousObjectCreationExpressionSyntax AnonymousObjectCreation(
                string identName,
                string memb1Name,
                string memb2Name,
                ExpressionSyntax expr = null)
        {
            __substitutions.Add(memb1Name, identName);
            __substitutions.Add(memb2Name, identName);

            var membDecltor =
                    expr == null
                        ? SyntaxFactory.AnonymousObjectMemberDeclarator(
                                SyntaxFactory.IdentifierName(memb2Name))
                        : SyntaxFactory.AnonymousObjectMemberDeclarator(
                                SyntaxFactory.NameEquals(
                                    SyntaxFactory.IdentifierName(memb2Name)),
                                expr);

            var newExpr =
                SyntaxFactory.AnonymousObjectCreationExpression(
                    SyntaxFactory.SeparatedList(
                        new List<AnonymousObjectMemberDeclaratorSyntax> {
                        SyntaxFactory.AnonymousObjectMemberDeclarator(
                            SyntaxFactory.IdentifierName(memb1Name)),
                        membDecltor }));

            return newExpr;
        }

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            return node.WithBody((BlockSyntax)node.Body.Accept(this));
        }

        private class __QueryContinuationExtractor : CSharpSyntaxRewriter
        {
            private readonly Stack<QueryExpressionSyntax> __ctx = new();

            public override SyntaxNode VisitQueryExpression(QueryExpressionSyntax node)
            {
                __ctx.Push(node);
                _ = base.VisitQueryExpression(node);
                return __ctx.Pop();
            }

            public override SyntaxNode VisitQueryContinuation(QueryContinuationSyntax node)
            {
                var origQryExpr = __ctx.Pop();

                origQryExpr = origQryExpr.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
                var nodeBody_P = (QueryBodySyntax)node.Body.Accept(this);
                origQryExpr = origQryExpr.ReplaceNode(
                    node.Body,
                    node.Body.AddClauses(nodeBody_P.Clauses.ToArray()));

                var modQryExpr =
                    SyntaxFactory.QueryExpression(
                        SyntaxFactory.FromClause(
                            node.Identifier,
                            SyntaxFactory.ParenthesizedExpression(origQryExpr)),
                        node.Body);

                __ctx.Push(modQryExpr);

                return null;
            }
        }

        private struct __TranslationInfo
        {
            public __TranslationInfo(InvocationExpressionSyntax callExpr,
                                     Dictionary<string, string> identMap)
            {
                CallExpr = callExpr;
                IdentMap = identMap;
            }

            internal InvocationExpressionSyntax CallExpr;
            internal Dictionary<string, string> IdentMap;
        }

        private const string IDENT_PREFIX = "____TRANSPARENT";
        private int __freshCnt;
        private string FreshIdentifier()
        {
            var name = IDENT_PREFIX + __freshCnt;
            ++__freshCnt;
            return name;
        }

        private readonly __TransparentIdentifierMapper __mapper = new();

        private class __TransparentIdentifierMapper : CSharpSyntaxRewriter
        {
            private Dictionary<string, string> __identMap;

            private readonly HashSet<string> __parmsTbl = new();

            public InvocationExpressionSyntax Apply(__TranslationInfo transInfo)
            {
                __identMap = transInfo.IdentMap;

                return (InvocationExpressionSyntax)transInfo.CallExpr.Accept(this);
            }

            public override SyntaxNode VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
            {
                _ = __parmsTbl.Add(node.Parameter.Identifier.Text);
                var nodeBody_P = (CSharpSyntaxNode)node.Body.Accept(this);
                __parmsTbl.Clear();

                return node.WithBody(nodeBody_P);
            }

            public override SyntaxNode VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
            {
                foreach (var p in node.ParameterList.Parameters)
                    _ = __parmsTbl.Add(p.Identifier.Text);
                var nodeBody_P = (CSharpSyntaxNode)node.Body.Accept(this);
                __parmsTbl.Clear();

                return node.WithBody(nodeBody_P);
            }

            public override SyntaxNode VisitAnonymousObjectCreationExpression(AnonymousObjectCreationExpressionSyntax node)
            {
                var initDecltors = SyntaxFactory.SeparatedList<AnonymousObjectMemberDeclaratorSyntax>();
                foreach (var initDecltor in node.Initializers)
                {
                    var initExpr_P = (ExpressionSyntax)initDecltor.Expression.Accept(this);
                    initDecltors = initDecltors.Add(initDecltor.WithExpression(initExpr_P));
                }

                return node.WithInitializers(initDecltors);
            }

            public override SyntaxNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
            {
                var nodeExpr_P = (ExpressionSyntax)node.Expression.Accept(this);

                return node.WithExpression(nodeExpr_P);
            }

            public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
            {
                var ident = node.Identifier.Text;
                if (ident.StartsWith(IDENT_PREFIX, StringComparison.Ordinal)
                        || !__identMap.ContainsKey(ident)
                        || __parmsTbl.Contains(ident))
                {
                    return node;
                }

                var knownIdents = new HashSet<string>();
                while (!knownIdents.Contains(ident)
                            && __identMap.ContainsKey(ident)
                            && !__identMap[ident].StartsWith(IDENT_PREFIX, StringComparison.Ordinal))
                {
                    ident = __identMap[ident];
                }

                Func<ExpressionSyntax, string, ExpressionSyntax> patch = (expr, name) =>
                {
                    return SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                expr,
                                SyntaxFactory.IdentifierName(name));
                };

                var parms = __parmsTbl.Where(p => p.StartsWith(IDENT_PREFIX, StringComparison.Ordinal));
                var useLevel = TransparencyLevel(parms.First());
                var defLevel = TransparencyLevel(__identMap[ident]);

                ExpressionSyntax identExpr =
                    SyntaxFactory.IdentifierName(IDENT_PREFIX + useLevel);
                for (int i = useLevel; i > defLevel; --i)
                    identExpr = patch(identExpr, IDENT_PREFIX + (i - 1));
                identExpr = patch(identExpr, ident);

                return identExpr;
            }

            private static int TransparencyLevel(string ident)
            {
                Debug.Assert(ident.StartsWith(IDENT_PREFIX, StringComparison.Ordinal));

                string level;
                var dotPos = ident.IndexOf('.');
                level = dotPos < 0
                        ? ident.Substring(IDENT_PREFIX.Length)
                        : ident.Substring(IDENT_PREFIX.Length, dotPos - IDENT_PREFIX.Length);

                return int.Parse(level, CultureInfo.InvariantCulture);
            }
        }
    }
}
