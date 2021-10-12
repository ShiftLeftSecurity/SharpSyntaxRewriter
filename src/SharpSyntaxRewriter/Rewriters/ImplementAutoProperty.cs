// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using SharpSyntaxRewriter.Rewriters.Types;
using SharpSyntaxRewriter.Utilities;

namespace SharpSyntaxRewriter.Rewriters
{
    public class ImplementAutoProperty : SymbolicRewriter
    {
        public override string Name()
        {
            return "<implement auto-property>";
        }

        private class __BackingFieldInfo
        {
            public __BackingFieldInfo(string name,
                                      TypeSyntax tySpec,
                                      SyntaxTokenList modifiers,
                                      EqualsValueClauseSyntax equalsValInit = null)
            {
                Name = name;
                TySpec = tySpec;
                Modifiers = modifiers;
                EqualsValInit = equalsValInit;
            }

            public string Name;
            public TypeSyntax TySpec;
            public SyntaxTokenList Modifiers;
            public EqualsValueClauseSyntax EqualsValInit;
        }

        private readonly Stack<List<__BackingFieldInfo>> __ctx = new();

        public override SyntaxNode VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            return node;
        }

        private TypeDeclarationSyntax VisitTypeDeclaration<TypeDeclarationSyntaxT>(
                TypeDeclarationSyntaxT node,
                Func<TypeDeclarationSyntaxT, SyntaxNode> visit)
            where TypeDeclarationSyntaxT : TypeDeclarationSyntax
        {
            __ctx.Push(new List<__BackingFieldInfo>());
            var node_P = (TypeDeclarationSyntax)visit(node);
            var fldInfos = __ctx.Pop();

            var fldTbl = fldInfos.ToDictionary(fld => fld.Name);
            var fldDecls = new List<MemberDeclarationSyntax>();

            foreach (var membDecl in node_P.Members)
            {
                if (membDecl is not PropertyDeclarationSyntax propDecl)
                {
                    fldDecls.Add(membDecl);
                    continue;
                }

                var propName = SynthesizedFieldName(propDecl.Identifier.Text);
                if (fldTbl.ContainsKey(propName))
                {
                    var fldInfo = fldTbl[propName];
                    var fldDecl =
                        SyntaxFactory.FieldDeclaration(
                            SyntaxFactory.List<AttributeListSyntax>(),
                            SyntaxFactory.TokenList(
                                fldInfo.Modifiers.Where(
                                    m => m.Kind() != SyntaxKind.VirtualKeyword
                                         && m.Kind() != SyntaxKind.OverrideKeyword)),
                            SyntaxFactory.VariableDeclaration(
                                fldInfo.TySpec,
                                SyntaxFactory.SeparatedList(
                                    new List<VariableDeclaratorSyntax> {
                                SyntaxFactory.VariableDeclarator(
                                    SyntaxFactory.Identifier(fldInfo.Name),
                                    null,
                                    fldInfo.EqualsValInit)})));

                    fldDecls.Add(membDecl.WithoutTrailingTrivia());
                    fldDecls.Add(fldDecl.WithoutLeadingTrivia()
                                        .WithTrailingTrivia(membDecl.GetTrailingTrivia()));

                    // Remove the property from the table because, in the presence
                    // of interface- overriden ones, we'll see the property twice.
                    _ = fldTbl.Remove(propName);
                }
                else
                {
                    fldDecls.Add(membDecl);
                }
            }

            return node_P.WithMembers(SyntaxFactory.List(fldDecls));
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            return VisitTypeDeclaration(node,
                                        base.VisitClassDeclaration);
        }

        public override SyntaxNode VisitRecordDeclaration(RecordDeclarationSyntax node)
        {
            return VisitTypeDeclaration(node,
                                        base.VisitRecordDeclaration);
        }

        // In a `struct', it's required that fields are initialized... we must
        // artificially create corresponding initializers.
        private readonly Stack<List<StatementSyntax>> __struct = new();

        public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node)
        {
            __struct.Push(new List<StatementSyntax>());
            var node_P = VisitTypeDeclaration(node, n => base.VisitStructDeclaration((StructDeclarationSyntax)n));
            var stmts = __struct.Pop();

            if (stmts.Any())
            {
                var ctorDecls = node_P.DescendantNodes()
                                      .OfType<ConstructorDeclarationSyntax>();
                if (ctorDecls.Any())
                {
                    var ctorTbl =
                        new Dictionary<ConstructorDeclarationSyntax,
                                       ConstructorDeclarationSyntax>();
                    var ctorDecls_P = node_P.DescendantNodes()
                                            .OfType<ConstructorDeclarationSyntax>();
                    foreach (var ctorDecl in ctorDecls_P)
                    {
                        var ctorDecl_P = ctorDecl
                            .WithBody(
                                SyntaxFactory.Block(
                                    SyntaxFactory.List(stmts).AddRange(ctorDecl.Body.Statements)))
                            .WithTriviaFrom(ctorDecl);
                        ctorTbl.Add(ctorDecl, ctorDecl_P);
                    }
                    node_P = node_P.ReplaceNodes(ctorTbl.Keys, (n, _) => ctorTbl[n]);
                }
            }

            return node_P;
        }

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            if (ModifiersChecker.Has_abstract(node.Modifiers))
                return node;

            // Auto properties don't have expression body.
            if (node.ExpressionBody != null)
                return node;

            Debug.Assert(node.AccessorList != null);

            // Accessors of an auto property doen't have expression body.
            foreach (var access in node.AccessorList.Accessors)
            {
                if (access.Body != null || access.ExpressionBody != null)
                    return node;
            }

            var fldName = SynthesizedFieldName(node.Identifier.Text);
            var fldInfo = new __BackingFieldInfo(fldName, node.Type, node.Modifiers);
            __ctx.Peek().Add(fldInfo);

            if (node.Initializer != null)
            {
                fldInfo.EqualsValInit = node.Initializer;
                node = node.RemoveNode(node.Initializer,
                                       SyntaxRemoveOptions.KeepEndOfLine)
                           .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None))
                           .WithTrailingTrivia(node.SemicolonToken.TrailingTrivia);
            }
            else if (node.Parent is StructDeclarationSyntax)
            {
                Debug.Assert(__struct.Any());

                var initExpr =
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            SyntaxFactory.IdentifierName(fldName),
                            SyntaxFactory.DefaultExpression(node.Type)));
                __struct.Peek().Add(initExpr);
            }

            var acsorDecls = SyntaxFactory.AccessorList();
            foreach (var acsorDecl in node.AccessorList.Accessors)
            {
                AccessorDeclarationSyntax acsorDecl_P;
                switch (acsorDecl.Kind())
                {
                    case SyntaxKind.GetAccessorDeclaration:
                        acsorDecl_P =
                            SyntaxFactory.AccessorDeclaration(
                                SyntaxKind.GetAccessorDeclaration,
                                SyntaxFactory.Block(
                                    SyntaxFactory.ReturnStatement(
                                        SyntaxFactory.IdentifierName(fldName))));
                        break;

                    case SyntaxKind.SetAccessorDeclaration:
                        acsorDecl_P =
                            SyntaxFactory.AccessorDeclaration(
                                SyntaxKind.SetAccessorDeclaration,
                                SyntaxFactory.Block(
                                    SyntaxFactory.ExpressionStatement(
                                        SyntaxFactory.AssignmentExpression(
                                            SyntaxKind.SimpleAssignmentExpression,
                                            SyntaxFactory.IdentifierName(fldName),
                                            SyntaxFactory.IdentifierName("value")))));
                        break;

                    case SyntaxKind.InitAccessorDeclaration:
                        acsorDecl_P =
                            SyntaxFactory.AccessorDeclaration(
                                SyntaxKind.InitAccessorDeclaration,
                                SyntaxFactory.Block(
                                    SyntaxFactory.ExpressionStatement(
                                        SyntaxFactory.AssignmentExpression(
                                            SyntaxKind.SimpleAssignmentExpression,
                                            SyntaxFactory.IdentifierName(fldName),
                                            SyntaxFactory.IdentifierName("value")))));
                        break;

                    default:
                        continue;
                }
                Debug.Assert(acsorDecl_P != null);

                acsorDecls = acsorDecls.AddAccessors(
                    acsorDecl_P
                        .WithModifiers(acsorDecl.Modifiers)
                        .WithAttributeLists(acsorDecl.AttributeLists)
                        .WithTriviaFrom(acsorDecl))
                    .WithTriviaFrom(node.AccessorList)
                    .WithOpenBraceToken(node.AccessorList.OpenBraceToken)
                    .WithCloseBraceToken(node.AccessorList.CloseBraceToken);
            }

            return node.WithAccessorList(acsorDecls).WithTriviaFrom(node);
        }

        /*
         * See https://github.com/dotnet/csharplang/issues/2468 for the reason of this visit.
         */
        public override SyntaxNode VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            if (!node.Ancestors().OfType<ConstructorDeclarationSyntax>().Any())
                return node;

            var node_P = node.WithRight((ExpressionSyntax)node.Right.Accept(this));

            var lhsSym = _semaModel.GetSymbolInfo(node.Left).Symbol;
            if (lhsSym is IPropertySymbol propSym
                    && propSym.IsReadOnly
                    && propSym.DeclaringSyntaxReferences.Any())
            {
                return
                    node_P.WithLeft(
                        SyntaxFactory.IdentifierName(
                            SynthesizedFieldName(propSym.Name))
                        .WithTriviaFrom(node.Left));
            }

            return node_P;
        }

        public static string SynthesizedFieldName(string propName)
        {
            // We can't rewrite `<' and `>' in the source, so the spelled names.
            // (A tool may wish to "format" this at a later stage.)

            return "____LT____" + propName + "____GT____k_BackingField";
        }

        public override SyntaxNode VisitQueryExpression(QueryExpressionSyntax node)
        {
            return node;
        }
    }
}
