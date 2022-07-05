using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpSyntaxRewriter.Rewriters;

namespace Tests
{
    [TestClass]
    public class TestReplicateLocalInitialization : RewriterTester
    {
        protected override SyntaxTree ApplyRewrite(SyntaxTree tree, Compilation compilation)
        {
            ReplicateLocalInitialization rw = new();
            return rw.Apply(tree, compilation.GetSemanticModel(tree));
        }

        [TestMethod]
        public void TestReplicateObjectInitializer()
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
        Program obj = new Program() { val = 42 }; obj.val = 42;
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateObjectInitializerBlockTriviaSameLine()
        {
            var original = @"
class Program
{
    private int val;
    public void Foo() {
        Program obj = new Program() { val = 42 };
    }
}
";
            var expected = @"
class Program
{
    private int val;
    public void Foo() {
        Program obj = new Program() { val = 42 }; obj.val = 42;
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateObjectInitializerInitNextLine()
        {
            var original = @"
class Program
{
    private int val;
    public void Foo()
    {
        Program obj = new Program() {
            val = 42 };
    }
}
";
            var expected = @"
class Program
{
    private int val;
    public void Foo()
    {
        Program obj = new Program() {
            val = 42 }; obj.val = 42;
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateObjectInitializerInitNextLine2()
        {
            var original = @"
class Program
{
    private int val;
    public void Foo()
    {
        Program obj = new Program() {
            val = 42
        };
    }
}
";
            var expected = @"
class Program
{
    private int val;
    public void Foo()
    {
        Program obj = new Program() {
            val = 42
        }; obj.val = 42;
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateObjectInitializerMultipeElements()
        {
            var original = @"
class StudentName
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int ID { get; set; }
}

class CollInit
{
    void foo()
    {
        var student = new StudentName()
        {
            FirstName = ""Sachin"",
            LastName = ""Karnik"",
            ID=211
        };
    }
}
";

            var expected = @"
class StudentName
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int ID { get; set; }
}

class CollInit
{
    void foo()
    {
        var student = new StudentName()
        {
            FirstName = ""Sachin"",
            LastName = ""Karnik"",
            ID=211
        }; student.ID=211; student.LastName=""Karnik""; student.FirstName=""Sachin"";
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateObjectInitializerNestedPreserveTrivia1()
        {
            var original = @"
class Num
{
    public int One {get;set;}
    public int Two {get;set;}
}
class Aaa
{
    public Num Ppp {get;set;}
    public int xxx {get;set;}
}

class CollInit
{
    void foo()
    {
        Num obj;
        Aaa iii = new Aaa()
        {
            Ppp = (obj = new Num
            {
                One = 111,
                Two = 222
            }),
            xxx = 888
        };
    }
}
";

            var expected = @"
class Num
{
    public int One {get;set;}
    public int Two {get;set;}
}
class Aaa
{
    public Num Ppp {get;set;}
    public int xxx {get;set;}
}

class CollInit
{
    void foo()
    {
        Num obj;
        Aaa iii = new Aaa()
        {
            Ppp = (obj = new Num
            {
                One = 111,
                Two = 222
            }),
            xxx = 888
        };iii.xxx=888; iii.Ppp=(obj=new Num{One=111,Two=222});obj.Two=222;obj.One=111;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateObjectInitializerNestedPreserveTrivia2()
        {
            var original = @"
class Num
{
    public int One {get;set;}
    public int Two {get;set;}
}
class Aaa
{
    public Num Ppp {get;set;}
    public int xxx {get;set;}
}

class CollInit
{
    void foo()
    {
        Num obj;
        Aaa iii = new Aaa()
        {
            Ppp = (obj = new Num
            {
                One = 111
                , Two = 222
            }),
            xxx = 888
        };
    }
}
";

            var expected = @"
class Num
{
    public int One {get;set;}
    public int Two {get;set;}
}
class Aaa
{
    public Num Ppp {get;set;}
    public int xxx {get;set;}
}

class CollInit
{
    void foo()
    {
        Num obj;
        Aaa iii = new Aaa()
        {
            Ppp = (obj = new Num
            {
                One = 111
                , Two = 222
            }),
            xxx = 888
        };iii.xxx=888; iii.Ppp=(obj=new Num{One=111,Two=222});obj.Two=222;obj.One=111;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateObjectInitializerNestedPreserveTrivia3()
        {
            var original = @"
class Num
{
    public int One {get;set;}
    public int Two {get;set;}
}
class Aaa
{
    public Num Ppp {get;set;}
    public int xxx {get;set;}
}

class CollInit
{
    void foo()
    {
        Num obj;
        Aaa iii = new Aaa()
        {
            Ppp = (obj = new Num
            {
                One = 111,  Two = 222
            }),
            xxx = 888
        };
    }
}
";

            var expected = @"
class Num
{
    public int One {get;set;}
    public int Two {get;set;}
}
class Aaa
{
    public Num Ppp {get;set;}
    public int xxx {get;set;}
}

class CollInit
{
    void foo()
    {
        Num obj;
        Aaa iii = new Aaa()
        {
            Ppp = (obj = new Num
            {
                One = 111, Two = 222
            }),
            xxx = 888
        };iii.xxx=888; iii.Ppp=(obj=new Num{One=111,Two=222});obj.Two=222;obj.One=111;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateObjectInitializerNestedPreserveTrivia4()
        {
            var original = @"
class Num
{
    public int One {get;set;}
    public int Two {get;set;}
}
class Aaa
{
    public Num Ppp {get;set;}
    public int xxx {get;set;}
}

class CollInit
{
    void foo()
    {
        Num obj;
        Aaa iii = new Aaa()
        {
            Ppp = (obj = new Num
            {
                One = 111,
                Two = 222
            }), xxx = 888
        };
    }
}
";

            var expected = @"
class Num
{
    public int One {get;set;}
    public int Two {get;set;}
}
class Aaa
{
    public Num Ppp {get;set;}
    public int xxx {get;set;}
}

class CollInit
{
    void foo()
    {
        Num obj;
        Aaa iii = new Aaa()
        {
            Ppp = (obj = new Num
            {
                One = 111,
                Two = 222
            }), xxx = 888
        };iii.xxx=888; iii.Ppp=(obj=new Num{One=111,Two=222});obj.Two=222;obj.One=111;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateObjectInitializerNestedPreserveTrivia5()
        {
            var original = @"
class Num
{
    public int One {get;set;}
    public int Two {get;set;}
}
class Aaa
{
    public Num Ppp {get;set;}
    public int xxx {get;set;}
}

class CollInit
{
    void foo()
    {
        Num obj;
        Aaa iii = new Aaa()
        {
            Ppp = (obj = new Num {
                One = 111,
                Two = 222
            }),
            xxx = 888
        };
    }
}
";

            var expected = @"
class Num
{
    public int One {get;set;}
    public int Two {get;set;}
}
class Aaa
{
    public Num Ppp {get;set;}
    public int xxx {get;set;}
}

class CollInit
{
    void foo()
    {
        Num obj;
        Aaa iii = new Aaa()
        {
            Ppp = (obj = new Num {
                One = 111,
                Two = 222
            }),
            xxx = 888
        };iii.xxx=888; iii.Ppp=(obj=new Num{One=111,Two=222});obj.Two=222;obj.One=111;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateObjectInitializerNestedPreserveTrivia6()
        {
            var original = @"
class Num
{
    public int One {get;set;}
    public int Two {get;set;}
}
class Aaa
{
    public Num Ppp {get;set;}
    public int xxx {get;set;}
}

class CollInit
{
    void foo()
    {
        Num obj;
        Aaa iii = new Aaa() {
            Ppp = (obj = new Num {
                One = 111,
                Two = 222
            }),
            xxx = 888
        };
    }
}
";

            var expected = @"
class Num
{
    public int One {get;set;}
    public int Two {get;set;}
}
class Aaa
{
    public Num Ppp {get;set;}
    public int xxx {get;set;}
}

class CollInit
{
    void foo()
    {
        Num obj;
        Aaa iii = new Aaa() {
            Ppp = (obj = new Num {
                One = 111,
                Two = 222
            }),
            xxx = 888
        };iii.xxx=888; iii.Ppp=(obj=new Num{One=111,Two=222});obj.Two=222;obj.One=111;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateObjectInitializerDirectlyInReturnStatement()
        {
            var original = @"
class Abc
{
    int ddd;

    Abc ggg()
    {
        Abc temp;
        return temp = new Abc() { ddd = 42 };
    }
}
";

            var expected = @"
class Abc
{
    int ddd;

    Abc ggg()
    {
        Abc temp;
        temp=new Abc(){ddd=42}; temp.ddd=42; return temp;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateObjectInitializerDirectlyInReturnStatementParenthesis()
        {
            var original = @"
class Abc
{
    int ddd;

    Abc ggg()
    {
        Abc temp;
        return (temp = new Abc() { ddd = 42 });
    }
}
";

            var expected = @"
class Abc
{
    int ddd;

    Abc ggg()
    {
        Abc temp;
        temp=new Abc(){ddd=42}; temp.ddd=42; return temp;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateObjectInitializerInSwitchStatement()
        {
            var original = @"
class A
{
    public int Vvv;
    public A g(A ooo){ return ooo; }
    public void f()
    {
        A o = default(A);
        switch (g((o = new A() { Vvv = 12 })))
        {
            default:
                break;
        }
    }
}
";

            var expected = @"
class A
{
    public int Vvv;
    public A g(A ooo){ return ooo; }
    public void f()
    {
        A o = default(A);
        A ____sw_0=g((o=new A() { Vvv = 12 }));o.Vvv=12; switch (____sw_0)
        {
            default:
                break;
        }
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateObjectInitializerInSwitchStatementAsAssignment()
        {
            var original = @"
class A
{
    public int Vvv;
    public void f()
    {
        A a = null;
        switch (a = new A() { Vvv = 12 })
        {
            default:
                break;
        }
    }
}
";

            var expected = @"
class A
{
    public int Vvv;
    public void f()
    {
        A a = null;
        a = new A() { Vvv = 12 }; a.Vvv=12; switch (a)
        {
            default:
                break;
        }
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateObjectInitializerWithObjectInitializerAsConstructorArgument()
        {
            var original = @"
class Test
{
    public Test(Abc ppp) {}
    public int kkk;
}

class Abc
{
    int ddd;

    Test fff()
    {
        Test ttt;
        Abc aaa;
        return ttt = new Test((aaa = new Abc() { ddd = 42 })) { kkk = 99 };
    }
}
";

            var expected = @"
class Test
{
    public Test(Abc ppp) {}
    public int kkk;
}

class Abc
{
    int ddd;

    Test fff()
    {
        Test ttt;
        Abc aaa;
        ttt=new Test((aaa=new Abc(){ddd=42})){kkk=99}; ttt.kkk=99; aaa.ddd=42; return ttt;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDontReplicateObjectInitializerAsInvocationArgument()
        {
            var original = @"
class Program
{
    private int val;
    public Program Bar(Program p) { return p; }
    public void Foo()
    {
        Program t;
        Program obj = Bar(t = new Program() { val = 42 });
    }
}
";

            var expected = @"
class Program
{
    private int val;
    public Program Bar(Program p) { return p; }
    public void Foo()
    {
        Program t;
        Program obj=Bar(t=new Program(){val=42}); t.val=42;
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDontReplicateObjectInitializerAsInvocationArgumentNested()
        {
            var original = @"
class Other { public double pi; }
class Program
{
    public Program(Other o) {}
    private int val;
    Program Bar(Program p) { return p; }
    public void Foo()
    {
        Program p;
        Other o;
        Bar(p = new Program(o = new Other() { pi = 3.14 }) { val = 42 });
    }
}
";

            var expected = @"
class Other { public double pi; }
class Program
{
    public Program(Other o) {}
    private int val;
    Program Bar(Program p) { return p; }
    public void Foo()
    {
        Program p;
        Other o;
        Bar(p=new Program(o=new Other(){pi=3.14}){val=42}); p.val=42; o.pi=3.14;
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateObjectInitializerInObjectInitializer()
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
        Point ____obj_0=default(Point);
        Point ____obj_1=default(Point);
        Rectangle r = new Rectangle
        {
            P1=(____obj_0=new Point{X=0,Y=1}),
            P2=(____obj_1=new Point{X=2,Y=3})
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
        Point ____obj_0=default(Point);
        Point ____obj_1=default(Point);
        Rectangle r = new Rectangle
        {
            P1=(____obj_0=newPoint{X=0,Y=1}),
            P2=(____obj_1=newPoint{X=2,Y=3})
        }; r.P2=(____obj_1=new Point{X=2,Y=3}); ____obj_1.Y=3; ____obj_1.X=2; r.P1=(____obj_0=new Point{X=0,Y=1}); ____obj_0.Y=1; ____obj_0.X=0;
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateObjectInitializerInObjectInitializerWithoutNew()
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
            P1 = { X = 0, Y = 1 },
            P2 = { X = 2, Y = 3 }
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
        Rectangle r = new Rectangle
        {
            P1 = { X = 0, Y = 1 },
            P2 = { X = 2, Y = 3 }
        }; r.P2.Y = 3; r.P2.X = 2; r.P1.Y = 1; r.P1.X = 0;
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateCollectionInitializer()
        {
            var original = @"
using System.Collections.Generic;
class Program
{
    public void Foo()
    {
        var obj = new List<int>() { 42 };
    }
}
";
            var expected = @"
using System.Collections.Generic;
class Program
{
    public void Foo()
    {
        var obj = new List<int>() { 42 }; obj.Add(42);
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateCollectionInitializerDirectlyInReturnStatement()
        {
            var original = @"
using System.Collections.Generic;

class Abc
{
    List<int> hhh()
    {
        List<int> lst;
        return lst = new List<int> { 1111, 3333 };
    }
}
";

            var expected = @"
using System.Collections.Generic;

class Abc
{
    List<int> hhh()
    {
        List<int> lst;
        lst=new List<int>{1111,3333}; lst.Add(1111); lst.Add(3333); return lst;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateCollectionInitializerMultipleElements()
        {
            var original = @"
using System.Collections.Generic;
class Program
{
    public void Foo()
    {
        var obj = new List<int>() { 42, 100, 9 };
    }
}
";
            var expected = @"
using System.Collections.Generic;
class Program
{
    public void Foo()
    {
        var obj = new List<int>() { 42, 100, 9 }; obj.Add(42); obj.Add(100); obj.Add(9);
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateCollectionInitializerNonLiteralElements()
        {
            var original = @"
using System.Collections.Generic;
class Program
{
    int g() { return 1234; }
    public void Foo()
    {
        int num = 10;
        var obj = new List<int>() { 42, num, g() };
    }
}
";
            var expected = @"
using System.Collections.Generic;
class Program
{
    int g() { return 1234; }
    public void Foo()
    {
        int num = 10;
        var obj = new List<int>() { 42, num, g() }; obj.Add(42); obj.Add(num); obj.Add(g());
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateArrayInitializerInDeclarator()
        {

            var original = @"
class Program
{
    public void Foo()
    {
        int[] a = new int[] { 1, 2 };
    }
}
";
            var expected = @"
class Program
{
    public void Foo()
    {
        int[] a = new int[] { 1, 2 }; a[0]=1; a[1]=2;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }


        [TestMethod]
        public void TestReplicateArrayInitializerInAssignment()
        {
            var original = @"
class Program
{
    public void Foo()
    {
        int[] a;
        a = new int[] { 1, 2 };
    }
}
";
            var expected = @"
class Program
{
    public void Foo()
    {
        int[] a;
        a = new int[] { 1, 2 }; a[0]=1; a[1]=2;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }


        [TestMethod]
        public void TestReplicateArrayInitializerNonLiteral()
        {
            var original = @"
class Program
{
    public void Foo()
    {
        Program[] a = new Program[] { null, new Program() };
    }
}
";
            var expected = @"
class Program
{
    public void Foo()
    {
        Program[] a = new Program[] { null, new Program() }; a[0]=null; a[1]=new Program();
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }


        [TestMethod]
        public void TestReplicateArrayInitializerInferredType()
        {
            var original = @"
class Program
{
    public void Foo()
    {
        var a = new int[] { 1, 2 };
    }
}
";
            var expected = @"
class Program
{
    public void Foo()
    {
        var a = new int[] { 1, 2 }; a[0]=1; a[1]=2;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateArrayInitializerPreserveTrivia1()
        {
            var original = @"
class Program
{
    public void Foo()
    {
        var a = new int[]
        { 1, 2 };
    }
}
";
            var expected = @"
class Program
{
    public void Foo()
    {
        var a = new int[]
        { 1, 2 }; a[0]=1; a[1]=2;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateArrayInitializerPreserveTrivia2()
        {
            var original = @"
class Program
{
    public void Foo()
    {
        var a = new int[]
        {
            1, 2
        };
    }
}
";
            var expected = @"
class Program
{
    public void Foo()
    {
        var a = new int[]
        {
            1, 2
        }; a[0]=1; a[1]=2;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateArrayInitializerPreserveTrivia3()
        {
            var original = @"
class Program
{
    public void Foo()
    {
        var a = new int[]
        {
            1,
            2
        };
    }
}
";
            var expected = @"
class Program
{
    public void Foo()
    {
        var a = new int[]
        {
            1,
            2
        }; a[0]=1; a[1]=2;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateArrayInitializerPreserveTrivia4()
        {
            var original = @"
class Program
{
    public void Foo()
    {
        var a = new int[]
        {
            1
            , 2
        };
    }
}
";
            var expected = @"
class Program
{
    public void Foo()
    {
        var a = new int[]
        {
            1
            , 2
        }; a[0]=1; a[1]=2;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateArrayInitializerPreserveTrivia5()
        {
            var original = @"
class Program
{
    public void Foo()
    {
        var a = new int[] {
            1, 2
        };
    }
}
";
            var expected = @"
class Program
{
    public void Foo()
    {
        var a = new int[] {
            1, 2
        }; a[0]=1; a[1]=2;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateArrayInitializerPreserveTrivia6()
        {
            var original = @"
class Program
{
    public void Foo()
    {
        var a = new int[] {
            1,
            2
        };
    }
}
";
            var expected = @"
class Program
{
    public void Foo()
    {
        var a = new int[] {
            1,
            2
        }; a[0]=1; a[1]=2;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateArrayInitializerPreserveTrivia7()
        {
            var original = @"
class Program
{
    public void Foo()
    {
        var a = new int[]
        {
            1
            , 2 };
    }
}
";
            var expected = @"
class Program
{
    public void Foo()
    {
        var a = new int[]
        {
            1
            , 2 }; a[0]=1; a[1]=2;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateArrayInitializerBidimensional()
        {
            var original = @"
class Program
{
    public void Foo()
    {
        int[,] a = new int[4, 2] { { 1, 2 }, { 3, 4 }, { 5, 6 }, { 7, 8 } };
    }
}
";

            var expected = @"
class Program
{
    public void Foo()
    {
        int[,] a = new int[4, 2] { { 1, 2 }, { 3, 4 }, { 5, 6 }, { 7, 8 } }; a[0,0] = 1; a[0,1] = 2; a[1,0] = 3; a[1,1] = 4; a[2,0] = 5; a[2,1] = 6; a[3,0] = 7; a[3,1] = 8;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateArrayInitializerBidimensionalPreserveTrivia1()
        {
            var original = @"
class Program
{
    public void Foo()
    {
        int[,] a = new int[4, 2]
        { { 1, 2 }, { 3, 4 }, { 5, 6 }, { 7, 8 } };
    }
}
";

            var expected = @"
class Program
{
    public void Foo()
    {
        int[,] a = new int[4, 2]
        { { 1, 2 }, { 3, 4 }, { 5, 6 }, { 7, 8 } }; a[0,0] = 1; a[0,1] = 2; a[1,0] = 3; a[1,1] = 4; a[2,0] = 5; a[2,1] = 6; a[3,0] = 7; a[3,1] = 8;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateArrayInitializerBidimensionalPreserveTrivia2()
        {
            var original = @"
class Program
{
    public void Foo()
    {
        int[,] a = new int[4, 2]
        {
            { 1, 2 }, { 3, 4 }, { 5, 6 }, { 7, 8 }
        };
    }
}
";

            var expected = @"
class Program
{
    public void Foo()
    {
        int[,] a = new int[4, 2]
        {
            { 1, 2 }, { 3, 4 }, { 5, 6 }, { 7, 8 }
        }; a[0,0] = 1; a[0,1] = 2; a[1,0] = 3; a[1,1] = 4; a[2,0] = 5; a[2,1] = 6; a[3,0] = 7; a[3,1] = 8;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateArrayInitializerBidimensionalPreserveTrivia3()
        {
            var original = @"
class Program
{
    public void Foo()
    {
        int[,] a = new int[4, 2]
        {
            { 1, 2 },
            { 3, 4 },
            { 5, 6 }, { 7, 8 }
        };
    }
}
";

            var expected = @"
class Program
{
    public void Foo()
    {
        int[,] a = new int[4, 2]
        {
            { 1, 2 },
            { 3, 4 },
            { 5, 6 }, { 7, 8 }
        }; a[0,0] = 1; a[0,1] = 2; a[1,0] = 3; a[1,1] = 4; a[2,0] = 5; a[2,1] = 6; a[3,0] = 7; a[3,1] = 8;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateArrayInitializerBidimensionalPreserveTrivia4()
        {
            var original = @"
class Program
{
    public void Foo()
    {
        int[,] a = new int[4, 2] {
            { 1, 2 },
            { 3, 4 },
            { 5, 6 }, { 7, 8 }
        };
    }
}
";

            var expected = @"
class Program
{
    public void Foo()
    {
        int[,] a = new int[4, 2] {
            { 1, 2 },
            { 3, 4 },
            { 5, 6 }, { 7, 8 }
        }; a[0,0] = 1; a[0,1] = 2; a[1,0] = 3; a[1,1] = 4; a[2,0] = 5; a[2,1] = 6; a[3,0] = 7; a[3,1] = 8;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateArrayInitializerTridimensional()
        {
            var original = @"
class Program
{
    public void Foo()
    {
        int[,,] a;
        a = new int[1,1,1]{{{1}}};
    }
}
";
            var expected = @"
class Program
{
    public void Foo()
    {
        int[,,] a;
        a = new int[1,1,1]{{{1}}}; a[0,0,0] = 1;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateImplicitArrayInitializer()
        {
            var original = @"
class Abc
{
    void f()
    {
        var c = new[] { 55, 66 };
    }
}
";
            var expected = @"
class Abc
{
    void f()
    {
        var c = new[] { 55, 66 }; c[0] = 55; c[1] = 66;
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateArrayInitializerStatic()
        {
            // Note that there's no `new' call in this case.
            var original = @"
public class Abc
{
    void g()
    {
       int[] jj = { 1, 2 };
    }
}
";
            var expected = @"
public class Abc
{
    void g()
    {
       int[] jj = { 1, 2 }; jj[0] = 1; jj[1] = 2;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDontReplicateArrayInitializerEmpty()
        {
            var original = @"
class C
{
    void f()
    {
        var a = new C[] {};
        C first = a[0];
    }
}
";

            var expected = @"
class C
{
    void f()
    {
        var a = new C[] {};
        C first = a[0];
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestMultiDimensinoalArrayCreationWithInitializer3()
        {
            var original = @"
class Program
{
    public void Foo()
    {
        string[, ,] a = new string[4,2,3]
        {
            {
                { ""a"", ""b"", null },
                { ""d"", ""e"", ""f"" }
            },
            { 
                { ""g"", ""h"", ""i""},
                { ""j"", ""k"", ""l"" }
            },
            {
                { ""m"", ""n"", ""o""},
                { ""p"", ""q"", ""r"" }
            },
            {
                { ""s"", ""t"", ""u""},
                { ""v"", ""w"", ""x"" }
            }
        };
    }
}
";
            var expected = @"
class Program
{
    public void Foo()
    {
        string[, ,] a = new string[4,2,3]
        {
            {
                { ""a"", ""b"", null },
                { ""d"", ""e"", ""f"" }
            },
            { 
                { ""g"", ""h"", ""i""},
                { ""j"", ""k"", ""l"" }
            },
            {
                { ""m"", ""n"", ""o""},
                { ""p"", ""q"", ""r"" }
            },
            {
                { ""s"", ""t"", ""u""},
                { ""v"", ""w"", ""x"" }
            }
        }; a[0,0,0] = ""a""; a[0,0,1] = ""b""; a[0,0,2] = null; a[0,1,0] = ""d""; a[0,1,1] = ""e""; a[0,1,2] = ""f""; a[1,0,0] = ""g""; a[1,0,1] = ""h""; a[1,0,2] = ""i"";  a[1,1,0] = ""j"";a[1,1,1] = ""k""; a[1,1,2] = ""l""; a[2,0,0] = ""m""; a[2,0,1] = ""n""; a[2,0,2] = ""o""; a[2,1,0] = ""p""; a[2,1,1] = ""q""; a[2,1,2] = ""r""; a[3,0,0] = ""s""; a[3,0,1] = ""t""; a[3,0,2] = ""u""; a[3,1,0] = ""v""; a[3,1,1] = ""w""; a[3,1,2] = ""x"";
   }
}
"
;

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestMultiDimensinoalArrayCreationWithInitializer4()
        {
            var original = @"
class Program
{
    public Program g() {
        return null;
    }
    
    public void Foo()
    {
        Program [,] a = new Program [2, 2]
        {
            {new Program(),g()},
            {null,new Program()}
        };
    }
}
";
            var expected = @"
class Program
{
    public Program g() {
        return null;
    }
    
    public void Foo()
    {
        Program [,] a = new Program [2, 2]
        {
            {new Program(),g()},
            {null,new Program()}
        }; a[0,0] = new Program(); a[0,1] = g(); a[1,0] = null; a[1,1] = new Program();
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestJaggedArrayCreationWithInitializer1()
        {
            var original = @"
class Program
{
    public void Foo()
    {
        int[][,] a = new int[2][,] 
        {
            new int[,] { {1}, {2} },
            new int[,] { {2}, {4}, {5} }
        };
    }
}
";
            var expected = @"
class Program
{
    public void Foo()
    {
        int[][,] a = new int[2][,] 
        {
            new int[,] { {1}, {2} },
            new int[,] { {2}, {4}, {5} }
        }; a[0]=new int[,]{{1},{2}}; a[1]=new int[,]{{2},{4},{5}};
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestJaggedArrayCreationWithInitializer2()
        {
            var original = @"
class Program
{
    const int x = 2;
    public void Foo()
    {
        var a = new int[x][,] 
        {
            new int[,] { {1}, {2} },
            new int[,] { {2}, {4}, {5} }
        };
    }
}
";
            var expected = @"
class Program
{
    const int x = 2;
    public void Foo()
    {
        var a = new int[x][,] 
        {
            new int[,] { {1}, {2} },
            new int[,] { {2}, {4}, {5} }
        }; a[0]=new int[,]{{1},{2}}; a[1]=new int[,]{{2},{4},{5}};
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDictionaryCreationWithInitializer1()
        {
            var original = @"
using System.Collections.Generic;

class CollInit
{
    public static void Main()
    {
        Dictionary<int, Dictionary<int, int>> LoaderArray = 
            new Dictionary<int, Dictionary<int, int>>()
        {
            {1, new Dictionary<int, int>(){{3, 6}}},
            {2, new Dictionary<int, int>(){{4, 7}}},
            {3, new Dictionary<int, int>(){{5, 8}}}
        };
    }
}
";

            // This rewriter only applyes on "stored" `new' invocations. The nested dictionaries
            // creation expressions are expected to remain the same from this rewriter's standpoint.
            var expected = @"
using System.Collections.Generic;

class CollInit
{
    public static void Main()
    {
        Dictionary<int, Dictionary<int, int>> LoaderArray = 
            new Dictionary<int, Dictionary<int, int>>()
        {
            {1, new Dictionary<int, int>(){{3, 6}}},
            {2, new Dictionary<int, int>(){{4, 7}}},
            {3, new Dictionary<int, int>(){{5, 8}}}
        }; LoaderArray.Add(1,newDictionary<int,int>(){{3,6}}); LoaderArray.Add(2,newDictionary<int,int>(){{4,7}}); LoaderArray.Add(3,newDictionary<int,int>(){{5,8}});
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDictionaryCreationWithInitializer2()
        {
            var original = @"
using System.Collections.Generic;

class StudentName
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int ID { get; set; }
}

class CollInit
{
    void foo()
    {
        Dictionary<int, StudentName> students = new Dictionary<int, StudentName>()
        {
           { 111, new StudentName { FirstName = ""Sachin"", LastName = ""Karnik"", ID=211}}
        };
    }
}
";

            // This rewriter only applyes on "stored" `new' invocations. The nested student creation
            // expression is expected to remain the same from this rewriter's standpoint.
            var expected = @"
using System.Collections.Generic;

class StudentName
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int ID { get; set; }
}

class CollInit
{
    void foo()
    {
        Dictionary<int, StudentName> students = new Dictionary<int, StudentName>()
        {
           { 111, new StudentName { FirstName = ""Sachin"", LastName = ""Karnik"", ID=211}}
        }; students.Add(111,new StudentName{FirstName=""Sachin"",LastName=""Karnik"",ID=211});
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestAssignmentBasedDictionaryInitialization()
        {
            var original = @"
using System.Collections.Generic;

class Abc
{
    public void fff()
    {
        var ddd = new Dictionary<int, string>()
        {
            [11] = ""oneone"",
            [99] = ""nine""
        };
    }
}
";

            var expected = @"
using System.Collections.Generic;

class Abc
{
    public void fff()
    {
        var ddd = new Dictionary<int, string>()
        {
            [11] = ""oneone"",
            [99] = ""nine""
        };
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateInitializationBeforeReturn()
        {
            var original = @"
using System;
using System.Collections.Generic;

public class Abc
{
    public static List<Abc> fff()
    {
        Abc aaa = new Abc();
        var tmp = new List<Abc> { aaa };
        return tmp;
    }
}
";

            var expected = @"
using System;
using System.Collections.Generic;

public class Abc
{
    public static List<Abc> fff()
    {
        Abc aaa = new Abc();
        var tmp = new List<Abc> { aaa }; tmp.Add(aaa);
        return tmp;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateInitializationDifferentAssignedTypeInDeclaration()
        {
            var original = @"
interface IFoo {}
public class Foo : IFoo
{
    public int TheBar;
}
public class Test
{
    public void fff()
    {
        IFoo obj = new Foo
        {
            TheBar = 123
        };
    }
}
";

            var expected = @"
interface IFoo {}
public class Foo : IFoo
{
    public int TheBar;
}
public class Test
{
    public void fff()
    {
        IFoo obj = new Foo
        {
            TheBar = 123
        }; var ____init_0 = new Foo { TheBar = 123 }; ____init_0.TheBar =123; obj =____init_0;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateInitializationDifferentAssignedTypeInAssignment()
        {
            var original = @"
interface IFoo {}
public class Foo : IFoo
{
    public int TheBar;
}
public class Test
{
    public void fff()
    {
        IFoo obj = null;
        obj = new Foo
        {
            TheBar = 999
        };
    }
}
";

            var expected = @"
interface IFoo {}
public class Foo : IFoo
{
    public int TheBar;
}
public class Test
{
    public void fff()
    {
        IFoo obj = null;
        obj = new Foo
        {
            TheBar = 999
        }; var ____init_0 = new Foo { TheBar = 999 }; ____init_0.TheBar =999; obj =____init_0;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateInitializationNameConflict()
        {
            // Here, we have 2 rewriters that produce new objects.

            var original = @"
public interface IFoo {}
public class Foo : IFoo
{
    public int TheBar;
}

public class BBB { public int _bbb; }
public class CCC
{
    public IFoo jjj;
    public void ggg(BBB p) {}
}

public class Test
{
    public void fff()
    {
        var ii = new CCC();
        ii.jjj = new Foo { TheBar = 11 };
        ii.ggg(new BBB { _bbb = 999 });
    }
}
";

            var expected = @"
public interface IFoo {}
public class Foo : IFoo
{
    public int TheBar;
}

public class BBB { public int _bbb; }
public class CCC
{
    public IFoo jjj;
    public void ggg(BBB p) {}
}

public class Test
{
    public void fff()
    {
        var ii = new CCC();
        ii.jjj = new Foo { TheBar = 11 };var ____init_0=new Foo { TheBar = 11 };____init_0.TheBar =11 ;ii.jjj =____init_0;
        ii.ggg(new BBB { _bbb = 999 });
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateInitializationDifferentAssignedTypeNested()
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
        Foo ____obj_0=default(Foo);
        IBar ibar = new Bar
        {
            TheFoo=(____obj_0=new Foo{what=123})
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
        Foo ____obj_0=default(Foo);
        IBar ibar = new Bar
        {
            TheFoo=(____obj_0=new Foo{what=123})
        };var ____init_0=new Bar{TheFoo=(____obj_0=new Foo{what=123})};____init_0.TheFoo=(____obj_0=new Foo{what=123});____obj_0.what=123;ibar=____init_0;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateInitializationNoBlockSwitchSection()
        {
            var original = @"
using System;

public class Abc
{
    public void fff()
    {
        switch (111)
        {
            case 222:
                var rrr = new int[] { 99, 88 };
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
        switch (111)
        {
            case 222:
                { var rrr = new int[] { 99, 88 };rrr[0]=99;rrr[1]=88; }
        }
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateInitializationEnsureUniqueReturnObjects()
        {
            var original = @"
public class Test
{
    public int iii;

    public int ggg(Test ppp) { return 0; }

    public int fff()
    {
        if (true)
        {
            Test ____obj_0=default(Test);
            return ggg((____obj_0 = new Test { iii = 11 }));
        }
        Test ____obj_1=default(Test);
        return ggg((____obj_1=new Test { iii = 22 }));
    }
}
";

            var expected = @"
public class Test
{
    public int iii;

    public int ggg(Test ppp) { return 0; }

    public int fff()
    {
        if (true)
        {
            Test ____obj_0=default(Test);
            int ____ret_0=ggg((____obj_0=newTest{iii=11}));
            ____obj_0.iii=11;
            return ____ret_0;
        }
        Test ____obj_1=default(Test);
        int ____ret_1=ggg((____obj_1=newTest{iii=22}));____obj_1.iii=22;return____ret_1;
    }
}";

            TestRewrite_LineIgnore(original, expected);
        }

        [TestMethod]
        public void TestReplicateObjectInitializerInObjectInitializerAsArgument()
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
        Xpt ____obj_0=default(Xpt);
        var ooo=new Abc
        {
            iii=ggg((____obj_0=new Xpt{jjj=99}))
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
        Xpt ____obj_0=default(Xpt);
        var ooo=new Abc
        {
            iii=ggg((____obj_0=new Xpt{jjj=99}))
        };ooo.iii=ggg((____obj_0=newXpt{jjj=99}));____obj_0.jjj=99;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateObjectInitializerInDictionaryElementAccess()
        {
            var original = @"
public struct Point
{
    int x;
    public int X { get { return x; } set { x = value; } }
}

public struct Wrap
{
    public int Vvv;
    Point _p;
    public Point Ppp { get { return _p; } set { _p = value; }}
}

public class Abc
{
    public void fff()
    {
        System.Collections.Generic.Dictionary<int, Wrap> ddd = null;

        Point ____obj_0=default(Point );
        Wrap ____obj_1=default(Wrap);

        ddd[0] = (____obj_1 = new Wrap() { Ppp = (____obj_0 = new Point { X = 11 }) });
    }
}
";

            var expected = @"
public struct Point
{
    int x;
    public int X { get { return x; } set { x = value; } }
}

public struct Wrap
{
    public int Vvv;
    Point _p;
    public Point Ppp { get { return _p; } set { _p = value; }}
}

public class Abc
{
    public void fff()
    {
        System.Collections.Generic.Dictionary<int, Wrap> ddd = null;

        Point ____obj_0=default(Point );
        Wrap ____obj_1=default(Wrap);

        ddd[0] = (____obj_1 = new Wrap() { Ppp = (____obj_0 = new Point { X = 11 }) });____obj_1.Ppp=(____obj_0=new Point{X=11});____obj_0.X=11;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateCollectionInitializerInObjectInitializerAsArgument()
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
        char[] ____obj_0=default(char[]);
        var retVal=new Abc
        {
            lll=mmm.Split((____obj_0=new[]{','}))
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
        char[] ____obj_0=default(char[]);
        var retVal=new Abc
        {
            lll=mmm.Split((____obj_0=new[]{','}))
        };retVal.lll=mmm.Split((____obj_0=new[]{','}));____obj_0[0]=',';
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateCollectionInitializerInUsingStatement()
        {
            var original = @"
using System;

class Program
{
    static void f()
    {
        using(SqlHelper sqlHelper = new SqlHelper() { Val = 123 })
        {
            f();
        }
    }
}
 
public class SqlHelper : IDisposable
{
    public int Val;
    public void Dispose() {}
}
";

            var expected = @"
using System;

class Program
{
    static void f()
    {
        using(SqlHelper sqlHelper = new SqlHelper() { Val = 123 })
        {
            sqlHelper.Val=123;  f();
        }
    }
}
 
public class SqlHelper : IDisposable
{
    public int Val;
    public void Dispose() {}
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateCollectionInitializerInUsingStatement2()
        {
            var original = @"
using System;

class Program
{
    static void f()
    {
        using(SqlHelper sqlHelper = new SqlHelper() { Val = 123 })
        {
        }
    }
}
 
public class SqlHelper : IDisposable
{
    public int Val;
    public void Dispose() {}
}
";

            var expected = @"
using System;

class Program
{
    static void f()
    {
        using(SqlHelper sqlHelper = new SqlHelper() { Val = 123 })
        {
         sqlHelper.Val=123; }
    }
}
 
public class SqlHelper : IDisposable
{
    public int Val;
    public void Dispose() {}
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateCollectionInitializerInUsingStatement3()
        {
            var original = @"
using System;

class Program
{
    static void f()
    {
        using(SqlHelper sqlHelper = new SqlHelper() { Val = 123 })
        {}
    }
}
 
public class SqlHelper : IDisposable
{
    public int Val;
    public void Dispose() {}
}
";

            var expected = @"
using System;

class Program
{
    static void f()
    {
        using(SqlHelper sqlHelper = new SqlHelper() { Val = 123 })
        {sqlHelper.Val=123; }
    }
}
 
public class SqlHelper : IDisposable
{
    public int Val;
    public void Dispose() {}
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateCollectionInitializerInUsingStatement4()
        {
            var original = @"
using System;

class Program
{
    static void f()
    {
        using(SqlHelper sqlHelper = new SqlHelper() { Val = 123 })
            ;
    }
}
 
public class SqlHelper : IDisposable
{
    public int Val;
    public void Dispose() {}
}
";

            var expected = @"
using System;

class Program
{
    static void f()
    {
        using(SqlHelper sqlHelper = new SqlHelper() { Val = 123 })
        {sqlHelper.Val=123;  ; }
    }
}
 
public class SqlHelper : IDisposable
{
    public int Val;
    public void Dispose() {}
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestReplicateObjectInitializerCommentTrivia()
        {
            var original = @"
class Ddd { public int One; public int Two; }

class Aaa
{
    Ddd foo()
    {
        Ddd obj; // a comment right here
        return obj = new Ddd
        {
            One = 111,
            Two = 222
        };
    }
}
";

            var expected = @"
class Ddd { public int One; public int Two; }

class Aaa
{
    Ddd foo()
    {
        Ddd obj; // a comment right here
        obj = new Ddd
        {
            One = 111,
            Two = 222
        }; obj.Two=222;obj.One=111;return obj;
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }
    }
}
