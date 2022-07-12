using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpSyntaxRewriter.Rewriters.Types;

namespace SharpSyntaxRewriter.Rewriters
{
    public class ExpandFileScopedNamespace : SymbolicRewriter
    {
        private const string ID = "<expand file-scoped namespace>";

        public override string Name()
        {
            return ID;
        }

        public override SyntaxNode VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
        {
            return SyntaxFactory.NamespaceDeclaration(
                namespaceKeyword: node.NamespaceKeyword,
                name: node.Name,
                externs: node.Externs,
                attributeLists: node.AttributeLists,
                modifiers: node.Modifiers,
                openBraceToken: SyntaxFactory.Token(SyntaxKind.OpenBraceToken).WithLeadingTrivia(node.GetLeadingTrivia()),
                usings: node.Usings,
                members: node.Members,
                closeBraceToken: SyntaxFactory.Token(SyntaxKind.CloseBraceToken),
                semicolonToken: node.SemicolonToken).WithoutTrailingTrivia();
        }
    }
}
