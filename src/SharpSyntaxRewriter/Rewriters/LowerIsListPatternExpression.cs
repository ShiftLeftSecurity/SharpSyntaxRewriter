using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpSyntaxRewriter.Rewriters.Types;
using System;
using System.Linq;

namespace SharpSyntaxRewriter.Rewriters
{
    public class LowerIsListPatternExpression : SymbolicRewriter
    {
        public const string ID = "<lower `is' list pattern expression>";
        
        public override string Name()
        {
            return ID;
        }
        
        public override SyntaxNode VisitIsPatternExpression(IsPatternExpressionSyntax node)
        {
            if (node.Pattern is ListPatternSyntax)
            {
                return new ListPatternLowererVisitor(_semaModel).Visit(node.Pattern)(node.Expression);
            }

            return base.VisitIsPatternExpression(node);
        }
    }

    internal class ListPatternLowererVisitor : CSharpSyntaxVisitor<Func<ExpressionSyntax,ExpressionSyntax>>
    {
        private readonly SemanticModel __semanticModel;

        public ListPatternLowererVisitor(SemanticModel semanticModel)
        {
            __semanticModel = semanticModel;
        }
        
        public override Func<ExpressionSyntax, ExpressionSyntax> VisitConstantPattern(ConstantPatternSyntax node)
        {
            var constantsTypeSym = __semanticModel.GetTypeInfo(node.Expression).Type;
            
            // If we can't find the constant's type, return `is <constant>`. 
            if (constantsTypeSym is null)
            {
                return holeExpr => SyntaxFactory.IsPatternExpression(holeExpr, node);
            }

            var constantsTypeSyntax = SyntaxFactory.ParseTypeName(constantsTypeSym.ToString());
            
            return holeExpr => SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression,
                SyntaxFactory.CastExpression(constantsTypeSyntax, holeExpr),
                node.Expression);
        }
        
        public override Func<ExpressionSyntax, ExpressionSyntax> VisitTypePattern(TypePatternSyntax node)
        {
            return holeExpr => SyntaxFactory.IsPatternExpression(holeExpr, node);
        }
        
        public override Func<ExpressionSyntax, ExpressionSyntax> VisitRecursivePattern(RecursivePatternSyntax node)
        {
            return holeExpr => SyntaxFactory.IsPatternExpression(holeExpr, node);
        }

        public override Func<ExpressionSyntax, ExpressionSyntax> VisitParenthesizedPattern(ParenthesizedPatternSyntax node)
        {
            var nodePat = Visit(node.Pattern);
            
            return holeExpr => SyntaxFactory.ParenthesizedExpression(nodePat(holeExpr));
        }

        public override Func<ExpressionSyntax, ExpressionSyntax> VisitUnaryPattern(UnaryPatternSyntax node)
        {
            var nodePat = Visit(node.Pattern);
            
            return node.OperatorToken.Kind() switch
            {
                SyntaxKind.NotKeyword => holeExpr => SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, SyntaxFactory.ParenthesizedExpression(nodePat(holeExpr))),
            };
        }

        public override Func<ExpressionSyntax, ExpressionSyntax> VisitVarPattern(VarPatternSyntax node)
        {
            // Small optimization: `var _` is treated the same way as `_`
            if (node.Designation is DiscardDesignationSyntax)
            {
                return _ => SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);
            }
            
            return holeExpr => SyntaxFactory.IsPatternExpression(holeExpr, node);
        }

        public override Func<ExpressionSyntax, ExpressionSyntax> VisitSlicePattern(SlicePatternSyntax node)
        {
            return node.Pattern switch
            {
                // Small optimization: `..var _` is treated the same way as `.. _`
                null or DiscardPatternSyntax or VarPatternSyntax{Designation:DiscardDesignationSyntax} => _ => SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression),
                RecursivePatternSyntax recPat => holeExpr => SyntaxFactory.IsPatternExpression(holeExpr, recPat),
                VarPatternSyntax varPat => holeExpr => SyntaxFactory.IsPatternExpression(holeExpr, varPat)
            };
        }

        public override Func<ExpressionSyntax, ExpressionSyntax> VisitDiscardPattern(DiscardPatternSyntax node)
        {
            return _ => SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);
        }

        public override Func<ExpressionSyntax, ExpressionSyntax> VisitBinaryPattern(BinaryPatternSyntax node)
        {
            var lhsPat = Visit(node.Left);
            var rhsPat = Visit(node.Right);
            
            return node.OperatorToken.Kind() switch
            {
                SyntaxKind.OrKeyword => holeExpr => SyntaxFactory.BinaryExpression(SyntaxKind.LogicalOrExpression, lhsPat(holeExpr), rhsPat(holeExpr)),
                SyntaxKind.AndKeyword => holeExpr => SyntaxFactory.BinaryExpression(SyntaxKind.LogicalAndExpression, lhsPat(holeExpr), rhsPat(holeExpr)),
            };
        }

        public override Func<ExpressionSyntax, ExpressionSyntax> VisitRelationalPattern(RelationalPatternSyntax node)
        {
            SyntaxKind BinOpKind(SyntaxToken opToken) => opToken.Kind() switch
            {
                SyntaxKind.GreaterThanToken => SyntaxKind.GreaterThanExpression,
                SyntaxKind.GreaterThanEqualsToken => SyntaxKind.GreaterThanOrEqualExpression,
                SyntaxKind.LessThanToken => SyntaxKind.LessThanExpression,
                SyntaxKind.LessThanEqualsToken => SyntaxKind.LessThanOrEqualExpression
            };
            
            return holeExpr => SyntaxFactory.BinaryExpression(BinOpKind(node.OperatorToken), holeExpr, node.Expression);
        }

        public override Func<ExpressionSyntax, ExpressionSyntax> VisitListPattern(ListPatternSyntax node)
        {
            var sliceIndex = node.Patterns.IndexOf(pattern => pattern is SlicePatternSyntax);
            var hasSlice = sliceIndex > -1;
            var patterns = node.Patterns.Select((pattern, index) => LowerListElementPattern(pattern, index, sliceIndex, node.Patterns.Count));

            ExpressionSyntax NotNullSyntax(ExpressionSyntax expr) => 
                SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression, expr, SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));

            ExpressionSyntax HasLengthOrCountSyntax(ExpressionSyntax expr) =>
                SyntaxFactory.BinaryExpression(hasSlice ? SyntaxKind.GreaterThanOrEqualExpression : SyntaxKind.EqualsExpression,
                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, expr, SyntaxFactory.IdentifierName(GetSuitableLengthOrCountPropertyName(node))),
                    SyntaxFactory.ParseExpression((node.Patterns.Count - (hasSlice ? 1 : 0)).ToString()));

            ExpressionSyntax AndSyntax(ExpressionSyntax lhs, ExpressionSyntax rhs) => rhs switch
            {
                // Small optimization: `lhs && (true)` becomes `lhs`
                ParenthesizedExpressionSyntax { Expression: LiteralExpressionSyntax lit } when lit.IsKind(SyntaxKind.TrueLiteralExpression) => lhs,
                _ => SyntaxFactory.BinaryExpression(SyntaxKind.LogicalAndExpression, lhs, rhs)
            };

            return holeExpr => patterns
                .Select(pattern => pattern(holeExpr))
                .Select(SyntaxFactory.ParenthesizedExpression)
                .Aggregate(AndSyntax(NotNullSyntax(holeExpr), HasLengthOrCountSyntax(holeExpr)), AndSyntax);
        }

        private string GetSuitableLengthOrCountPropertyName(ListPatternSyntax node)
        {
            return __semanticModel.GetTypeInfo(node).Type?.GetMembers()
                       .OfType<IPropertySymbol>()
                       .Where(propSym => propSym.Type.Name == "Int32")
                       .Where(propSym => propSym.Name is "Length" or "Count")
                       .Select(propSym => propSym.Name)
                       .FirstOrDefault()
                   ?? "Length";
        }

        private Func<ExpressionSyntax, ExpressionSyntax> LowerListElementPattern(PatternSyntax currentPattern, int currentIndex, int sliceIndex, int listLength)
        {
            var currentPat = Visit(currentPattern);
            
            ExpressionSyntax ElemIndexSyntax(ExpressionSyntax expr)
            {
                var indexStr = sliceIndex switch
                {
                    <0 => $"{currentIndex}",
                    >=0 when currentIndex < sliceIndex => $"{currentIndex}",
                    >=0 when currentIndex == sliceIndex => $"{currentIndex}..^{listLength - sliceIndex -1}",
                    >=0 when currentIndex > sliceIndex => $"^{listLength - currentIndex}"
                };
                
                return SyntaxFactory.ElementAccessExpression(expr, SyntaxFactory.ParseBracketedArgumentList($"[{indexStr}]"));
            }
            
            return holeExpr => currentPat(ElemIndexSyntax(holeExpr));
        }
    }
    
}
