using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using SharpSyntaxRewriter.Rewriters;

namespace Tests
{
    [TestClass]
    public class TestImposeThisPrefix : RewriterTester
    {
        protected override SyntaxTree ApplyRewrite(SyntaxTree tree, Compilation compilation)
        {
            ImposeThisPrefix rw = new();
            return rw.Apply(tree, compilation.GetSemanticModel(tree));
        }

        [TestMethod]
        public void TestImposeThisPrefixBasic()
        {
            var original = @"
public class trick { public static int me; }
public class Base1 { public trick k; }
public class Value { public int _value; }
public class Base2 : Base1 { public int _cnt; public Value amount; public static int Constant; }

public class ClassType : Base2
{
    public struct Point { public int x; public int y; }
    private Point p;
    public static int Constant2;
    public void Foo()
    {
        int abc;
        abc = 10;
        p.x = 3;
        this.p.y = 5;
        _cnt = 10;
        this._cnt = 11;
        Constant = 10;
        Constant2 = 9;
        trick.me = 6;
        amount._value = trick.me + _cnt;
        this.amount._value++;
    }
}
";
            var expected = @"
public class trick { public static int me; }
public class Base1 { public trick k; }
public class Value { public int _value; }
public class Base2 : Base1 { public int _cnt; public Value amount; public static int Constant; }

public class ClassType : Base2
{
    public struct Point { public int x; public int y; }
    private Point p;
    public static int Constant2;
    public void Foo()
    {
        int abc;
        abc = 10;
        this.p.x = 3;
        this.p.y = 5;
        this._cnt = 10;
        this._cnt = 11;
        Constant = 10;
        Constant2 = 9;
        trick.me = 6;
        this.amount._value = trick.me + this._cnt;
        this.amount._value++;
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImposeThisPrefixMemberHide()
        {
            var original = @"
public class ClassType
{
    public struct Point { public int x; public int y; }
    private Point p;
    public void Foo()
    {
        Point p = new Point();
        p.x = 3;
        this.p.y = 5;
    }
}
";
            var expected = @"
public class ClassType
{
    public struct Point { public int x; public int y; }
    private Point p;
    public void Foo()
    {
        Point p = new Point();
        p.x = 3;
        this.p.y = 5;
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImposeThisPrefixMethodMember()
        {
            var original = @"
namespace ns {
public class ClassType
{
    public void Bar() {}
    public void Foo()
    {
        Bar();
    }
}
}
";
            var expected = @"
namespace ns {
public class ClassType
{
    public void Bar() {}
    public void Foo()
    {
        this.Bar();
    }
}
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImposeThisPrefixGenericCall()
        {
            var original = @"
class Abc
{
    public void f()
    {
        g<Abc>();
    }
    public void g<T>() where T : new()
    {
    }
}
";
            var expected = @"
class Abc
{
    public void f()
    {
        this.g<Abc>();
    }
    public void g<T>() where T : new()
    {
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImposeThisPrefixForTupleField()
        {
            var original = @"
using System;

public class Abc
{
    void g()
    {
        (string Alpha, string Beta) namedLetters = (""a"", ""b"");
        var alphabetStart = (Alpha: ""a"", Beta: ""b"");
    }
}
";

            var expected = @"
using System;

public class Abc
{
    void g()
    {
        (string Alpha, string Beta) namedLetters = (""a"", ""b"");
        var alphabetStart = (Alpha: ""a"", Beta: ""b"");
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImposeThisPrefixNoThisKeepObjInitTrivia1()
        {
            var original = @"
class StudentName
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

class CollInit
{
    void foo()
    {
        var student = new StudentName()
        {
            FirstName = ""aaa"",
            LastName = ""bbb""
        };
    }
}
";

            var expected = @"
class StudentName
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

class CollInit
{
    void foo()
    {
        var student = new StudentName()
        {
            FirstName = ""aaa"",
            LastName = ""bbb""
        };
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImposeThisPrefixNoThisKeepObjInitTrivia2()
        {
            var original = @"
class StudentName
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

class CollInit
{
    static string bar() { return """"; }
    void foo()
    {
        var student = new StudentName()
        {
            FirstName = CollInit.bar(),
            LastName = ""bbb""
        };
    }
}
";

            var expected = @"
class StudentName
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

class CollInit
{
    static string bar() { return """"; }
    void foo()
    {
        var student = new StudentName()
        {
            FirstName = CollInit.bar(),
            LastName = ""bbb""
        };
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImposeThisPrefixNoThisKeepObjInitTrivia3()
        {
            var original = @"
class StudentName
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

class CollInit
{
    static string bar() { return """"; }
    void foo()
    {
        var student = new StudentName()
        {
            FirstName = CollInit
                .bar(),
            LastName = ""bbb""
        };
    }
}
";

            var expected = @"
class StudentName
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

class CollInit
{
    static string bar() { return """"; }
    void foo()
    {
        var student = new StudentName()
        {
            FirstName = CollInit
                .bar(),
            LastName = ""bbb""
        };
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImposeThisPrefixNoThisKeepObjInitTrivia4()
        {
            var original = @"
class StudentName
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

class CollInit
{
    static string bar() { return """"; }
    void foo()
    {
        var student = new StudentName()
        {
            FirstName = CollInit
                .bar()
            ,

            LastName = ""bbb""
        };
    }
}
";

            var expected = @"
class StudentName
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

class CollInit
{
    static string bar() { return """"; }
    void foo()
    {
        var student = new StudentName()
        {
            FirstName = CollInit
                .bar()
            ,

            LastName = ""bbb""
        };
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImposeThisPrefixNoThisKeepObjInitTrivia5()
        {
            var original = @"
class StudentName
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

class CollInit
{
    static string bar() { return """"; }
    void foo()
    {
        var student = new StudentName()
        {
            FirstName = CollInit.bar(), LastName = ""bbb""
        };
    }
}
";

            var expected = @"
class StudentName
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

class CollInit
{
    static string bar() { return """"; }
    void foo()
    {
        var student = new StudentName()
        {
            FirstName = CollInit.bar(), LastName = ""bbb""
        };
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImposeThisPrefixNoThisKeepObjInitTrivia6()
        {
            var original = @"
class CollInit
{
    string FirstName = ""aaa"";
    string LastName = ""bbb"";

    void foo()
    {
        var student = new
        {
            this.FirstName,
            this.LastName
        };
    }
}
"
;

            var expected = @"
class CollInit
{
    string FirstName = ""aaa"";
    string LastName = ""bbb"";

    void foo()
    {
        var student = new
        {
            this.FirstName,
            this.LastName
        };
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImposeThisPrefixNoThisKeepObjInitTrivia7()
        {
            var original = @"
class StudentName
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

class CollInit
{
    string aaa = ""aaa"";
    string bbb = ""bbb"";
    void foo()
    {
        var student = new StudentName()
        {
            FirstName = this.aaa,
            LastName = this.bbb
        };
    }
}
"
;

            var expected = @"
class StudentName
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

class CollInit
{
    string aaa = ""aaa"";
    string bbb = ""bbb"";
    void foo()
    {
        var student = new StudentName()
        {
            FirstName = this.aaa,
            LastName = this.bbb
        };
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImposeThisPrefixNoThisKeepObjInitTrivia8()
        {
            var original = @"
class StudentName
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

class CollInit
{
    string aaa = ""aaa"";
    string bbb = ""bbb"";
    void foo()
    {
        var student = new StudentName()
        {
            FirstName = this
                .aaa,
            LastName = this
                .bbb
        };
    }
}
"
;

            var expected = @"
class StudentName
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

class CollInit
{
    string aaa = ""aaa"";
    string bbb = ""bbb"";
    void foo()
    {
        var student = new StudentName()
        {
            FirstName = this
                .aaa,
            LastName = this
                .bbb
        };
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImposeThisPrefixNoThisKeepObjInitTrivia9()
        {
            var original = @"
class StudentName
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

class CollInit
{
    string aaa = ""aaa"";
    string bbb = ""bbb"";
    void foo()
    {
        var student = new StudentName()
        {
            FirstName = this.aaa,
            LastName = this.bbb,
        };
    }
}
"
;

            var expected = @"
class StudentName
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

class CollInit
{
    string aaa = ""aaa"";
    string bbb = ""bbb"";
    void foo()
    {
        var student = new StudentName()
        {
            FirstName = this.aaa,
            LastName = this.bbb
        };
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImposeThisPrefixKeepObjInitTrivia1()
        {
            var original = @"
class CollInit
{
    string FirstName = ""aaa"";
    string LastName = ""bbb"";

    void foo()
    {
        var student = new
        {
            FirstName,
            LastName
        };
    }
}
"
;

            var expected = @"
class CollInit
{
    string FirstName = ""aaa"";
    string LastName = ""bbb"";

    void foo()
    {
        var student = new
        {
            this.FirstName,
            this.LastName
        };
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImposeThisPrefixKeepObjInitTrivia2()
        {
            var original = @"
class CollInit
{
    string FirstName = ""aaa"";
    string LastName = ""bbb"";

    void foo()
    {
        var student = new
        {
            FirstName

            ,

            LastName
        };
    }
}
"
;

            var expected = @"
class CollInit
{
    string FirstName = ""aaa"";
    string LastName = ""bbb"";

    void foo()
    {
        var student = new
        {
            this.FirstName

            ,

            this.LastName
        };
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImposeThisPrefixKeepObjInitTrivia3()
        {
            var original = @"
class StudentName
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

class CollInit
{
    string aaa = ""aaa"";
    string bbb = ""bbb"";
    void foo()
    {
        var student = new StudentName()
        {
            FirstName = aaa,
            LastName = bbb
        };
    }
}
"
;

            var expected = @"
class StudentName
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

class CollInit
{
    string aaa = ""aaa"";
    string bbb = ""bbb"";
    void foo()
    {
        var student = new StudentName()
        {
            FirstName = this.aaa,
            LastName = this.bbb
        };
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImposeThisPrefixKeepObjInitTrivia4()
        {
            var original = @"
class StudentName
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

class CollInit
{
    string aaa = ""aaa"";
    string bbb = ""bbb"";
    void foo()
    {
        var student = new StudentName()
        {
            FirstName = aaa, LastName = bbb
        };
    }
}
"
;

            var expected = @"
class StudentName
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

class CollInit
{
    string aaa = ""aaa"";
    string bbb = ""bbb"";
    void foo()
    {
        var student = new StudentName()
        {
            FirstName = this.aaa, LastName = this.bbb
        };
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImposeThisPrefixKeepObjInitTrivia5()
        {
            // Trailing comma after ltree item.

            var original = @"
class StudentName
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

class CollInit
{
    string aaa = ""aaa"";
    string bbb = ""bbb"";
    void foo()
    {
        var student = new StudentName()
        {
            FirstName = aaa,
            LastName = bbb,
        };
    }
}
"
;

            var expected = @"
class StudentName
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

class CollInit
{
    string aaa = ""aaa"";
    string bbb = ""bbb"";
    void foo()
    {
        var student = new StudentName()
        {
            FirstName = this.aaa,
            LastName = this.bbb
        };
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImposeThisPrefixNameColonDesignatingPropertyName()
        {
            var original = @"
public class AAA
{
    public int Foo {get;set;}
}

class CCC
{
    void fff(AAA ooo)
    {
        var xxx = ooo switch
        {
            { Foo: 111 } => 9.9
        };
    }
}
";

            var expected = @"
public class AAA
{
    public int Foo {get;set;}
}

class CCC
{
    void fff(AAA ooo)
    {
        var xxx = ooo switch
        {
            { Foo: 111 } => 9.9
        };
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImposeThisPrefixNameInAssignmentOfWithMatch()
        {
            var original = @"
public record Person(string FirstName);
class C
{
    void f()
    {
        Person ppp = new(""aaa"");
        Person qqq = ppp with { FirstName = ""bbb"" };
    }
}
";

            var expected = @"
public record Person(string FirstName);
class C
{
    void f()
    {
        Person ppp = new(""aaa"");
        Person qqq = ppp with { FirstName = ""bbb"" };
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }
    }
}
