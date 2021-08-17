using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using SharpSyntaxRewriter.Rewriters;

namespace Tests
{
    [TestClass]
    public class TestExpandForeach : RewriterTester
    {
        protected override SyntaxTree ApplyRewrite(SyntaxTree tree, Compilation compilation)
        {
            ExpandForeach rw = new();
            return rw.Apply(tree, compilation.GetSemanticModel(tree));
        }

        [TestMethod]
        public void TestExpandForeachOfArrayWithTypeSpec()
        {
            var original = @"
class Program
{
    public void Foo()
    {
        int[] iarr = { 1, 2 };
        foreach (int e in iarr)
            ;
    }
}
";

        var expected = @"
class Program
{
    public void Foo()
    {
        int[] iarr = { 1, 2 };
        { var e_L6C8 = ((System.Collections.IEnumerable)iarr).GetEnumerator();  while(e_L6C8.MoveNext()) { int e=(int)(int)e_L6C8.Current;
             ; }}
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestExpandForeachOfArrayWithTypeSpecSymmetricBrace()
        {
            var original = @"
class Program
{
    public void Foo()
    {
        int[] iarr = { 1, 2 };
        foreach (int e in iarr)
        {
            ;
        }
    }
}
";

            var expected = @"
class Program
{
    public void Foo()
    {
        int[] iarr = { 1, 2 };
        { var e_L6C8 = ((System.Collections.IEnumerable)iarr).GetEnumerator();  while(e_L6C8.MoveNext()) { int e=(int)(int)e_L6C8.Current;
        {
            ;
        } }}
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestExpandForeachOfArrayWithTypeSpecAsymmetricBrace()
        {
            var original = @"
class Program
{
    public void Foo()
    {
        int[] iarr = { 1, 2 };
        foreach (int e in iarr) {
            ;
        }
    }
}
";

            var expected = @"
class Program
{
    public void Foo()
    {
        int[] iarr = { 1, 2 };
        { var e_L6C8 = ((System.Collections.IEnumerable)iarr).GetEnumerator();  while(e_L6C8.MoveNext()) { int e=(int)(int)e_L6C8.Current; {
            ;
        } }}
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestExpandForeachOfArrayWithTypeSpecBraceSameLine()
        {
            var original = @"
class Program
{
    public void Foo()
    {
        int[] iarr = { 1, 2 };
        foreach (int e in iarr) { ; }
    }
}
";

            var expected = @"
class Program
{
    public void Foo()
    {
        int[] iarr = { 1, 2 };
        { var e_L6C8 = ((System.Collections.IEnumerable)iarr).GetEnumerator();  while(e_L6C8.MoveNext()) { int e=(int)(int)e_L6C8.Current; { ; } }}
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestExpandForeachOfArrayWithTypeSpecEmptyBraceBlock()
        {
            var original = @"
class Program
{
    public void Foo()
    {
        int[] iarr = { 1, 2 };
        foreach (int e in iarr)
        {}
    }
}
";

            var expected = @"
class Program
{
    public void Foo()
    {
        int[] iarr = { 1, 2 };
        { var e_L6C8 = ((System.Collections.IEnumerable)iarr).GetEnumerator();  while(e_L6C8.MoveNext()) { int e=(int)(int)e_L6C8.Current;
        {} }}
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }


        [TestMethod]
        public void TestExpandForeachOfArrayWithVarSpec()
        {
            var original = @"
class Program
{
    public void Foo()
    {
        int[] iarr = { 1, 2 };
        foreach (var e in iarr)
            ;
    }
}
";

            var expected = @"
class Program
{
    public void Foo()
    {
        int[] iarr = { 1, 2 };
        { var e_L6C8 = ((System.Collections.IEnumerable)iarr).GetEnumerator(); while(e_L6C8.MoveNext()) { var e=(int)(int)e_L6C8.Current;
            ; }}
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestExpandForeachOfList()
        {
            var original = @"
using System;
using System.Collections.Generic;

public class Test
{
    public void fff()
    {
        List<int> jjj = null;
        foreach (var ooo in jjj)
            break;
    }
}
";

            var expected = @"
using System;
using System.Collections.Generic;

public class Test
{
    public void fff()
    {
        List<int> jjj = null;
        { var e_L9C8=((List<int>)jjj).GetEnumerator(); while(e_L9C8.MoveNext()) { var ooo=(int)(int)e_L9C8.Current;
                break; } }
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestExpandForeachOfDynamicCollection()
        {
            var original = @"
using System;

public class Abc
{
    public void fff()
    {
        dynamic abc = null;
        foreach (var iii in abc) {}
    }
}
";

            var expected = @"
using System;

public class Abc
{
    public void fff()
    {
        dynamic abc = null;
        { var e_L8C8=((System.Collections.IEnumerable)abc).GetEnumerator(); while (e_L8C8.MoveNext()) { var iii =(dynamic)(dynamic)e_L8C8.Current; {} } }
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestExpandForeachOfTupleSyntaxList()
        {
            var original = @"
using System;
using System.Collections.Generic;

public class Test
{
    public void fff()
    {
        List<ValueTuple<int, int>> ppp = null;
        foreach (var ttt in ppp)
            break;
    }
}
";

            var expected = @"
using System;
using System.Collections.Generic;

public class Test
{
    public void fff()
    {
        List<ValueTuple<int, int>> ppp = null;
        { var e_L9C8=((List<(int,int)>)ppp).GetEnumerator(); while(e_L9C8.MoveNext()) { var ttt=((int,int))((int,int))e_L9C8.Current;
                break; } }
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestExpandForeachOfNewTupleSyntaxList()
        {
            var original = @"
using System;
using System.Collections.Generic;

public class Test
{
    public void fff()
    {
        List<(int, int)> lll = null;
        foreach (var (xx, yy) in lll)
            break;
    }
}
";

            var expected = @"
using System;
using System.Collections.Generic;

public class Test
{
    public void fff()
    {
        List<(int, int)> lll = null;
        { var e_L9C8=((List<(int,int)>)lll).GetEnumerator(); while(e_L9C8.MoveNext()) { var(xx,yy)=((int,int))e_L9C8.Current;
                break; } }
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestExpandForeachRequiringGlobalNsQualifier()
        {
            var original = @"
using System;

namespace Abc.System {}

namespace Abc
{
    public class Test
    {
        public void fff()
        {
            Video vvv = null;
            foreach (var ooo in vvv.getlist()) {}
        }
    }
}
";

            var expected = @"
using System;

namespace Abc.System {}

namespace Abc
{
    public class Test
    {
        public void fff()
        {
            Video vvv = null;
            { var e_L12C12=((global::System.Collections.Generic.IEnumerable<int>)vvv.getlist()).GetEnumerator(); while(e_L12C12.MoveNext()) { var ooo=(int)(int)e_L12C12.Current; {} } }
        }
    }
}
";

            var complement = @"
using System.Collections.Generic;

class Video
{
    public IEnumerable<int> getlist() { return null; }
}
";

            TestRewrite_LinePreserve(original, expected, complement);
        }

        [TestMethod]
        public void TestExpandForeachCollectionExplicitImplementsIEnumerable()
        {
            var original = @"
using System;
using System.Dynamic;

public class Abc
{
    public void fff()
    {
        ExpandoObject ooo = null;
        foreach (var iii in ooo)
        {}
    }
}
";

            var expected = @"
using System;
using System.Dynamic;

public class Abc
{
    public void fff()
    {
        ExpandoObject ooo = null;
        { var e_L9C8=((System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string,object?>>)ooo).GetEnumerator(); while(e_L9C8.MoveNext()) { var iii=(System.Collections.Generic.KeyValuePair<string,object?>)(System.Collections.Generic.KeyValuePair<string,object?>)e_L9C8.Current;
        {} }}
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }
    }
}
