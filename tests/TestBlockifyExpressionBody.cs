using System;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using SharpSyntaxRewriter.Rewriters;

namespace Tests
{
    [TestClass]
    public class TestBlockifyExpressionBody : RewriterTester
    {
        protected override SyntaxTree ApplyRewrite(SyntaxTree tree, Compilation compilation)
        {
            BlockifyExpressionBody rw = new();
            return rw.Apply(tree, compilation.GetSemanticModel(tree));
        }

        [TestMethod]
        public void TestBlockifyExpressionBodyProperty()
        {
            var original = @"
class Test
{
    int PropertyWithExpressionBody => 10;
}
";

            var expected = @"
class Test
{
    int PropertyWithExpressionBody { get { return 10; } }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestBlockifyExpressionBodyPropertyLineBreak()
        {
            var original = @"
class Test
{
    int PropertyWithExpressionBody =>
        10;
}
";

            var expected = @"
class Test
{
    int PropertyWithExpressionBody
        { get { return 10; } }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestBlockifyExpressionBodyMethodKeepTrivia1()
        {
            var original = @"
class Aaa
{
    static int foo() =>
        Aaa.foo();
}
";

            var expected = @"
class Aaa
{
    static int foo()
        { return Aaa.foo(); }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestBlockifyExpressionBodyMethodKeepTrivia2()
        {
            var original = @"
class Aaa
{
    static int foo() =>
        Aaa
            .foo();
}
";

            var expected = @"
class Aaa
{
    static int foo()
        { return Aaa
            .foo(); }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestBlockifyExpressionBodyMethodKeepTrivia3()
        {
            var original = @"
class Aaa
{
    static int foo()
    => Aaa.foo();
}
";

            var expected = @"
class Aaa
{
    static int foo()
        { return Aaa.foo(); }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestBlockifyExpressionBodyMethodKeepTrivia4()
        {
            var original = @"
class Aaa
{
    static int foo()
    => Aaa.
        foo();
}
";

            var expected = @"
class Aaa
{
    static int foo()
        { return Aaa.
            foo(); }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestBlockifyExpressionBodyMethodNonVoidReturn()
        {
            var original = @"
class Foo
{
    public string Joe() { return ""Joe""; }
}
";

            var expected = @"
class Foo
{
    public string Joe() { return ""Joe""; }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestBlockifyExpressionBodyMethodParameterVoidReturn()
        {
            var original = @"
class Foo
{
    public string Joe() { return ""Joe""; }
    public string Say(string ss) => Joe();
    public void Bar() => Say(""xyz"");
}
";

            var expected = @"
class Foo
{
    public string Joe() { return ""Joe""; }
    public string Say(string ss) { return Joe(); }
    public void Bar() { Say(""xyz""); }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestBlockifyExpressionBodyIndexer()
        {
            var original = @"
using System;
class BitArray
{
    int[] bits = new int[]{};
    int length = 0;

    public bool this[int index] =>
            (index < 0 || index >= length)
                ? throw new IndexOutOfRangeException()
                : (bits[index >> 5] & 1 << index) != 0;
}
";

            var expected = @"
using System;
class BitArray
{
    int[] bits = new int[]{};
    int length = 0;

    public bool this[int index]
    { get { return (index < 0 || index >= length)
                        ? throw new IndexOutOfRangeException()
                        : (bits[index >> 5] & 1 << index) != 0; } }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestBlockifyExpressionBodyOperator()
        {
            var original = @"
public class Test
{
    public static Test operator++(Test t) => new Test();
}
";

            var expected = @"
public class Test
{
    public static Test operator++(Test t) { return new Test(); }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestBlockifyExpressionBodyConversionOperator()
        {
            var original = @"
struct Convertible<T>
{
    public static explicit operator T(Convertible<T> value) => default(T);
}
";

            var expected = @"
struct Convertible<T>
{
    public static explicit operator T(Convertible<T> value) { return default(T); }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestBlockifyExpressionBodyAccessor()
        {
            // https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-7#more-expression-bodied-members
            var original = @"
public class Test
{
    private string aaa;
    private string bbb
    {
        set => this.aaa = value ?? ""default"";
    }
}
";

            var expected = @"
public class Test
{
    private string aaa;
    private string bbb
    {
        set{this.aaa=value??""default"";}
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestBlockifyExpressionBodyThrowExpression()
        {
            var original = @"
using System;
public class Test
{
    public Test What() => throw new NotImplementedException();
}
";

            var expected = @"
using System;
public class Test
{
    public Test What() { throw new NotImplementedException(); }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestBlockifyExpressionBodyThrowExpressionInAccessor()
        {
            var original = @"
using System;
public class Abc
{
    public int vvv
    {
        get => throw new Exception();
        set => throw new Exception();
    }
}
";

            var expected = @"
using System;
public class Abc
{
    public int vvv
    {
        get { throw new Exception(); }
        set { throw new Exception(); }
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestBlockifyExpressionBodyThrowExpressionInProperty()
        {
            var original = @"
using System;
public class Abc
{
    public int vvv => throw new Exception();
}
";

            var expected = @"
using System;
public class Abc
{
    public int vvv { get { throw new Exception(); } }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestBlockifyExpressionBodyLocalFunctionWithinConstructor()
        {
            var original = @"
using System;
public class Abc
{
    public Abc()
    {
        object ParseQuery(string query) => query.Split('&');
    }
}
";

            var expected = @"
using System;
public class Abc
{
    public Abc()
    {
        object ParseQuery(string query){ return query.Split('&');}
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestBlockifyExpressionBodyThrowExpressionVoidReturn()
        {
            var original = @"
using System;

public class Abc
{
    public void fff() => throw new NotSupportedException();
}
";

            var expected = @"
using System;

public class Abc
{
    public void fff() { throw new NotSupportedException(); }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestBlockifyExpressionBodyLambdaWithAnonymousType()
        {
            var original = @"
using System.Linq;

class Test
{
    void f()
    {
        var obj = new
            {
                qwe = new []
                {
                    new { nnn = 333 }
                }
                .Select(aaa => new { hhh = aaa} )
            };
    }
}
";

            var expected = @"
using System.Linq;

class Test
{
    void f()
    {
        var obj = new
            {
                qwe = new []
                {
                    new { nnn = 333 }
                }
                .Select(aaa => { return new { hhh = aaa}; } )
            };
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestBlockifyExpressionBodyGenericNonVoidTask()
        {
            var original = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

class Data {}

class CCC
{
    private async Task<Data> Get() { return null; }

    async Task<Data> f()
    {
        List<int> seriesWithPerson = null;
        var infos = (await Task.WhenAll(seriesWithPerson.Select(
                                            async i =>
                                            await Get()))
                    ).Where(i => i != null);

        return null;
    }
}
";

            var expected = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

class Data {}

class CCC
{
    private async Task<Data> Get() { return null; }

    async Task<Data> f()
    {
        List<int> seriesWithPerson = null;
        var infos = (await Task.WhenAll(seriesWithPerson.Select(
                                            async i =>
                                            { return await Get(); }))
                    ).Where(i => { return i != null; });

        return null;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }
    }
}
