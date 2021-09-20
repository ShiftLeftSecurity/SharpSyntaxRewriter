using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using SharpSyntaxRewriter.Rewriters;

namespace Tests
{
    [TestClass]
    public class TestImposeExplicitReturn : RewriterTester
    {
        protected override SyntaxTree ApplyRewrite(SyntaxTree tree, Compilation compilation)
        {
            ImposeExplicitReturn rw = new();
            return rw.Apply(tree, compilation.GetSemanticModel(tree));
        }

        [TestMethod]
        public void TestImposeExplicitReturnMethod()
        {
            var original = @"
public class Example
{
    public int asdf() { return 21; }
    private void Display() {}
    public void foo()
    {
        Display();
    }

    public void bar()
    {
        Display();
        return;
    }

    public void zem(int a)
    {
        if (a == 2)
            return;
        Display();
    }

    public void you(int b)
    {
        if (b == 2)
            return;
        Display();
        return;
    }
}
";
 
            var expected = @"
public class Example
{
    public int asdf() { return 21; }
    private void Display() { return; }
    public void foo()
    {
        Display();
    return; }

    public void bar()
    {
        Display();
        return;
    }

    public void zem(int a)
    {
        if (a == 2)
            return;
        Display();
    return; }

    public void you(int b)
    {
        if (b == 2)
            return;
        Display();
        return;
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImposeExplicitReturnPropertyAccessor()
        {
            var original = @"
public class Example
{
    public int Prop
    {
       get { return _v; }
       set { _v = value; }
    }
    private int _v;
}
";

            var expected = @"
public class Example
{
    public int Prop
    {
       get { return _v; }
       set { _v = value; return; }
    }
    private int _v;
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImposeExplicitReturnIndexerAccessor()
        {
            var original = @"
class CCC
{
    public int iii;
    public string sss;

    public string this[int qqq]
    {
        get { return ""foo""; }
        set { iii = qqq; sss = value; }
    }
}
";

            var expected = @"
class CCC
{
    public int iii;
    public string sss;

    public string this[int qqq]
    {
        get { return ""foo""; }
        set { iii = qqq; sss = value; return ;}
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImposeExplicitReturnLambdaWithBody()
        {
            // It's expected that lambdas with expression-body first
            // undergo expression-body desugaring (into blocks).

            var original = @"
using System;

class Test
{
    int f()
    {
        Action ccc = () => { f(); };
        return 1;
    }
}
";

            var expected = @"
using System;

class Test
{
    int f()
    {
        Action ccc = () => { f(); return; };
        return 1;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImposeExplicitReturnLocalFunction()
        {
            var original = @"
public class Test
{
    public void fff<T>(T[] array)
    {
        void sss(int jjj, T vvv) { array[jjj] = vvv; }
    }
}
";

            var expected = @"
public class Test
{
    public void fff<T>(T[] array)
    {
        void sss(int jjj, T vvv) { array[jjj] = vvv; return; }
    return; }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImposeExplicitReturnLocalFunctionIfElse()
        {
            var original = @"
using System;

public class Xyz<T> {}

public class Abc
{
    public void fff()
    {
        int lll() { if (true) return 123; else return 456; }
    }
}
";

            var expected = @"
using System;

public class Xyz<T> {}

public class Abc
{
    public void fff()
    {
        int lll() { if (true) return 123; else return 456; }
    return; }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImposeExplicitReturnNestedLocalFunctions()
        {
            var original = @"
public class Test
{
    public void fff<T>(T[] array)
    {
        int xxx;
        void lll(int ppp) { mmm(ppp); void mmm(int rrr) { xxx = rrr; } }
    }
}
";

            var expected = @"
public class Test
{
    public void fff<T>(T[] array)
    {
        int xxx;
        void lll(int ppp) { mmm(ppp); void mmm(int rrr) { xxx = rrr; return; } return; }
    return; }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImposeExplicitReturnInAsyncTaskResultMethod()
        {
            var original = @"
using System;
using System.Threading.Tasks;

public class Abc
{
    public async Task<int> ggg() { return 123; }
    public async Task fff()
    {
        var www = await ggg();
    }
}
";

            var expected = @"
using System;
using System.Threading.Tasks;

public class Abc
{
    public async Task<int> ggg() { return 123; }
    public async Task fff()
    {
        var www = await ggg();
    return; }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImposeExplicitReturnInTaskResultMethodWithException()
        {
            var original = @"
using System;
using System.Threading.Tasks;

public class Test
{
    public Task fff()
    {
        switch (1) {
            default:
                throw new Exception(""asdf"");
        }
    }
}
";

            var expected = @"
using System;
using System.Threading.Tasks;

public class Test
{
    public Task fff()
    {
        switch (1) {
            default:
                throw new Exception(""asdf"");
        }
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImposeExplicitReturnInLambdaWithinPropertyAccessor()
        {
            var original = @"
using System;
using System.Collections.Generic;
using System.Linq;

class Test
{
    void Process() { return; }
    decimal SubTotal
    {
        get {
            List<int> vvv = null;
            vvv.ForEach(ppp => { Process(); });
            return 1;
        }

    }
}
";

            var expected = @"
using System;
using System.Collections.Generic;
using System.Linq;

class Test
{
    void Process() { return; }
    decimal SubTotal
    {
        get {
            List<int> vvv = null;
            vvv.ForEach(ppp => { Process(); return; });
            return 1;
        }

    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }
    }
}
