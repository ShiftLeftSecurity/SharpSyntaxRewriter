using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using SharpSyntaxRewriter.Rewriters;

namespace Tests
{
    [TestClass]
    public class TestInitializeOutArgument : RewriterTester
    {
        protected override SyntaxTree ApplyRewrite(SyntaxTree tree, Compilation compilation)
        {
            InitializeOutArgument rw = new();
            return rw.Apply(tree, compilation.GetSemanticModel(tree));
        }

        [TestMethod]
        public void TestInitializeOutArgumentOutParameter()
        {
            var original = @"
using System;

public class Abc
{
    public bool ggg(out int xxx) { xxx = 123; return true; }

    public void fff()
    {
        int mmm;
        var ccc = ggg(out mmm);
    }
}
";

            var expected = @"
using System;

public class Abc
{
    public bool ggg(out int xxx) { xxx = 123; return true; }

    public void fff()
    {
        int mmm;
        mmm = default; var ccc = ggg(out mmm);
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestInitializeOutArgumentUnderscoreOutArgument()
        {
            var original = @"
public class Abc
{
    public void ggg(out int ppp) { ppp = 222;}

    public void fff()
    {
        ggg(out _);
    }
}
";

            var expected = @"
public class Abc
{
    public void ggg(out int ppp) { ppp = 222;}

    public void fff()
    {
        ggg(out _);
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestInitializeOutArgumentOutParameterInferredTypeSpec()
        {
            // In this case, we wouldn't need to initialize, but with the current logic a
            // previous initialization isn't checked (it'd be more expensive), so we
            // just assign to it anyways -- shouldn't impact data flow, given that the
            // `out' argument will be assigne anyways.

            var original = @"
using System;

public class Abc
{
    public bool ggg(out int xxx) { xxx = 123; return true; }

    public void fff()
    {
        var mmm = 123;
        var ccc = ggg(out mmm);
    }
}
";

            var expected = @"
using System;

public class Abc
{
    public bool ggg(out int xxx) { xxx = 123; return true; }

    public void fff()
    {
        var mmm = 123;
        mmm = default; var ccc = ggg(out mmm);
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestInitializeOutArgumentOutParameterNoSideEffects()
        {
            var original = @"
public class Abc
{
    public void ggg(out int ppp) { ppp = 999; }
    public void hhh(ref int qqq) { qqq = 888; }
    public void uuu(int www) {}

    public void fff()
    {
        int iii; // may be uninitialized
        ggg(out iii);

        int mmm = 0;
        hhh(ref mmm);

        uuu(mmm);
    }
}
";

            var expected = @"
public class Abc
{
    public void ggg(out int ppp) { ppp = 999; }
    public void hhh(ref int qqq) { qqq = 888; }
    public void uuu(int www) {}

    public void fff()
    {
        int iii; // may be uninitialized
        iii = default; ggg(out iii);

        int mmm = 0;
        hhh(ref mmm);

        uuu(mmm);
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestInitializeOutArgumentWithVarAndInt()
        {
            var original = @"
using System;

public class Abc
{
    public bool ggg(out int xxx) { xxx = 123; return true; }

    public void fff()
    {
        var aaa = ggg(out var iii);
        var bbb = ggg(out int jjj);
    }
}
";

            var expected = @"
using System;

public class Abc
{
    public bool ggg(out int xxx) { xxx = 123; return true; }

    public void fff()
    {
        int iii=default(int);var aaa = ggg(out iii);
        int jjj=default(int);var bbb = ggg(out jjj);
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestInitializeOutArgumentInStatementWithoutBlock()
        {
            var original = @"
using System;

public class Abc
{
    public bool ggg(out int xxx) { xxx = 123; return true; }
    public void hhh(int aaa) {}

    public void fff()
    {
        if (true)
            ggg(out var iii);

        if (true)
            ggg(out var iii);
    }
}
";

            var expected = @"
using System;

public class Abc
{
    public bool ggg(out int xxx) { xxx = 123; return true; }
    public void hhh(int aaa) {}

    public void fff()
    {
        if (true)
        { int iii = default(int); ggg(out iii); }

        if (true)
        { int iii = default(int); ggg(out iii); }
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestInitializeOutArgumentReuseArgumentInConditional()
        {
            var original = @"
using System;

public class Abc
{
    public Abc(int ppp) {}

    public bool ggg(out int xxx) { xxx = 123; return true; }

    public void fff()
    {
        var aaa = ggg(out var iii) ? new Abc(iii) : null;
        var bbb = ggg(out int jjj) ? new Abc(jjj) : null;
    }
}
";

            var expected = @"
using System;

public class Abc
{
    public Abc(int ppp) {}

    public bool ggg(out int xxx) { xxx = 123; return true; }

    public void fff()
    {
        int iii=default(int); var aaa = ggg(out iii) ? new Abc(iii) : null;
        int jjj=default(int); var bbb = ggg(out jjj) ? new Abc(jjj) : null;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestInitializeOutArgumentReuseArgumentInList()
        {
            var original = @"
using System;

public class Abc
{
    public int ggg(out int xxx) { xxx = 123; return xxx; }

    public void fff()
    {
        var ooo = new int[] { ggg(out var mmm), ggg(out var iii) };
    }
}
";

            var expected = @"
using System;

public class Abc
{
    public int ggg(out int xxx) { xxx = 123; return xxx; }

    public void fff()
    {
        int mmm=default(int); int iii=default(int);var ooo = new int[] { ggg(out mmm), ggg(out iii) };
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestInitializeOutArgumentArgumentInSwitchSection()
        {
            var original = @"
using System;

public class Abc
{
    public void fff()
    {
        {
            int value = 88;
        }

        switch (99)
        {
            case (11):
                int.TryParse(""asdf"", out var value);
        }
    }
}
";

            var expected = @"
using System;

public class Abc
{
    public void fff()
    {
        {
            int value = 88;
        }

        switch (99)
        {
            case (11):
                int value = default(int);int.TryParse(""asdf"", out value);
        }
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestInitializeOutArgumentReuseArgumentInSwitchSection()
        {
            var original = @"
using System;

public class Abc
{
    public bool ggg(out int xxx) { xxx = 123; return true; }
    public void hhh(int aaa) {}

    public void fff()
    {
        switch (11)
        {
            case 99:
                ggg(out var iii);
                hhh(iii);
                break;
        }
    }
}
";

            var expected = @"
using System;

public class Abc
{
    public bool ggg(out int xxx) { xxx = 123; return true; }
    public void hhh(int aaa) {}

    public void fff()
    {
        switch (11)
        {
            case 99:
                int iii=default(int);ggg(out iii);
                hhh(iii);
                break;
        }
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestInitializeOutArgumentReuseArgumentInSwitchSectionTwice()
        {
            var original = @"
using System;

public class Abc
{
    public bool ggg(out int xxx) { xxx = 123; return true; }
    public void hhh(int aaa) {}

    public void fff()
    {
        switch (11)
        {
            case 99:
                ggg(out var iii);
                hhh(iii);
                break;

            case 88:
                hhh(iii);
                break;
        }
    }
}
";

            var expected = @"
using System;

public class Abc
{
    public bool ggg(out int xxx) { xxx = 123; return true; }
    public void hhh(int aaa) {}

    public void fff()
    {
        switch (11)
        {
            case 99:
                int iii=default(int);ggg(out iii);
                hhh(iii);
                break;

            case 88:
                hhh(iii);
                break;
        }
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestInitializeDeclaringOutTupleParameter()
        {
            var original = @"
using System;

public class Abc
{
    public void hhh(out (int , int ) o) { o.Item1 = 0; o.Item2 = 0;}

    public void fff()
    {
        hhh(out (int vv1, int vv2) ttt);
        hhh(out (int vv11, int) tt);
        hhh(out (int, int) q);
    }
}
";

            var expected = @"
using System;

public class Abc
{
    public void hhh(out (int , int ) o) { o.Item1 = 0; o.Item2 = 0;}

    public void fff()
    {
        (int vv1,int vv2)ttt=default((int vv1,int vv2));        hhh(out ttt);
        (int vv11,int)tt=default((intvv11,int));        hhh(outtt);
        (int,int)q=default((int,int));        hhh(outq);
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestInitializeOutArgumentDeclaringTupleExpression()
        {
            var original = @"
class Abc
{
    public void fff()
    {
        (int xxx, int yyy) = (111, 222);
        var _xxx = xxx;
        var _yyy = yyy;
    }
}
";

            var expected = @"
class Abc
{
    public void fff()
    {
        int xxx = default(int); int yyy = default(int); (xxx, yyy) = (111, 222);
        var _xxx = xxx;
        var _yyy = yyy;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestInitializeOutArgumentDeclaringNestedTupleExpression()
        {
            var original = @"
using System.Linq;
using System.Collections.Generic;
public class Abc
{
    public void fff()
    {
        Dictionary<string, (int, string)> ddd = null;
        (var aaa, (var bbb, var ccc)) = ddd.ToList()[0];
    }
}
";

            var expected = @"
using System.Linq;
using System.Collections.Generic;
public class Abc
{
    public void fff()
    {
        Dictionary<string, (int, string)> ddd = null;
        string aaa=default(string); int bbb=default(int); string ccc=default(string); (aaa, (bbb,ccc)) = ddd.ToList()[0];
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }


        [TestMethod]
        public void TestInitializeOutArgumentDeclaringTupleExpressionInferredTypeSpec()
        {
            var original = @"
class Abc
{
    public void fff()
    {
        (var xxx, var yyy) = (111, 222);
        var _xxx = xxx;
        var _yyy = yyy;
    }
}
";

            var expected = @"
class Abc
{
    public void fff()
    {
        int xxx = default(int); int yyy = default(int); (xxx, yyy) = (111, 222);
        var _xxx = xxx;
        var _yyy = yyy;
    }
}
";

            TestRewrite_LineIgnore(original, expected);
        }

        [TestMethod]
        public void TestInitializeOutArgumentDiscardDesignation()
        {
            var original = @"
using System;

public class Abc
{
    public void ggg(out int  ppp) { ppp = 123; }
    public void fff()
    {
        ggg(out var _);
    }
}
";

            var expected = @"
using System;

public class Abc
{
    public void ggg(out int  ppp) { ppp = 123; }
    public void fff()
    {
        ggg(out var _);
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }
    }
}
