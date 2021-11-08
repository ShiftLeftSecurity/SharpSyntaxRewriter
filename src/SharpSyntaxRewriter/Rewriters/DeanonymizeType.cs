// Copyright 2021 ShiftLeft, Inc.
// Author: Leandro T. C. Melo

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;

using SharpSyntaxRewriter.Extensions;
using SharpSyntaxRewriter.Rewriters.Types;

namespace SharpSyntaxRewriter.Rewriters
{
    public class DeanonymizeType : SymbolicRewriter
    {
        public override string Name()
        {
            return "<deanonymize type>";
        }

        private static readonly ConcurrentDictionary<string, int> __deanonIDs = new();

        private readonly Dictionary<ITypeSymbol, string> __deanonTyNames = new();

        public override void Reset()
        {
            __deanonIDs.Clear();
        }

        private class __AnonymousTypeInfo
        {
            public __AnonymousTypeInfo(string key,
                                     string containingName,
                                     IEnumerable<string> containingTyParms)
            {
                Key = key;
                ContainingName = containingName;
                ContainingTypeParameters = containingTyParms == null
                    ? new ()
                    : new (containingTyParms);
            }

            public string Key;

            public string ContainingName;

            public HashSet<string> ContainingTypeParameters;

            public readonly List<TypeDeclarationSyntax> Deanonymized = new();
        }

        private readonly Stack<__AnonymousTypeInfo> __ctx = new();

        private TypeDeclarationSyntax VisitTypeDeclaration<TypeDeclarationT>(
                TypeDeclarationT node,
                Func<TypeDeclarationT, SyntaxNode> visit)
            where TypeDeclarationT : TypeDeclarationSyntax
        {
            var tySym = _semaModel.GetDeclaredSymbol(node);

            var anonTyInfo =
                new __AnonymousTypeInfo(
                    tySym.CanonicalDisplay(),
                    node.Identifier.Text,
                    node.TypeParameterList?.Parameters.Select(n => n.Identifier.Text));
            __ctx.Push(anonTyInfo);
            var node_P = (TypeDeclarationSyntax)visit(node);
            _ = __ctx.Pop();
            var tyDecls = anonTyInfo.Deanonymized;

            if (!tyDecls.Any())
                return node_P;

            var tyDecl = tyDecls.Last().WithTrailingTrivia(
                    node.OpenBraceToken.TrailingTrivia);
            tyDecls.RemoveAt(tyDecls.Count - 1);
            tyDecls.Add(tyDecl);

            var membDecls =
                SyntaxFactory.List<MemberDeclarationSyntax>(tyDecls);
            node_P = node_P.WithOpenBraceToken(
                        node.OpenBraceToken.WithTrailingTrivia(
                            SyntaxFactory.TriviaList()));
            membDecls = membDecls.AddRange(node_P.Members);
            node_P = node_P.WithMembers(membDecls);

            return node_P;
        }

        public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node)
        {
            return VisitTypeDeclaration(node,
                                        base.VisitStructDeclaration);
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

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            return node.WithBody((BlockSyntax)node.Body.Accept(this));
        }

        public override SyntaxNode VisitAnonymousObjectCreationExpression(
                AnonymousObjectCreationExpressionSyntax node)
        {
            var propDecls = SyntaxFactory.List<MemberDeclarationSyntax>();
            var tyParms = new HashSet<string>();
            var access = Accessibility.Internal;
            var ctorCallArgNodes = SyntaxFactory.SeparatedList<ArgumentSyntax>();

            var child = node.ChildNodesAndTokens();
            for (var i = 0; i < child.Count; ++i)
            {
                if (child[i].AsNode()
                        is not AnonymousObjectMemberDeclaratorSyntax dcltorNode)
                    continue;

                var dcltorNode_P =
                    (AnonymousObjectMemberDeclaratorSyntax)dcltorNode.Accept(this);

                string membName = null;
                if (dcltorNode_P.NameEquals != null)
                {
                    membName = dcltorNode_P.NameEquals.Name.Identifier.Text;
                }
                else
                {
                    switch (dcltorNode_P.Expression.Kind())
                    {
                        case SyntaxKind.IdentifierName:
                            membName = ((IdentifierNameSyntax)dcltorNode_P.Expression)
                                .Identifier.Text;
                            break;

                        case SyntaxKind.SimpleMemberAccessExpression:
                            membName = ((MemberAccessExpressionSyntax)dcltorNode_P.Expression)
                                .Name.Identifier.Text;
                            break;

                        default:
                            Debug.Fail("unknown anonymous type member declarator");
                            break;
                    }
                }

                var exprTySym = _semaModel.GetTypeInfo(dcltorNode.Expression).Type;

                if (exprTySym.DeclaredAccessibility < access)
                    access = exprTySym.DeclaredAccessibility;

                var propTySpec = RecognizePropertyOfAnonymousType(
                        exprTySym,
                        dcltorNode.Expression.SpanStart);
                var propDecl =
                    // TODO: Change to a `PropertyDeclarationSyntax' instead of
                    // a `FieldDeclarationSyntax'.
                    SyntaxFactory.FieldDeclaration(
                        SyntaxFactory.List<AttributeListSyntax>(),
                        SyntaxFactory.TokenList(
                            SyntaxFactory.Token(SyntaxKind.InternalKeyword)),
                        SyntaxFactory.VariableDeclaration(
                            propTySpec,
                            SyntaxFactory.SeparatedList(
                                new List<VariableDeclaratorSyntax> {
                                    SyntaxFactory.VariableDeclarator(membName) })));
                propDecls = propDecls.Add(propDecl);

                // Collect type parameters but only consider distincit ones.
                var tyParmNames = CollectTypeParameterNamesOfAnonymousType(exprTySym);
                if (tyParmNames != null)
                {
                    foreach (var n in tyParmNames)
                        if (!tyParms.Contains(n)
                                && !__ctx.Peek().ContainingTypeParameters.Contains(n))
                        {
                            _ = tyParms.Add(n);
                        }
                }

                ctorCallArgNodes =
                    ctorCallArgNodes.Add(
                        SyntaxFactory.Argument(dcltorNode_P.Expression)
                        .WithTriviaFrom(dcltorNode));
                if (ctorCallArgNodes.SeparatorCount > 0)
                {
                    var commaTk = (SyntaxToken)child[i - 1];
                    ctorCallArgNodes = ctorCallArgNodes.ReplaceSeparator(
                        ctorCallArgNodes.GetSeparator(ctorCallArgNodes.SeparatorCount - 1), commaTk);
                }
            }

            var anonTySym = _semaModel.GetTypeInfo(node).Type;
            string deanonTyName;
            if (__deanonTyNames.ContainsKey(anonTySym))
            {
                deanonTyName = __deanonTyNames[anonTySym];
            }
            else
            {
                deanonTyName = SynthesizeTypeDeclaration(
                    access.ToRewriteToken(),
                    propDecls,
                    tyParms.ToList());
                __deanonTyNames.Add(anonTySym, deanonTyName);
            }

            var argNodes_P = SyntaxFactory.ArgumentList(ctorCallArgNodes);
            argNodes_P = argNodes_P
                .WithOpenParenToken(
                    argNodes_P.OpenParenToken.WithTriviaFrom(
                        node.OpenBraceToken))
                .WithCloseParenToken(
                    argNodes_P.CloseParenToken.WithTriviaFrom(
                        node.CloseBraceToken));

            if (node.Initializers.Any() && node.Initializers.SeparatorCount == node.Initializers.Count)
            {
                var commaTk = node.Initializers.GetSeparators().Last();
                argNodes_P = argNodes_P
                    .WithCloseParenToken(argNodes_P.CloseParenToken
                    .WithLeadingTrivia(commaTk.LeadingTrivia
                                              .AddRange(commaTk.TrailingTrivia)
                                              .AddRange(argNodes_P.CloseParenToken.LeadingTrivia)));
            }

            return
                SyntaxFactory.ObjectCreationExpression(
                    SyntaxFactory.ParseTypeName(deanonTyName)
                        .WithTrailingTrivia(node.NewKeyword.TrailingTrivia),
                    argNodes_P,
                    null);
        }

        private TypeSyntax RecognizePropertyOfAnonymousType(ITypeSymbol tySym, int spanPos)
        {
            Debug.Assert(tySym != null);

            if (tySym.SpecialType == SpecialType.System_Void)
            {
                return SyntaxFactory.PredefinedType(
                           SyntaxFactory.Token(SyntaxKind.VoidKeyword));
            }

            if (!tySym.IsAnonymousType)
            {
                var tyName = tySym.ToMinimalDisplayString(_semaModel, spanPos);

                // Normalize the names of anonymous type that have been deanonymise
                // and which are underlying types of non-anonymous generic types.
                var underTySym = tySym.UnderlyingType();
                if (underTySym is INamedTypeSymbol namedTySym
                        && namedTySym.IsGenericType
                        && namedTySym.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T)
                {
                    // Traverse in reverse order since an anonymous type might be
                    // parameterized by another anonymous type discovered later.
                    foreach (var knownTySym in __deanonTyNames.Keys.Reverse())
                    {
                        var s = knownTySym.ToMinimalDisplayString(_semaModel, spanPos);
                        tyName = tyName.Replace(s, __deanonTyNames[knownTySym]);
                    }
                }

                if (__deanonTyNames.TryGetValue(underTySym, out string deanonTyName))
                {
                    var s = underTySym.ToMinimalDisplayString(_semaModel, spanPos);
                    tyName = tyName.Replace(s, deanonTyName);
                }

                return SyntaxFactory.ParseTypeName(tyName);
            }

            if (__deanonTyNames.ContainsKey(tySym))
                return SyntaxFactory.ParseTypeName(__deanonTyNames[tySym]);

            var tyParmNames = new List<string>();
            var propDecls = SyntaxFactory.List<MemberDeclarationSyntax>();
            foreach (var sym in tySym.GetMembers())
            {
                switch (sym)
                {
                    case IPropertySymbol propSym:
                        if (string.IsNullOrEmpty(propSym.Name))
                            break;

                        var membTySpec =
                            RecognizePropertyOfAnonymousType(propSym.Type, spanPos);
                        tyParmNames.AddRange(
                            CollectTypeParameterNamesOfAnonymousType(propSym.Type));

                        var membDecl =
                            SyntaxFactory.PropertyDeclaration(
                                SyntaxFactory.List<AttributeListSyntax>(),
                                SyntaxFactory.TokenList(SyntaxFactory.Token(
                                    SyntaxKind.InternalKeyword)),
                                membTySpec,
                                null,
                                SyntaxFactory.Identifier(propSym.Name),
                                SyntaxFactory.AccessorList()
                                    .AddAccessors(
                                        SyntaxFactory.AccessorDeclaration(
                                            SyntaxKind.GetAccessorDeclaration))
                                    .AddAccessors(
                                        SyntaxFactory.AccessorDeclaration(
                                            SyntaxKind.SetAccessorDeclaration)));
                        propDecls = propDecls.Add(membDecl);
                        break;

                    default:
                        Debug.Fail("unknown anonymous type member");
                        return null;
                }
            }

            var tyDecl = SynthesizeTypeDeclaration(
                    SyntaxFactory.Token(SyntaxKind.InternalKeyword),
                    propDecls,
                    tyParmNames);

            return SyntaxFactory.ParseTypeName(tyDecl);
        }

        private static List<string> CollectTypeParameterNamesOfAnonymousType(
                ITypeSymbol tySym)
        {
            var tyParmsNames = new List<string>();
            switch (tySym)
            {
                case ITypeParameterSymbol tyParamSym:
                    tyParmsNames.Add(tyParamSym.Name);
                    break;

                case INamedTypeSymbol namedTySym:
                    if (namedTySym.IsAnonymousType)
                    {
                        foreach (var sym in tySym.GetMembers())
                            if (sym is IPropertySymbol propSym)
                            {
                                tyParmsNames.AddRange(
                                    CollectTypeParameterNamesOfAnonymousType(propSym.Type));
                            }
                    }
                    else if (namedTySym.IsGenericType
                                && namedTySym.OriginalDefinition.SpecialType
                                        != SpecialType.System_Nullable_T)
                    {
                        namedTySym.TypeArguments.ToList().ForEach(
                            sym => tyParmsNames.AddRange(
                                CollectTypeParameterNamesOfAnonymousType(sym)));
                    }
                    break;
            }
            return tyParmsNames;
        }

        private string SynthesizeTypeDeclaration(
                SyntaxToken access,
                SyntaxList<MemberDeclarationSyntax> propDecls,
                List<string> tyParmNames)
        {
            Debug.Assert(__ctx.Any());

            var anonTyInfo = __ctx.Peek();
            var deanonID = __deanonIDs.AddOrUpdate(anonTyInfo.Key, 1, (_, old) => old + 1);
            var ident = "__AnonymousType"
                        + deanonID
                        + "_"
                        + anonTyInfo.ContainingName;

            var ctorParmNodes = SyntaxFactory.SeparatedList<ParameterSyntax>();
            var ctorStmtNodes = new List<StatementSyntax>();
            foreach (var propDecl in propDecls)
            {
                SyntaxToken propIdentTk;
                TypeSyntax propTySpec;
                switch (propDecl.Kind())
                {
                    // TODO: Depends on the other TODO for replacing a `FieldDeclarationsyntax'
                    // for a `PropertyDeclarationSyntax'; then, we could adjust here as well.

                    case SyntaxKind.FieldDeclaration:
                        var fldDecl = (FieldDeclarationSyntax)propDecl;
                        propIdentTk = fldDecl.Declaration.Variables.First().Identifier;
                        propTySpec = fldDecl.Declaration.Type;
                        break;

                    case SyntaxKind.PropertyDeclaration:
                        propIdentTk = ((PropertyDeclarationSyntax)propDecl).Identifier;
                        propTySpec = ((PropertyDeclarationSyntax)propDecl).Type;
                        break;

                    default:
                        Debug.Fail("unhandled anonymous type member");
                        return null;
                }

                ctorParmNodes = ctorParmNodes.Add(
                    SyntaxFactory.Parameter(
                        SyntaxFactory.List<AttributeListSyntax>(),
                        SyntaxFactory.TokenList(),
                        propTySpec,
                        propIdentTk,
                        null));

                ctorStmtNodes.Add(
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.ThisExpression(),
                                SyntaxFactory.IdentifierName(propIdentTk)),
                            SyntaxFactory.IdentifierName(propIdentTk))));
            }

            var ctorDecl =
                SyntaxFactory.ConstructorDeclaration(ident)
                    .WithParameterList(SyntaxFactory.ParameterList(ctorParmNodes))
                    .WithBody(SyntaxFactory.Block(ctorStmtNodes))
                    .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.InternalKeyword)));

            var name = ident;
            TypeParameterListSyntax tyParmNodes;
            if (tyParmNames != null && tyParmNames.Any())
            {
                tyParmNodes = SyntaxFactory.TypeParameterList();
                name += "<";
                for (int i = 0; i < tyParmNames.Count; ++i)
                {
                    tyParmNodes = tyParmNodes.AddParameters(SyntaxFactory.TypeParameter(tyParmNames[i]));
                    name += tyParmNames[i];
                    if (i < tyParmNames.Count - 1)
                        name += ",";
                }
                name += ">";
            }
            else
            {
                // The `TypeParameterListSyntax' must be `null'; not just empty,
                // which would lead to an empty pair of angle brackets, `<>'.
                tyParmNodes = null;
            }

            var deanonTyDecl =
                SyntaxFactory.ClassDeclaration(
                    SyntaxFactory.List<AttributeListSyntax>(),
                    SyntaxFactory.TokenList().Add(access),
                    SyntaxFactory.Identifier(ident),
                    tyParmNodes,
                    null,
                    SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
                    SyntaxFactory.List(propDecls).Add(ctorDecl));

            __ctx.Peek().Deanonymized.Add(deanonTyDecl);

            return name;
        }
    }
}
