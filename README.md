![](https://github.com/ShiftLeftSecurity/SharpSyntaxRewriter-playground/actions/workflows/build-and-test.yml/badge.svg)

# The (C) Sharp Syntax Rewriter Tool

This is a [Roslyn](https://github.com/dotnet/roslyn)-based tool for C# syntax rewriting with the purpose of source-code lowering.

## Design

There are 2 varieties of rewriter:

- a purely syntactic one, `Rewriter`, and
- a symbolic one, `SymbolicRewriter`.

For the rewriting, the former relies on syntactic information only; the latter also relies on symbolic information by means of a [SemanticModel](https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.semanticmodel).

## Rewriters

- `BlockifyExpressionBody`
- `DeanonymizeType`
- `DecomposeNullConditional`
- `EmplaceGlobalStatement`
- `EnsureVisibleConstructor`
- `ExpandForeach`
- `ImplementAutoProperty`
- `ImposeExplicitReturn`
- `ImposeThisPrefix`
- `InitializeOutArgument`
- `ReplicateLocalInitialization`
- `StoreObjectCreation`
- `TranslateLinq`
- `UncoalesceCoalescedNull`
- `UninterpolateStrings`
- `UnparameterizeRecordDeclaration`

Hint: A "description" of the exact rewrite performed by a given rewriter, you might want to check the [tests](https://github.com/ShiftLeftSecurity/SharpSyntaxRewriter-playground/tree/master/tests).

## Example

```csharp
void ApplyRewrite(IRewriter rewriter, SyntaxTree tree)
{
    SyntaxTree tree_P;
    if (rewriter.IsPurelySyntactic())
    {
        tree_P = ((Rewriter)rewriter).Apply(tree);
    }
    else
    {
        Compilation compilation = /* ... */
        tree_P = ((SymbolicRewriter)rewriter).Apply(tree,
                                                    compilation.GetSemanticModel(tree));
    }
}
```


