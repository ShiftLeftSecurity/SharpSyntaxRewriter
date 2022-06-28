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
        public const string ID = "<implement auto-property>";

        public override string Name()
        {
            return ID;
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

        // In a `struct', fields must be initialized in the constructor.
        private readonly Stack<List<StatementSyntax>> __initStmts = new();

        public override SyntaxNode VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            return node;
        }

        private TypeDeclarationSyntax VisitTypeDeclaration<TypeDeclarationT>(
                TypeDeclarationT node,
                Func<TypeDeclarationT, SyntaxNode> visit)
            where TypeDeclarationT : TypeDeclarationSyntax
        {
            if (node.IsKind(SyntaxKind.StructDeclaration)
                    || node.IsKind(SyntaxKind.RecordStructDeclaration))
            {
                __initStmts.Push(new List<StatementSyntax>());
            }

            __ctx.Push(new List<__BackingFieldInfo>());
            var node_P = (TypeDeclarationSyntax)visit(node);
            var fldInfos = __ctx.Pop();

            List<StatementSyntax> stmts = null;
            if (node.IsKind(SyntaxKind.StructDeclaration)
                   || node.IsKind(SyntaxKind.RecordStructDeclaration))
            {
                stmts = __initStmts.Pop();
            }

            var fldNamesTbl = fldInfos.ToDictionary(fld => fld.Name);
            var fldDecls = new List<MemberDeclarationSyntax>();

            foreach (var membDecl in node_P.Members)
            {
                if (membDecl is not PropertyDeclarationSyntax propDecl)
                {
                    fldDecls.Add(membDecl);
                    continue;
                }

                var propName = SynthesizedFieldName(propDecl.Identifier.Text);
                if (fldNamesTbl.ContainsKey(propName))
                {
                    var fldInfo = fldNamesTbl[propName];
                    var fldDecl =
                        SyntaxFactory.FieldDeclaration(
                            SyntaxFactory.List<AttributeListSyntax>(),
                            SyntaxFactory.TokenList(
                                fldInfo.Modifiers.Where(
                                    mod => !mod.IsKind(SyntaxKind.VirtualKeyword)
                                           && !mod.IsKind(SyntaxKind.OverrideKeyword))),
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
                    // of interface-overriden ones, we'll see the property twice.
                    _ = fldNamesTbl.Remove(propName);
                }
                else
                {
                    fldDecls.Add(membDecl);
                }
            }

            node_P = node_P.WithMembers(SyntaxFactory.List(fldDecls));

            if (stmts != null && stmts.Any())
            {
                var ctorDecls = node_P.DescendantNodes()
                                      .OfType<ConstructorDeclarationSyntax>();
                if (ctorDecls.Any())
                {
                    var ctorTbl = new Dictionary<
                            ConstructorDeclarationSyntax,
                            ConstructorDeclarationSyntax>();
                    var ctorDecls_P = ctorDecls;
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

        public override SyntaxNode VisitRecordDeclaration(RecordDeclarationSyntax node)
        {
            return VisitTypeDeclaration(node,
                                        base.VisitRecordDeclaration);
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            return VisitTypeDeclaration(node,
                                        base.VisitClassDeclaration);
        }

        public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node)
        {
            return VisitTypeDeclaration(node,
                                        base.VisitStructDeclaration);
        }

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            if (ModifiersChecker.Has_abstract(node.Modifiers))
                return node;

            // Auto properties don't have an expression body.
            if (node.ExpressionBody != null)
                return node;

            Debug.Assert(node.AccessorList != null);

            // An accessor of an auto property doesn't have an expression body.
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
                Debug.Assert(!(node.Parent.IsKind(SyntaxKind.StructDeclaration)
                                || node.Parent.IsKind(SyntaxKind.RecordStructDeclaration)));

                fldInfo.EqualsValInit = node.Initializer;
                node = node.RemoveNode(node.Initializer,
                                       SyntaxRemoveOptions.KeepEndOfLine)
                           .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None))
                           .WithTrailingTrivia(node.SemicolonToken.TrailingTrivia);
            }
            else if (node.Parent.IsKind(SyntaxKind.StructDeclaration)
                        || node.Parent.IsKind(SyntaxKind.RecordStructDeclaration))
            {
                Debug.Assert(__initStmts.Any());

                var initExpr =
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            SyntaxFactory.IdentifierName(fldName),
                            SyntaxFactory.DefaultExpression(node.Type)));
                __initStmts.Peek().Add(initExpr);

                var tyDecl = (TypeDeclarationSyntax)node.Parent;
                if (ModifiersChecker.Has_readonly(tyDecl.Modifiers)
                        && !ModifiersChecker.Has_readonly(fldInfo.Modifiers))
                {
                    fldInfo.Modifiers =
                        fldInfo.Modifiers.Add(
                            SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));
                }
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
        private ITypeSymbol __tySym;
        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            __tySym = _semaModel.GetDeclaredSymbol(node)?.ContainingType;
            var node_P = base.VisitConstructorDeclaration(node);
            __tySym = null;

            return node_P;
        }

        public override SyntaxNode VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            if (__tySym == null)
                return node;

            var node_P = node.WithRight((ExpressionSyntax)node.Right.Accept(this));

            var lhsSym = _semaModel.GetSymbolInfo(node.Left).Symbol;
            var chosenExpr = __ChooseBetweenPropertyOrFieldExpression(node.Left, lhsSym);
            return chosenExpr == node.Left
                    ? node_P
                    : node_P.WithLeft(chosenExpr);
        }

        public override SyntaxNode VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
        {
            if (__tySym == null)
                return node;

            var node_P = node.WithOperand((ExpressionSyntax)node.Operand.Accept(this));

            var operandSym = _semaModel.GetSymbolInfo(node.Operand).Symbol;
            var chosenExpr = __ChooseBetweenPropertyOrFieldExpression(node.Operand, operandSym);
            return chosenExpr == node.Operand
                    ? node_P
                    : node_P.WithOperand(chosenExpr);
        }

        public override SyntaxNode VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
        {
            if (__tySym == null)
                return node;

            var node_P = node.WithOperand((ExpressionSyntax)node.Operand.Accept(this));

            var operandSym = _semaModel.GetSymbolInfo(node.Operand).Symbol;
            var choseExpr = __ChooseBetweenPropertyOrFieldExpression(node.Operand, operandSym);
            return choseExpr == node.Operand
                    ? node_P
                    : node_P.WithOperand(choseExpr);
        }

        private ExpressionSyntax __ChooseBetweenPropertyOrFieldExpression(
            ExpressionSyntax propExpr,
            ISymbol sym)
        {
            Debug.Assert(__tySym != null);

            return (sym is IPropertySymbol propSym
                        && SymbolEqualityComparer.Default.Equals(propSym.ContainingType, __tySym)
                        && propSym.IsReadOnly
                        && propSym.DeclaringSyntaxReferences.Any())
                   ? SyntaxFactory.IdentifierName(SynthesizedFieldName(propSym.Name))
                                  .WithTriviaFrom(propExpr)
                   : propExpr;
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
