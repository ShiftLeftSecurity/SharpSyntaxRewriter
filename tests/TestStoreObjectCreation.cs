using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using SharpSyntaxRewriter.Rewriters;
namespace Tests
{
    [TestClass]
    public class TestStoreObjectCreation : RewriterTester
    {
        protected override SyntaxTree ApplyRewrite(SyntaxTree tree, Compilation compilation)
        {
            StoreObjectCreation rw = new();
            return rw.Apply(tree, compilation.GetSemanticModel(tree));
        }

        [TestMethod]
        public void TestStoreObjectCreationForFieldAssignment()
        {
            var original = @"
class Abc
{
    public Program ppp;
}

class Program
{
    public static void Main(string[] args)
    {
        Abc abc = null;
        abc.ppp = new Program();
    }
}
";

            var expected = @"
class Abc
{
    public Program ppp;
}

class Program
{
    public static void Main(string[] args)
    {
        Abc abc = null;
        Program ____obj_0 = new Program(); abc.ppp = ____obj_0;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestStoreObjectCreationForFieldAssignmentTargetTypedNew()
        {
            var original = @"
class Abc
{
    public Program ppp;
}

class Program
{
    public static void Main(string[] args)
    {
        Abc abc = null;
        abc.ppp = new();
    }
}
";

            var expected = @"
class Abc
{
    public Program ppp;
}

class Program
{
    public static void Main(string[] args)
    {
        Abc abc = null;
        Program ____obj_0 = new(); abc.ppp = ____obj_0;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestStoreObjectCreationForThisFieldAssignment()
        {
            var original = @"
public class Abc {}
public class Xpt
{
    public Abc vvv;
    public void fff()
    {
        vvv = new Abc();
    }
}
";

            var expected = @"
public class Abc {}
public class Xpt
{
    public Abc vvv;
    public void fff()
    {
        Abc ____obj_0 = new Abc(); vvv = ____obj_0;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestStoreObjectCreationForThisFieldAssignmentTargetTypedNew()
        {
            var original = @"
public class Abc {}
public class Xpt
{
    public Abc vvv;
    public void fff()
    {
        vvv = new();
    }
}
";

            var expected = @"
public class Abc {}
public class Xpt
{
    public Abc vvv;
    public void fff()
    {
        Abc ____obj_0 = new(); vvv = ____obj_0;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestStoreObjectCreationForStaticPropertyAssignment()
        {
            var original = @"
public class Abc {}
public class Xpt
{
    public static Abc vvv { get; set; }
    public static void fff()
    {
        vvv = new Abc();
    }
}
";

            var expected = @"
public class Abc {}
public class Xpt
{
    public static Abc vvv { get; set; }
    public static void fff()
    {
        Abc ____obj_0 = new Abc();vvv = ____obj_0;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestStoreObjectCreationForIndexedArrayAssignment()
        {
            var original = @"
public class Xpt
{
    public static void fff()
    {
        Xpt[] aaa = null;
        aaa[0] = new Xpt();
    }
}
";

            var expected = @"
public class Xpt
{
    public static void fff()
    {
        Xpt[] aaa = null;
        Xpt ____obj_0 = new Xpt(); aaa[0] = ____obj_0;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestStoreObjectCreationForIndexedArrayOfPropertyAssignment()
        {
            var original = @"
public class Xpt
{
    public Xpt[] _aaa;
    public Xpt[] aaa { get { return _aaa; } set { _aaa = value; } }
    public void fff()
    {
        aaa[0] = new Xpt();
    }
}
";

            var expected = @"
public class Xpt
{
    public Xpt[] _aaa;
    public Xpt[] aaa { get { return _aaa; } set { _aaa = value; } }
    public void fff()
    {
        Xpt ____obj_0 = new Xpt(); aaa[0] = ____obj_0;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestStoreObjectCreationForDeclarator()
        {
            var original = @"
class Program
{
    private int val;
    public void Foo()
    {
        Program obj = new Program() { val = 42 };
    }
}
";

            var expected = @"
class Program
{
    private int val;
    public void Foo()
    {
        Program obj = new Program() { val = 42 };
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestStoreObjectCreationForLocalAssignment()
        {
            var original = @"
class Program
{
    private int val;
    public void Foo()
    {
        Program obj = null;
        obj = new Program() { val = 42 };
    }
}
";

            var expected = @"
class Program
{
    private int val;
    public void Foo()
    {
        Program obj = null;
        obj = new Program() { val = 42 };
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestStoreObjectCreationForInitializer()
        {
            var original = @"
public class Point
{
    int x, y;
    public int X { get { return x; } set { x = value; } }
    public int Y { get { return y; } set { y = value; } }
}
public class Rectangle
{
    Point p1, p2;
    public Point P1 { get { return p1; } set { p1 = value; } }
    public Point P2 { get { return p2; } set { p2 = value; } }
}
class Program
{
    public void Foo()
    {
        Rectangle r = new Rectangle
        {
            P1 = new Point { X = 0, Y = 1 },
            P2 = new Point { X = 2, Y = 3 }
        };
    }
}
";

            var expected = @"
public class Point
{
    int x, y;
    public int X { get { return x; } set { x = value; } }
    public int Y { get { return y; } set { y = value; } }
}
public class Rectangle
{
    Point p1, p2;
    public Point P1 { get { return p1; } set { p1 = value; } }
    public Point P2 { get { return p2; } set { p2 = value; } }
}
class Program
{
    public void Foo()
    {
        Point ____obj_0=new Point{X=0,Y=1}; Point ____obj_1=newPoint{X=2,Y=3}; Rectangle r = new Rectangle
        {
            P1=____obj_0,
            P2=____obj_1
        };
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestStoreObjectCreationForFunctionArgument()
        {
            var original = @"
public class Program
{
    public static void Sink(Program paramSink) {}
    public static void Main(string[] args)
    {
        Sink(new Program());
    }
}
";
            var expected = @"
public class Program
{
    public static void Sink(Program paramSink) {}
    public static void Main(string[] args)
    {
        Program ____obj_0 = new Program();Sink(____obj_0 );
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestStoreObjectCreationForFunctionArgumentTargetTypeNew()
        {
            var original = @"
public class Program
{
    public static void Sink(Program paramSink) {}
    public static void Main(string[] args)
    {
        Sink(new());
    }
}
";
            var expected = @"
public class Program
{
    public static void Sink(Program paramSink) {}
    public static void Main(string[] args)
    {
        Program ____obj_0 = new ();Sink(____obj_0 );
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestStoreObjectCreationForTargetTypeNewAndInitOnlyProperty()
        {
            var original = @"
class Program
{
    public void Sink(Program paramSink) {}

    public string N { get; init; }

    public void f()
    {
        Sink(new () { N = ""aaa"" });
    }
}
";

            var expected = @"
class Program
{
    public void Sink(Program paramSink) {}

    public string N { get; init; }

    public void f()
    {
        Program ____obj_0 = new(){N=""aaa""}; Sink(____obj_0);
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestStoreObjectCreationArrayForFunctionArgument()
        {
            var original = @"
class Program
{
    public void Bar(Program[] many) {}
    public void Foo()
    {
        Bar(new Program[11]);
    }
}
";

            var expected = @"
class Program
{
    public void Bar(Program[] many) {}
    public void Foo()
    {
        Program[] ____obj_0 = new Program[11];Bar(____obj_0);
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestStoreObjectCreationArrayWithRankForFunctionArgument()
        {
            var original = @"
public class Test
{
    public Test(int[] ppp) {}

    public void fff()
    {
        int aaa = 10, bbb = 10;
        new Test(new int[aaa + bbb]);
    }
}
";

            var expected = @"
public class Test
{
    public Test(int[] ppp) {}

    public void fff()
    {
        int aaa = 10, bbb = 10;
        int[] ____obj_0=new int[aaa+bbb];Test ____obj_1=new Test(____obj_0);new Test(new int[aaa + bbb]);
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestStoreObjectCreationImplicitArrayForFunctionArgument()
        {
            var original = @"
class Program
{
    public void Bar(Program[] many) {}
    public void Foo()
    {
        Bar(new [] { new Program() });
    }
}
";

            var expected = @"
class Program
{
    public void Bar(Program[] many) {}
    public void Foo()
    {
        Program ____obj_0 =new Program();Program[] ____obj_1=new[]{____obj_0};Bar(____obj_1);
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestStoreObjectCreationThroughInterface()
        {
            var original = @"
public interface IFoo {}
public class Foo : IFoo { public int what; }
public interface IBar {}
public class Bar : IBar { public IFoo TheFoo; }

public class Test
{
    public void fff()
    {
        IBar ibar = new Bar
        {
            TheFoo = new Foo { what = 123 }
        };
    }
}
";
            var expected = @"
public interface IFoo {}
public class Foo : IFoo { public int what; }
public interface IBar {}
public class Bar : IBar { public IFoo TheFoo; }

public class Test
{
    public void fff()
    {
        Foo ____obj_0=new Foo{what=123}; IBar ibar = new Bar
        {
            TheFoo=____obj_0
        };
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestStoreObjectCreationForFunctionArgumentInSubexpression()
        {
            var original = @"
using System;

public class Xpt
{
    public int jjj;
}

public class Abc
{
    public int iii;

    public int ggg(Xpt ppp) { return 0; }

    public void fff()
    {
        var ooo = new Abc
        {
            iii = ggg(new Xpt { jjj = 99 })
        };
    }
}
";

            var expected = @"
using System;

public class Xpt
{
    public int jjj;
}

public class Abc
{
    public int iii;

    public int ggg(Xpt ppp) { return 0; }

    public void fff()
    {
        Xpt ____obj_0=new Xpt{jjj=99}; var ooo=new Abc
        {
            iii=ggg(____obj_0)
        };
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestStoreObjectCreationArrayForFunctionArgumentInSubexpression()
        {
            var original = @"
using System;
using System.Linq;
using System.Collections.Generic;

class Abc
{
    IList<string> lll;
    void fff()
    {
        string mmm = null;
        var retVal = new Abc
        {
            lll = mmm.Split(new [] { ',' })
        };
    }
}
";

            var expected = @"
using System;
using System.Linq;
using System.Collections.Generic;

class Abc
{
    IList<string> lll;
    void fff()
    {
        string mmm = null;
        char[] ____obj_0=new[]{','}; var retVal=new Abc
        {
            lll=mmm.Split(____obj_0)
        };
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestStoreObjectCreationForNestedObjectCreationInSubexpression()
        {
            var original = @"
using System;
using System.Xml;
using System.Xml.Linq;

public class Test
{
    void Bar()
    {
        XDocument document = null;
        var root = document.Root;
        root.Add(new XElement(""CreditCard"", new XElement(""Username"", null)));
    }
}
";

            var expected = @"
using System;
using System.Xml;
using System.Xml.Linq;

public class Test
{
    void Bar()
    {
        XDocument document = null;
        var root = document.Root;
        XElement ____obj_0=new XElement(""Username"",null);XElement ____obj_1=new XElement(""CreditCard"",____obj_0);root.Add(____obj_1);
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestStoreObjectCreationForBaseClassInitialization()
        {
            var original = @"
public class Data { public int Number; }
public class Other
{
    public Other(Data data) {}
}
public class Abc : Other
{
    Data data;
    public Abc() : base(new Data { Number = 123 }) {}
}
";

            var expected = @"
public class Data { public int Number; }
public class Other
{
    public Other(Data data) {}
}
public class Abc : Other
{
    Data data;
    public Abc() : base(new Data { Number = 123 }) {}
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestStoreObjectCreationOutsideBlock()
        {
            var original = @"
using System;
using System.Collections.Generic;

public class Abc
{
    public Abc(Abc adf) {}
    public void fff()
    {
        List<Abc> ooo = null, iii = null;
        foreach (Abc cookie in ooo)
            iii.Add(new Abc(cookie));
    }
}
";

            var expected = @"
using System;
using System.Collections.Generic;

public class Abc
{
    public Abc(Abc adf) {}
    public void fff()
    {
        List<Abc> ooo = null, iii = null;
        foreach (Abc cookie in ooo)
        { Abc ____obj_0=new Abc(cookie);  iii.Add(____obj_0); }
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestStoreObjectCreationInsideNoBlockSwitchSectionUsedByFlowAfter()
        {
            var original = @"
using System;

public class Foo {}

public class Abc
{
    public Abc(Foo ooo) {}
    public void fff()
    {
        Abc iii = null;
        switch (10) {
            case 10:
                var aaa = new Abc(new Foo());
                iii = aaa;
                break;
        }
    }
}
";

            var expected = @"
using System;

public class Foo {}

public class Abc
{
    public Abc(Foo ooo) {}
    public void fff()
    {
        Abc iii = null;
        switch (10) {
            case 10:
                Foo ____obj_0=new Foo(); var aaa=new Abc(____obj_0);
                iii=aaa;
                break;
        }
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestStoreObjectCreationInsideNoBlockSwitchSectionUsesFlowFromBefore()
        {
            var original = @"
using System;

public class Xpo
{
    public Xpo(int ppp) {}
}

public class Abc
{
    public Xpo fff()
    {
        switch (111)
        {
            case 222:
                var nnn = 999;
                return new Xpo(nnn);
        }
        return null;
    }
}
";

            var expected = @"
using System;

public class Xpo
{
    public Xpo(int ppp) {}
}

public class Abc
{
    public Xpo fff()
    {
        switch (111)
        {
            case 222:
                var nnn = 999;
                Xpo ____obj_0=new Xpo(nnn); return ____obj_0;
        }
        return null;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestStoreObjectCreationInsideNoBlockSwitchSectionReuseLocalDeclaration()
        {
            var original = @"
using System;

public class Xpo
{
    public Xpo(int ppp) {}
}

public class Abc
{
    public Xpo fff()
    {
        switch (111)
        {
            case 222:
                var nnn = 999;
                return new Xpo(nnn);
            case 333:
                nnn = 999;
                return new Xpo(nnn);
        }
        return null;
    }
}
";

            var expected = @"
using System;

public class Xpo
{
    public Xpo(int ppp) {}
}

public class Abc
{
    public Xpo fff()
    {
        switch (111)
        {
            case 222:
                var nnn = 999;
                Xpo ____obj_0=new Xpo(nnn); return ____obj_0;
            case 333:
                nnn = 999;
                Xpo ____obj_1=new Xpo(nnn);return ____obj_1;
        }
        return null;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestStoreObjectCreationAsExpressionStatement()
        {
            var original = @"
public class Foo {}
public class Abc
{
    public void fff()
    {
        new Foo();
    }
}
";

            var expected = @"
public class Foo {}
public class Abc
{
    public void fff()
    {
        Foo ____obj_0 = new Foo();new Foo();
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestStoreObjectCreationWithNewObjectArgumentAsExpressionStatement()
        {
            var original = @"
public class Test
{
    public Test(int ppp) {}

    public void fff()
    {
        new Test(new int());
    }
}
";

            var expected = @"
public class Test
{
    public Test(int ppp) {}

    public void fff()
    {
        int ____obj_0=new int();Test ____obj_1=newTest(____obj_0);new Test(new int());
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestStoreObjectCreationInCompoundAssignment()
        {
            var original = @"
public class Velocity
{
    public Velocity(double value) {}

    public static Velocity operator +(Velocity first, Velocity second)
    {
        return null;
    }
}

public class Abc
{
    public void fff()
    {
        Velocity ms = new Velocity(52.34);
        ms += new Velocity(3.123);
    }
}
";

            var expected = @"
public class Velocity
{
    public Velocity(double value) {}

    public static Velocity operator+(Velocity first, Velocity second)
    {
        return null;
    }
}

public class Abc
{
    public void fff()
    {
        Velocity ms = new Velocity(52.34);
        ms += new Velocity(3.123);
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestStoreObjectCreationAsIfStatementCondition()
        {

            var original = @"
class Test
{
    static bool h(object ooo) { return false; }
    public void f()
    {
        int kk = 0;
        if (h(new[] { ""foo"" }))
        {
            kk = 99;
        }
    }
}
";

            var expected = @"
class Test
{
    static bool h(object ooo) { return false; }
    public void f()
    {
        int kk = 0;
        string[] ____obj_0=new[]{""foo""}; if(h(____obj_0))
        {
            kk=99;
        }
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestStoreObjectCreationAsTypeParameter()
        {
            var original = @"
public class Abc {}

public class Test<TTT>  where TTT : Abc, new()
{
    public TTT fff()
    {
        return new TTT();
    }
}
";

            var expected = @"
public class Abc {}

public class Test<TTT>  where TTT : Abc, new()
{
    public TTT fff()
    {
        TTT ____obj_0=new TTT(); return ____obj_0;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestStoreObjectCreationAsIndexedElement()
        {
            var original = @"
public class Abc
{
    public void fff()
    {
        System.Collections.Generic.Dictionary<int, Abc> ddd = null;
        ddd[0] = new Abc();
    }
}
";

            var expected = @"
public class Abc
{
    public void fff()
    {
        System.Collections.Generic.Dictionary<int, Abc> ddd = null;
        Abc ____obj_0  = new Abc();ddd[0] = ____obj_0;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestStoreObjectCreationPreserveTrivia1()
        {
            var original = @"
public class Aaa
{
    public class Bbb
    {
        int One;
        int Two;
        public Bbb(int One,int Two) {}
    }

    int bar(object ooo) { return 123; }
    void foo()
    {
        int vvv;
        vvv = this.bar(new Bbb
            (
                111,
                222
            ));
    }
}
";

            var expected = @"
public class Aaa
{
    public class Bbb
    {
        int One;
        int Two;
        public Bbb(int One,int Two) {}
    }

    int bar(object ooo) { return 123; }
    void foo()
    {
        int vvv;
        Bbb ____obj_0 = new Bbb
            (
                111,
                222
            ); vvv = this.bar(____obj_0);
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestStoreObjectCreationAlreadyStoredSwitchExpressionObject()
        {
            var original = @"
class CCC
{
    enum Rainbow { Red, Orange }

    void fff()
    {
        var rrr = Rainbow.Red;
        var www = rrr switch
        {
            Rainbow.Red => 111,
            Rainbow.Orange => 222,
        };
    }
}
";

            var expected = @"
class CCC
{
    enum Rainbow { Red, Orange }

    void fff()
    {
        var rrr = Rainbow.Red;
        var www = rrr switch
        {
            Rainbow.Red => 111,
            Rainbow.Orange => 222,
        };
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestStoreObjectCreationArraySwitchExpressionObjectAsReturn()
        {
            var original = @"
class CCC
{
    enum Rainbow { Red, Orange }

    string kkk(Rainbow jjj)
    {
        return jjj switch
        {
            Rainbow.Red => ""red"",
            Rainbow.Orange => ""orange"",
            _ => null,
        };
    }
}
";

            var expected = @"
classCCC
{
    enum Rainbow { Red,Orange }

    string kkk(Rainbow jjj)
    {
        string ____obj_0 = jjj switch
        {
            Rainbow.Red=>""red"",
            Rainbow.Orange=>""orange"",
            _ => null,
        }; return____obj_0;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestStoreObjectCreationArrayStackallocVariations()
        {
            var original = @"
using System;

public class CCC
{
    public void ggg(Span<int> ppp) {}

    public void fff()
    {
        int lll = 3;
        ggg(stackalloc int[lll]);
        ggg(stackalloc[] { 123, 456 });
        ggg(stackalloc int[2] { 111, 333 });
    }
}
";

            var expected = @"
using System;

public class CCC
{
    public void ggg(Span<int> ppp) {}

    public void fff()
    {
        int lll = 3;
        Span<int> ____obj_0=stackalloc int[lll];ggg(____obj_0);
        Span<int> ____obj_1=stackalloc[]{123,456};ggg(____obj_1);
        Span<int> ____obj_2=stackalloc int[2]{111,333}; ggg(____obj_2);
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }
    }
}
