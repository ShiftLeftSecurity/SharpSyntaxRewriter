using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpSyntaxRewriter.Rewriters;

namespace Tests
{
    [TestClass]
    public class TestLowerIsListPatternExpression : RewriterTester
    {
        protected override SyntaxTree ApplyRewrite(SyntaxTree tree, Compilation compilation)
        {
            return new LowerIsListPatternExpression().Apply(tree, compilation.GetSemanticModel(tree));
        }

        [TestMethod]
        public void TestArraysSingleElementIsThisNumericConstant()
        {
            var original = @"
class C
{
    bool M(int[] x)
    {
        return x is [1];
    }
}";
            var expected = @"
class C
{
    bool M(int[] x)
    {
        return x != null && x.Length == 1 && ((int)x[0] == 1);
    }
}";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestArraySingleElementIsNotNumericConstant()
        {
            var original = @"
class C
{
    bool M(int[] x)
    {
        return x is [not 0];
    }
}";
            var expected = @"
class C
{
    bool M(int[] x)
    {
        return x != null && x.Length == 1 && (!((int)x[0] == 0));
    }
}";
            TestRewrite_LinePreserve(original, expected);
        }


        [TestMethod]
        public void TestArraysSingleElementIsThisNumericConstantOrThatNumericConstant()
        {
            var original = @"
class C
{
    bool M(int[] x)
    {
        return x is [10 or 20];
    }
}";
            var expected = @"
class C
{
    bool M(int[] x)
    {
        return x != null && x.Length == 1 && ((int)x[0] == 10 || (int)x[0] == 20);
    }
}";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestArraysSingleElementIsEitherIntOrDouble()
        {
            var original = @"
class C
{
    bool M(object[] x)
    {
        return x is [int or double];
    }
}";
            var expected = @"
class C
{
    bool M(object[] x)
    {
        return x != null && x.Length == 1 && (x[0] is int || x[0] is double);
    }
}";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestArraysSingleElementIsIntAndZero()
        {
            var original = @"
class C
{
    bool M(object[] x)
    {
        return x is [int and 0];
    }
}";
            var expected = @"
class C
{
    bool M(object[] x)
    {
        return x != null && x.Length == 1 && (x[0] is int && (int)x[0] == 0);
    }
}";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestArraysSingleElementIsNotIntOrIsDouble()
        {
            var original = @"
class C
{
    bool M(object[] x)
    {
        return x is [not int or double];
    }
}";
            var expected = @"
class C
{
    bool M(object[] x)
    {
        return x != null && x.Length == 1 && (!(x[0] is int) || x[0] is double);
    }
}";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestArraysSingleElementIsNotIntOrDouble()
        {
            var original = @"
class C
{
    bool M(object[] x)
    {
        return x is [not (int or double)];
    }
}";
            var expected = @"
class C
{
    bool M(object[] x)
    {
        return x != null && x.Length == 1 && (!((x[0] is int || x[0] is double)));
    }
}";
            TestRewrite_LinePreserve(original, expected);
        }

        [DataTestMethod]
        [DataRow(">")]
        [DataRow("<")]
        [DataRow(">=")]
        [DataRow("<=")]
        public void TestArraysSingleElementIsInRelationWithNumericConstant(string relOp)
        {
            var original = $@"
class C
{{
    bool M(int[] x)
    {{
        return x is [{relOp} 0];
    }}
}}";
            var expected = $@"
class C
{{
    bool M(int[] x)
    {{
        return x != null && x.Length == 1 && (x[0] {relOp} 0);
    }}
}}";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestArraysSingleElementIsGreaterThanAndLesserThanIntegerLiterals()
        {
            var original = @"
class C
{
    bool M(int[] x)
    {
        return x is [>0 and <100];
    }
}";
            var expected = @"
class C
{
    bool M(int[] x)
    {
        return x != null && x.Length == 1 && (x[0] > 0 && x[0] < 100);
    }
}";
            TestRewrite_LinePreserve(original, expected);
        }
        
        
        [TestMethod]
        public void TestArrayIsEmptyPattern()
        {
            var original = @"
class C
{
    void M(int[] arr)
    {
        System.Console.WriteLine(arr is []);
    }
}";
            var expected = @"
class C
{
    void M(int[] arr)
    {
        System.Console.WriteLine(arr != null && arr.Length == 0);
    }
}";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestArraysSingleElementIsSlicePattern()
        {
            var original = @"
class C
{
    void M(int[] arr)
    {
        System.Console.WriteLine(arr is [..]);
    }
}";
            var expected = @"
class C
{
    void M(int[] arr)
    {
        System.Console.WriteLine(arr != null && arr.Length >= 0);
    }
}";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestArraysSingleElementIsSliceDiscardPattern()
        {
            var original = @"
class C
{
    void M(int[] arr)
    {
        System.Console.WriteLine(arr is [.. _]);
    }
}";
            var expected = @"
class C
{
    void M(int[] arr)
    {
        System.Console.WriteLine(arr != null && arr.Length >= 0);
    }
}";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestArraysSingleElementIsSliceVarDiscardPattern()
        {
            var original = @"
class C
{
    bool M(int[] arr)
    {
        return arr is [.. var _];
    }
}";
            var expected = @"
class C
{
    bool M(int[] arr)
    {
        return arr != null && arr.Length >= 0;
    }
}";
            TestRewrite_LinePreserve(original, expected);
        }
        
        [TestMethod]
        public void TestArraysSingleElementIsSliceVarPattern()
        {
            var original = @"
class C
{
    void M(int[] arr)
    {
        System.Console.WriteLine(arr is [.. var arr2]);
    }
}";
            var expected = @"
class C
{
    void M(int[] arr)
    {
        System.Console.WriteLine(arr != null && arr.Length >= 0 && (arr[0..^0] is var arr2));
    }
}";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestArrayIsExactlyTheseTwoElements()
        {
            var original = @"
class C
{
    void M(int[] arr)
    {
        System.Console.WriteLine(arr is [3,4]);
    }
}";

            var expected = @"
class C
{
    void M(int[] arr)
    {
        System.Console.WriteLine(arr != null && arr.Length == 2 && ((int)arr[0] == 3) && ((int)arr[1] == 4));
    }
}";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestArraysSingleElementIsEitherThisOrThat()
        {
            var original = @"
class C
{
    void M(int[] arr)
    {
        System.Console.WriteLine(arr is [3 or 4]);
    }
}";
            var expected = @"
class C
{
    void M(int[] arr)
    {
        System.Console.WriteLine(arr != null && arr.Length == 1 && ((int)arr[0] == 3 || (int)arr[0] == 4));
    }
}";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestArrayContainsExactlyThreeElementsUsingDiscardPatterns()
        {
            var original = @"
class C
{
    bool M(int[] arr)
    {
        return arr is [_,_,_];
    }
}";
            var expected = @"
class C
{
    bool M(int[] arr)
    {
        return arr != null && arr.Length == 3;
    }
}";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestArrayContainsAtLeastTwoElementsUsingSliceAndDiscardPatterns()
        {
            var original = @"
class C
{
    bool M(int[] arr)
    {
        return arr is [_, .., _];
    }
}";
            var expected = @"
class C
{
    bool M(int[] arr)
    {
        return arr != null && arr.Length >= 2;
    }
}";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestArrayContainsSingleElementUsingVarAndDiscardPattern()
        {
            var original = @"
class C
{
    bool M(int[] arr)
    {
        return arr is [var _];
    }
}";
            var expected = @"
class C
{
    bool M(int[] arr)
    {
        return arr != null && arr.Length == 1;
    }
}";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestSplitArrayIntoHeadAndTail()
        {
            var original = @"
class C
{
    bool M(int[] arr)
    {
        return arr is [var head, .. var tail];
    }
}";
            var expected = @"
class C
{
    bool M(int[] arr)
    {
        return arr != null && arr.Length >= 1 && (arr[0] is var head) && (arr[1..^0] is var tail);
    }
}";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestArrayEndsWithTheseTwoConstants()
        {
            var original = @"
class C
{
    bool M(int[] arr)
    {
        return arr is [.., 3, 4];
    }
}";
            var expected = @"
class C
{
    bool M(int[] arr)
    {
        return arr != null && arr.Length >= 2 && ((int)arr[^2] == 3) && ((int)arr[^1] == 4);
    }
}";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestNestedArraysSingleElementIsSingleArray()
        {
            var original = @"
class C
{
    bool M(int[][] arr)
    {
        return arr is [[1]];
    }
}";
            var expected = @"
class C
{
    bool M(int[][] arr)
    {
        return arr != null && arr.Length == 1 && (arr[0] != null && arr[0].Length == 1 && ((int)arr[0][0] == 1));
    }
}";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestArraysSingleElementIsPropertyPattern()
        {
            var original = @"
using System;
class C
{
    bool M(DateTime[] arr)
    {
        return arr is [{Month:10}];
    }
}";
            var expected = @"
using System;
class C
{
    bool M(DateTime[] arr)
    {
        return arr != null && arr.Length == 1 && (arr[0] is {Month:10});
    }
}";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestIsListPatternLoweringDoesNotOccurInSwitchCases()
        {
            var original = @"
class C
{
    bool M(int[] arr)
    {
        switch (arr)
        {
            case []: return false;
            default: return true;
        }
    }
}";
            TestRewrite_LinePreserve(original, original);
        }

        [TestMethod]
        public void TestIsListPatternLoweringDoesNotOccurOutsideOfListPatterns()
        {
            var original = @"
class C
{
    bool M(int x)
    {
        return x is 0;
    }
}";
            TestRewrite_LinePreserve(original, original);
        }

        [TestMethod]
        public void TestArraysSingleElementIsVarPatternInLogicalAnd()
        {
            var original = @"
class C
{
    bool M(int[] arr)
    {
        return arr is [var head] && head == 1;
    }
}";
            var expected = @"
class C
{
    bool M(int[] arr)
    {
        return arr != null && arr.Length == 1 && (arr[0] is var head) && head == 1;
    }
}";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestArraysSingleElementIsConstantEnumMember()
        {
            var original = @"
enum E
{
    F
};
class C
{
    bool M(object[] arr)
    {
        return arr is [E.F];
    }
}";
            var expected = @"
enum E
{
    F
};
class C
{
    bool M(object[] arr)
    {
        return arr != null && arr.Length == 1 && ((E)arr[0] == E.F);
    }
}";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestListsSingleElementIsConstant()
        {
            var original = @"
using System.Collections.Generic;
class C
{
    bool M(List<int> list)
    {
        return list is [0];
    }
}";
            var expected = @"
using System.Collections.Generic;
class C
{
    bool M(List<int> list)
    {
        return list != null && list.Count == 1 && ((int)list[0] == 0);
    }
}";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestListsSingleElementIsArrayWhoseSingleElementIsConstant()
        {
            var original = @"
using System.Collections.Generic;
class C
{
    bool M(List<int[]> list)
    {
        return list is [[10]];
    }
}";
            var expected = @"
using System.Collections.Generic;
class C
{
    bool M(List<int[]> list)
    {
        return list != null && list.Count == 1 && (list[0] != null && list[0].Length == 1 && ((int)list[0][0] == 10));
    }
}";
            TestRewrite_LinePreserve(original, expected);
        }
        
        [TestMethod]
        public void TestArraysSingleElementIsListWhoseSingleElementIsConstant()
        {
            var original = @"
using System.Collections.Generic;
class C
{
    bool M(List<int>[] arr)
    {
        return arr is [[10]];
    }
}";
            var expected = @"
using System.Collections.Generic;
class C
{
    bool M(List<int>[] arr)
    {
        return arr != null && arr.Length == 1 && (arr[0] != null && arr[0].Count == 1 && ((int)arr[0][0] == 10));
    }
}";
            TestRewrite_LinePreserve(original, expected);
        }
        
        [TestMethod]
        public void TestSpansSingleElementIsConstant()
        {
            var original = @"
using System;
class C
{
    bool M(Span<int> span)
    {
        return span is [10];
    }
}";
            var expected = @"
using System;
class C
{
    bool M(Span<int> span)
    {
        return span != null && span.Length == 1 && ((int)span[0] == 10);
    }
}";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestArraysSinglePatternIsSlicePropertyPattern()
        {
            var original = @"
class C
{
    bool M(int[] arr)
    {
        return arr is [.. { Length: 2 or 4 }];
    }
}";
            var expected = @"
class C
{
    bool M(int[] arr)
    {
        return arr != null && arr.Length >= 0 && (arr[0..^0] is {Length: 2 or 4});
    }
}";
            TestRewrite_LinePreserve(original, expected);
        }
    }
}
