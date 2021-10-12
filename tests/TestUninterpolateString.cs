using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using SharpSyntaxRewriter.Rewriters;

namespace Tests
{
    [TestClass]
    public class TestUninterpolateString : RewriterTester
    {
        protected override SyntaxTree ApplyRewrite(SyntaxTree tree, Compilation compilation)
        {
            UninterpolateString rw = new();
            return rw.Apply(tree, compilation.GetSemanticModel(tree));
        }

        [TestMethod]
        public void TestUninterpolateStringViaFormatCall()
        {
            var original = @"
using System;
class InterpolatedString
{
    int Foo() { return 1; }
    string Bar = ""bar"";
    void f()
    {
        var name = ""john"";
        string s1 = $""the {name} num {Foo()} and value {Bar} date {DateTime.Now.Year} end"";
    }
}
";

            var expected = @"
using System;
class InterpolatedString
{
    int Foo() { return 1; }
    string Bar = ""bar"";
    void f()
    {
        var name = ""john"";
        string s1 = string.Format(""the { 0} num { 1} and value { 2} date { 3} end"", name, Foo(), Bar,DateTime.Now.Year);
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestUninterpolateStringViaFormatCallLineBreakTrivia()
        {
            var original = @"
class InterpolatedString
{
    void f()
    {
        var name = ""john"";
        string s1 = $""the {name} "" +
            ""end"";
        }
    }
";

            var expected = @"
class InterpolatedString
{
    void f()
    {
        var name = ""john"";
        string s1 = string.Format(""the {0} "", name) +
            ""end"";
        }
    }
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestUninterpolateStringInferredTypeViaFormatCall()
        {
            var original = @"
using System;
class InterpolatedString
{
    int Foo() { return 1; }
    string Bar = ""bar"";
    void f()
    {
        var name = ""john"";
        var s1 = $""the {name} num {Foo()} and value {Bar} date {DateTime.Now.Year} end"";
    }
}
";

            var expected = @"
using System;
class InterpolatedString
{
    int Foo() { return 1; }
    string Bar = ""bar"";
    void f()
    {
        var name = ""john"";
        var s1 = string.Format(""the { 0} num { 1} and value { 2} date { 3} end"", name, Foo(), Bar,DateTime.Now.Year);
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestUninterpolateStringViaFormattableType()
        {
            var original = @"
using System;
class InterpolatedString
{
    int Foo() { return 1; }
    string Bar = ""bar"";
    void f()
    {
        var name = ""john"";
        FormattableString s1 = $""the {name} num {Foo()} and value {Bar} date {DateTime.Now.Year} end"";
    }
}
";

            var expected = @"
using System;
class InterpolatedString
{
    int Foo() { return 1; }
    string Bar = ""bar"";
    void f()
    {
        var name = ""john"";
        FormattableString s1 = System.Runtime.CompilerServices.FormattableStringFactory.Create(""the { 0} num { 1} and value { 2} date { 3} end"",name, Foo(), Bar,DateTime.Now.Year);
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestUninterpolateStringMultiple()
        {
            var original = @"
using System;
class InterpolatedString
{
    int Foo() { return 1; }
    string Bar = ""bar"";
    void f()
    {
        var name = ""john"";
        string s1 = $""the {name} num {Foo()} and value {Bar} date {DateTime.Now.Year:dd-MM-yyyy} end"";
    }
}
";

            var expected = @"
using System;
class InterpolatedString
{
    int Foo() { return 1; }
    string Bar = ""bar"";
    void f()
    {
        var name = ""john"";
        string s1 = string.Format(""the { 0} num { 1} and value { 2} date { 3:dd-MM-yyyy} end"",name, Foo(), Bar,DateTime.Now.Year);
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestUninterpolateStringWithAlign()
        {
            var original = @"
using System;
class InterpolatedString
{
    int Foo() { return 1; }
    string Bar = ""bar"";
    void f()
    {
        var name = ""john"";
        string s1 = $""the {name, 50} num {Foo()} and value {Bar} date {DateTime.Now.Year} end"";
    }
}
";

            var expected = @"
using System;
class InterpolatedString
{
    int Foo() { return 1; }
    string Bar = ""bar"";
    void f()
    {
        var name = ""john"";
        string s1 = string.Format(""the { 0,50} num { 1} and value { 2} date { 3} end"",name, Foo(), Bar,DateTime.Now.Year);
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestUninterpolateStringNested()
        {
            var original = @"
public class Test
{
    public string ggg(string ppp) { return ppp; }

    public string fff()
    {
        var jjj = 123;
        return $""whatever is { ggg($""nested {jjj}"") }"";
    }
}
";

            var expected = @"
public class Test
{
    public string ggg(string ppp) { return ppp; }

    public string fff()
    {
        var jjj = 123;
        return string.Format(""whatever is {0}"",ggg(string.Format(""nested {0}"",jjj)));
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestUninterpolateStringWithUserDefinedConverstion()
        {
            var original = @"
class AAA
{
    public static implicit operator AAA(string sss) => null;
}

class CCC
{
    void fff()
    {
        AAA vvv = $""{111}"";
    }
}
";

            var expected = @"
class AAA
{
    public static implicit operator AAA(string sss) => null;
}

class CCC
{
    void fff()
    {
        AAA vvv = string.Format(""{ 0}"",111);
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestUninterpolateStringFirstOperandTernaryCondition()
        {
            var original = @"
using System;

public class CCC
{
    private string EncodeParameter(int parameter)
    {
        return parameter == 4
                ? $""param is { parameter }""
                : ""a"";
    }
}
";

            var expected = @"
using System;

public class CCC
{
    private string EncodeParameter(int parameter)
    {
        return parameter == 4
                ? string.Format(""param is { 0 }"",parameter)
                : ""a"";
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }
    }
}
