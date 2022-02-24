using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using SharpSyntaxRewriter.Rewriters;

namespace Tests
{
    [TestClass]
    public class TestDecomposeNullConditional : RewriterTester
    {
        protected override SyntaxTree ApplyRewrite(SyntaxTree tree, Compilation compilation)
        {
            DecomposeNullConditional rw = new();
            return rw.Apply(tree, compilation.GetSemanticModel(tree));
        }

        [TestMethod]
        public void TestDecomposeNullConditionalNullableInvocation()
        {
            var original = @"
class C
{
    void f()
    {
        var c = new C();
        c?.f();
    }
}
";

            var expected = @"
class C
{
    void f()
    {
        var c = new C();
        if ((object)c != null) c.f();
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDecomposeNullConditionalNullableInvocationLineBreakTrivia()
        {
            var original = @"
class C
{
    void f()
    {
        var c = new C();
        c?
            .f();
    }
}
";

            var expected = @"
class C
{
    void f()
    {
        var c = new C();
        if ((object)c != null) c
            .f();
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDecomposeNullConditionalNullableInvocationLineBreakTrivia2()
        {
            var original = @"
class C
{
    void f()
    {
        var c = new C();
        c
            ?.f();
    }
}
";

            var expected = @"
class C
{
    void f()
    {
        var c = new C();
        if ((object)c != null) c
            .f();
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDecomposeNullConditionalNullableInvocationPostLineBreak()
        {
            var original = @"
class C
{
    void f()
    {
        var c = new C();

        c?.f();
    }
}
";

            var expected = @"
class C
{
    void f()
    {
        var c = new C();

        if ((object)c != null) c.f();
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDecomposeNullConditionalNullableInvocationPreAndPostLineBreak()
        {
            var original = @"
class C
{
    void f()
    {
        var c = new C();

        c?.f();

    }
}
";

            var expected = @"
class C
{
    void f()
    {
        var c = new C();

        if ((object)c != null) c.f();

    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDecomposeNullConditionalMemberAccessNonNullable()
        {
            var original = @"
class C
{
    int Val { get; set; }
    void f()
    {
        var c = new C();
        int? length = c?.Val;
    }
}
";

            var expected = @"
class C
{
    int Val { get; set; }
    void f()
    {
        var c = new C();
        int? length = ((object)c == null) ? (int?)null : c.Val;
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDecomposeNullConditionalMemberAccessNonNullableLineBreakTrivia()
        {
            var original = @"
class C
{
    int Val { get; set; }
    void f()
    {
        var c = new C();
        int? length = c?
            .Val;
    }
}
";

            var expected = @"
class C
{
    int Val { get; set; }
    void f()
    {
        var c = new C();
        int? length = ((object)c == null) ? (int?)null : c
            .Val;
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDecomposeNullConditionalMemberAccessNonNullableLineBreakTrivia2()
        {
            var original = @"
class C
{
    int Val { get; set; }
    void f()
    {
        var c = new C();
        int? length = c
            ?.Val;
    }
}
";

            var expected = @"
class C
{
    int Val { get; set; }
    void f()
    {
        var c = new C();
        int? length = ((object)c == null) ? (int?)null : c
            .Val;
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDecomposeNullConditionalMemberAccessNonNullableVar()
        {
            var original = @"
class C
{
    int Val { get; set; }
    void f()
    {
        var c = new C();
        var length2 = c?.Val;
    }
}
";

            var expected = @"
classC
{
    int Val { get; set; }
    void f()
    {
        var c= new C();
        var length2 = ((object)c==null)? (int?)null : c.Val;
     }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDecomposeNullConditionalArrayMemberAccess()
        {
            var original = @"
class C
{
    int[] arr = new int[] { 1 };
    void f()
    {
        var c = new C();
        var x = c?.arr[0];
    }
}
";

            var expected = @"
classC
{
    int[] arr = new int[]{1};
    void f()
    {
        var c =new C();
        var x = ((object)c==null) ? (int?)null : c.arr[0];
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDecomposeNullConditionalElementAccess()
        {
            var original = @"
class C
{
    void f()
    {
        var a = new C[] {};
        C first = a?[0];
    }
}
";

            var expected = @"
class C
{
    void f()
    {
        var a = new C[] {};
        C first = ((object)a == null) ? null : a[0];
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDecomposeNullConditionalElementAndMemberAccess()
        {
            var original = @"
class C
{
    int Val { get; set; }
    void f()
    {
        var a = new C[] {};
        int? count = a?[0]?.Val;
    }
}
";

            var expected = @"
class C
{
    int Val { get; set; }
    void f()
    {
        var a = new C[]{};
        int? count = ((object)a==null) ? (int?)null : ((object)a[0]==null) ? (int?)null : a[0].Val;
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDecomposeNullConditionalMemberAccessNestedInNullConditional()
        {
            var original = @"
class C
{
    class Inner { public int other { get; set; } }
    Inner inner = null;
    void f()
    {
        var c = new C();
        int? y = c?.inner?.other;
    }
}
";

            var expected = @"
class C
{
    class Inner { public int other { get; set; } }
    Inner inner = null;
    void f()
    {
        var c =new C();
        int? y= ((object)c==null) ? (int?)null : ((object)c.inner==null) ? (int?)null : c.inner.other;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }


        [TestMethod]
        public void TestDecomposeNullConditionalInvocationNestedInNullConditionalWithNullableType()
        {
            var original = @"
using System;

struct A
{
    public DateTime? BatchLPDate;
}

class Test
{
    public void f()
    {
        A? aaa = null;
        aaa?.BatchLPDate?.ToShortDateString();
    }
}
";

            var expected = @"
using System;

struct A
{
    public DateTime? BatchLPDate;
}

class Test
{
    public void f()
    {
        A? aaa = null;
        if ((object)aaa.Value != null) if ((object)aaa.Value.BatchLPDate.Value != null) aaa.Value.BatchLPDate.Value.ToShortDateString();
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDecomposeNullConditionalIndirectNestingInFunctionArgument()
        {
            var original = @"
using System;
using System.Linq;
using System.Collections.Generic;

public class Abc
{
    public string vvv<T>() { return null; }
    public Abc ggg() { return null; }
    public void fff()
    {
        List<Abc> aaa = null;
        var xxx = aaa.FirstOrDefault(ppp => ppp.ggg()?.vvv<string>() == ""mmm"")?.ggg();
    }
}
";

            var expected = @"
using System;
using System.Linq;
using System.Collections.Generic;

public class Abc
{
    public string vvv<T>() { return null; }
    public Abc ggg() { return null; }
    public void fff()
    {
        List<Abc> aaa = null;
        var xxx = ((object)aaa.FirstOrDefault(ppp=>((object)ppp.ggg()==null) ?null :ppp.ggg().vvv<string>()==""mmm"")==null) ?null :aaa.FirstOrDefault(ppp=>((object)ppp.ggg()==null) ?null :ppp.ggg().vvv<string>()==""mmm"").ggg();
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDecomposeNullConditionalInvocationAndNullConditionalMemberAccess()
        {
            var original = @"
class C
{
    int Val { get; set; }
    void f()
    {
        var c = new C();
        var z = c?.g()?.Val;
    }
    C g() { return null; }
}
";

            var expected = @"
class C
{
    int Val { get; set; }
    void f()
    {
         var c = new C();
         var z = ((object)c==null) ? (int?)null : ((object)c.g()==null) ? (int?)null:c.g().Val;
     }
     C g() { return null;}
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDecomposeNullConditionalInvocationAfterBinding()
        {
            var original = @"
class Abc
{
    class Xyz
    {
        public void gg() {}
    }

    Xyz xyz;

    void f()
    {
        var abc = new Abc();
        abc?.xyz.gg();
    }
}
";

            var expected = @"
class Abc
{
    class Xyz
    {
        public void gg() {}
    }

    Xyz xyz;

    void f()
    {
        var abc = new Abc();
        if((object)abc != null) abc.xyz.gg();
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDecomposeNullConditionalInvocationWithBaseNullableType()
        {
            var original = @"
using System;
using System.Collections.Generic;
using System.Linq;
public class TTT
{
    public DateTime? TrialEnd { get; set; }
    private void f()
    {
        var x =  this.TrialEnd?.AddHours(12);
    }
}
";

            var expected = @"
using System;
using System.Collections.Generic;
using System.Linq;
public class TTT
{
    public DateTime? TrialEnd { get; set; }
    private void f()
    {
        var x = ((object)this.TrialEnd.Value==null)?(System.DateTime?)null:this.TrialEnd.Value.AddHours(12);
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDecomposeNullConditionalInStatementAndExpression()
        {
            var original = @"
public delegate int Callback(int value, int other);

class Context
{
       public Callback Prog { get; set; }
}

partial class Abc
{
    public void bar(Context ctx)
    {
        int iii = 10;
        ctx.Prog?.Invoke(iii, ctx.Prog?.Invoke(iii, 0) == null ? 1 : 2);
    }

    class TrailingClass {}
    class TrailingClass2 {}
}
";

            var expected = @"
public delegate int Callback(int value, int other);

class Context
{
       public Callback Prog { get; set; }
}

partial class Abc
{
    public void bar(Context ctx)
    {
        int iii = 10;
        if((object)ctx.Prog!=null)  ctx.Prog.Invoke(iii, ((object)ctx.Prog==null)?(int?)null:ctx.Prog.Invoke(iii, 0) == null ? 1 : 2);
    }

    class TrailingClass {}
    class TrailingClass2 {}
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDecomposeNullConditionalMultidimArray()
        {
            var original = @"
using System;
using System.Collections.Generic;

public class Test
{
    public int[][] ddd;
    public void fff()
    {
        Test ooo = null;
        var ix = ooo?.ddd[123][543].ToString();
    }
}
";

            var expected = @"
using System;
using System.Collections.Generic;

public class Test
{
    public int[][] ddd;
    public void fff()
    {
        Test ooo = null;
        var ix=((object)ooo==null) ? null: ooo.ddd[123][543].ToString();
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDecomposeNullConditionalInvocationInActionAsStatementExpression()
        {
            var original = @"
using System;

public class Abc
{
    public Abc ggg(Action ppp) { return null; }
    public void hhh(Action<int> mmm) {}
    public string vvv<T>() { return null; }
    public void fff()
    {
        Abc aaa = null;
        ggg(() => aaa?.vvv<string>());
        ggg(() => { aaa?.vvv<string>(); });
        hhh(n => aaa?.vvv<string>());
    }
}
";

            var expected = @"
using System;

public class Abc
{
    public Abc ggg(Action ppp) { return null; }
    public void hhh(Action<int> mmm) {}
    public string vvv<T>() { return null; }
    public void fff()
    {
        Abc aaa = null;
        ggg(() => {if((object)aaa!=null)aaa.vvv<string>();});
        ggg(() => { if((object)aaa!=null)aaa.vvv<string>(); });
        hhh(n => {if((object)aaa!=null)aaa.vvv<string>();});
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDecomposeNullConditionalInvocationInActionNotAsStatementExpression()
        {
            var original = @"
using System;

public class Abc
{
    public Abc ggg(Func<string> ppp) { return null; }
    public string vvv<T>() { return null; }
    public void fff()
    {
        Abc aaa = null;
        ggg(() => aaa?.vvv<string>());
    }
}
";

            var expected = @"
using System;

public class Abc
{
    public Abc ggg(Func<string> ppp) { return null; }
    public string vvv<T>() { return null; }
    public void fff()
    {
        Abc aaa = null;
        ggg(()=>((object)aaa==null) ? null : aaa.vvv<string>());
    }
}
 ";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDecomposeNullConditionalFieldAccessInActionNotAsStatementExpression()
        {
            var original = @"
using System;

public class Abc
{
    public void ggg(Func<int?> mmm) {}
    public int rrr;
    public void fff()
    {
        Abc aaa = null;
        ggg(() => aaa?.rrr);
    }
}
";

            var expected = @"
using System;

public class Abc
{
    public void ggg(Func<int?> mmm) {}
    public int rrr;
    public void fff()
    {
        Abc aaa = null;
        ggg(()=>((object)aaa==null)?(int?)null:aaa.rrr);
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDecomposeNullConditionalWherePossibleNullResultIsRequired()
        {
            var original = @"
using System;

class A
{
    public int Vvv;
}

class Test
{
    void f()
    {
        A? a = null;
        var x = a?.Vvv ?? 33;
    }
}
";

            var expected = @"
using System;

class A
{
    public int Vvv;
}

class Test
{
    void f()
    {
        A? a = null;
        var x = ((object)a==null) ? (int?)null : a.Vvv ?? 33;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }
    }
}