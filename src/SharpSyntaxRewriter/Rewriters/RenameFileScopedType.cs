using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpSyntaxRewriter.Rewriters.Types;
using System;
using System.Linq;

namespace SharpSyntaxRewriter.Rewriters
{
    public class RenameFileScopedType : SymbolicRewriter
    {
        public const string ID = "<rename file-scoped type>";

        public override string Name()
        {
            return ID;
        }
        
        private static string SynthesizedTypeName(ISymbol namedTypeSymbol)
        {
            return namedTypeSymbol.MetadataName
                .Replace('<', '_')
                .Replace('>', '_')
                .Replace('`', '_');
        }

        private static bool IsFileLocal(INamedTypeSymbol sym)
        {
            return sym?.IsFileLocal ?? false;
        }

        private SyntaxNode VisitTypeDeclaration<T>(T node, Func<T, SyntaxNode> visit) where T : BaseTypeDeclarationSyntax
        {
            var node_P = visit(node) as T;
            var nodeSym = _semaModel.GetDeclaredSymbol(node);

            if (IsFileLocal(nodeSym))
            {
                var fileModifierToken = node_P.Modifiers.Single(token => token.IsKind(SyntaxKind.FileKeyword));
                
                var newIdentifier = SyntaxFactory.Identifier(SynthesizedTypeName(nodeSym))
                    .WithLeadingTrivia(node_P.Identifier.LeadingTrivia)
                    .WithTrailingTrivia(node_P.Identifier.TrailingTrivia);

                var internalModifierToken = SyntaxFactory.Token(SyntaxKind.InternalKeyword)
                    .WithLeadingTrivia(fileModifierToken.LeadingTrivia)
                    .WithTrailingTrivia(fileModifierToken.TrailingTrivia);

                var newModifiersList = node_P.Modifiers.Replace(fileModifierToken, internalModifierToken);

                return node_P.WithIdentifier(newIdentifier).WithModifiers(newModifiersList);
            }

            return node_P;
        }
        
        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            return VisitTypeDeclaration(node, base.VisitClassDeclaration);
        }

        public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node)
        {
            return VisitTypeDeclaration(node, base.VisitStructDeclaration);
        }

        public override SyntaxNode VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            return VisitTypeDeclaration(node, base.VisitInterfaceDeclaration);
        }

        public override SyntaxNode VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            return VisitTypeDeclaration(node, base.VisitEnumDeclaration);
        }

        public override SyntaxNode VisitRecordDeclaration(RecordDeclarationSyntax node)
        {
            return VisitTypeDeclaration(node, base.VisitRecordDeclaration);
        }
        
        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            var node_P = base.VisitIdentifierName(node) as IdentifierNameSyntax;

            if (node.IsVar)
                return node_P;
            
            var nodeSym = _semaModel.GetSymbolInfo(node).Symbol as INamedTypeSymbol;

            if (!IsFileLocal(nodeSym))
                return node_P;

            if (nodeSym.Name != node.Identifier.Text)
                return node_P;

            var newIdentifier = SyntaxFactory.ParseToken(SynthesizedTypeName(nodeSym))
                .WithLeadingTrivia(node_P.GetLeadingTrivia())
                .WithTrailingTrivia(node_P.GetTrailingTrivia());

            return node_P.WithIdentifier(newIdentifier);
        }
    }
}
