using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using SharpSyntaxRewriter.Rewriters;

namespace Tests
{
    [TestClass]
    public class TestUncoalesceCoalescedNull : RewriterTester
    {
        protected override SyntaxTree ApplyRewrite(SyntaxTree tree, Compilation compilation)
        {
            UncoalesceCoalescedNull rw = new();
            return rw.Apply(tree, compilation.GetSemanticModel(tree));
        }

        [TestMethod]
        public void TestUncoalesceCoalescedNullImplicitConversionInRHS()
        {
            var original = @"
class Test
{
    void f()
    {
        short? sss = null;

        short iii = sss ?? 0;
    }
}
";

            var expected = @"
class Test
{
    void f()
    {
        short? sss = null;

        short iii = sss!=null ? sss.Value :(short)0;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestUncoalesceCoalescedNullImplicitConversionInRHSWithNamespaceAmbiguity()
        {
            var original = @"
using System;

namespace Aaa
{
    namespace System
    {
        class Version {}
    }
}

namespace Aaa
{
    public class Ccc
    {
        public void fff()
        {
            Version? ooo = null;
            var mmm = ooo ?? null;
        }
    }
}
";

            var expected = @"
using System;

namespace Aaa
{
    namespace System
    {
        class Version {}
    }
}

namespace Aaa
{
    public class Ccc
    {
        public void fff()
        {
            Version? ooo = null;
            var mmm = ooo != (Version)null ? ooo : null;
        }
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestUncoalesceCoalescedNullImplicitConversionInRHSWithOperatorAmbiguity()
        {
            var original = @"
using System;

public struct StringValues
{
    public static implicit operator StringValues(string value) { return null; }

    public static bool operator ==(StringValues left, string right) => false;
    public static bool operator !=(StringValues left, string right) => false;

    public static bool operator ==(StringValues left, object right) => false;
    public static bool operator !=(StringValues left, object right) => false;

    public static bool operator ==(string left, StringValues right) => false;
    public static bool operator !=(string left, StringValues right) => false;

    public static bool operator ==(object left, StringValues right) => false;
    public static bool operator !=(object left, StringValues right) => false;

    public static bool operator ==(StringValues left, StringValues right) => false;
    public static bool operator !=(StringValues left, StringValues right) => false;

}

public class Ccc
{
    public void fff()
    {
        StringValues? ooo = null;
        var vvv = ooo ?? ""aaa"";
    }
}
";

            var expected = @"
using System;

public struct StringValues
{
    public static implicit operator StringValues(string value) { return null; }

    public static bool operator ==(StringValues left, string right) => false;
    public static bool operator !=(StringValues left, string right) => false;

    public static bool operator ==(StringValues left, object right) => false;
    public static bool operator !=(StringValues left, object right) => false;

    public static bool operator ==(string left, StringValues right) => false;
    public static bool operator !=(string left, StringValues right) => false;

    public static bool operator ==(object left, StringValues right) => false;
    public static bool operator !=(object left, StringValues right) => false;

    public static bool operator ==(StringValues left, StringValues right) => false;
    public static bool operator !=(StringValues left, StringValues right) => false;

}

public class Ccc
{
    public void fff()
    {
        StringValues? ooo = null;
        var vvv =  ooo != (StringValues?)null ? ooo.Value : (StringValues)""aaa"";
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestUncoalesceCoalescedNull0()
        {
            var original = @"
namespace Ns {
class NullCoalesce
{
    static void Main()
    {
        int? x = null;
        int y = x ?? -1;
    }
}
}
";

            var expected = @"
namespace Ns {
class NullCoalesce
{
    static void Main()
    {
        int? x = null;
        int y = x!=null ? x.Value : -1;
    }
}
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestUncoalesceCoalescedNull1()
        {
            var original = @"
namespace Ns {
class NullCoalesce
{
    static void Main()
    {
        int? x = null;
        int? y = x ?? -1;
    }
}
}
";

            var expected = @"
namespace Ns {
class NullCoalesce
{
    static void Main()
    {
        int? x = null;
        int? y = x!=null ? x : -1;
    }
}
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestUncoalesceCoalescedNull2()
        {
            var original = @"
namespace Ns {
class NullCoalesce
{
    static int? GetNullableInt() { return null; }

    static void Main()
    {
        int i = GetNullableInt() ?? default(int);
    }
}
}
";

            var expected = @"
namespace Ns {
class NullCoalesce
{
    static int? GetNullableInt() { return null; }

    static void Main()
    {
        int i = GetNullableInt() != null ? GetNullableInt().Value : default(int);
    }
}
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestUncoalesceCoalescedNull3()
        {
            var original = @"
namespace Ns {
class NullCoalesce
{
    static string GetStringValue() {  return null; }

    static void Main()
    {
        string s = GetStringValue();
        var w = s ?? ""Unspecified"";
    }
}
}
";

            var expected = @"
namespace Ns {
class NullCoalesce
{
    static string GetStringValue() { return null; }

    static void Main()
    {
        string s = GetStringValue();
        var w = s != (string)null ? s : ""Unspecified"";
    }
}
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestUncoalesceCoalescedNull4()
        {
            var original = @"
using System;

public class Foo
{
    public decimal? Dvd { get; set; }
    public int? Aired { get; set; }

    public void fff()
    {
        object ooo = Dvd ?? Aired;
    }
}
 ";

            var expected = @"
using System;

public class Foo
{
    public decimal? Dvd { get; set; }
    public int? Aired { get; set; }

    public void fff()
    {
        object ooo = Dvd != null? Dvd:Aired;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestUncoalesceCoalescedNull5()
        {
            var original = @"
using System;

public class Foo
{
    public decimal? Dvd { get; set; }
    public int? Aired { get; set; }
    public void fff()
    {
        object ooo = null;
        bar(ooo == null ?  null : Dvd ?? Aired);
    }
    public void bar(object xx) {}
}
 ";

            var expected = @"
using System;

public class Foo
{
    public decimal? Dvd { get; set; }
    public int? Aired { get; set; }
    public void fff()
    {
        object ooo = null;
        bar(ooo == null ?  null : Dvd != null? Dvd:Aired);
    }
    public void bar(object xx) {}
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestUncoalesceCoalescedNull6()
        {
            var original = @"
class Abc
{
    void f(Abc abc)
    {
        var ss = """";
        string s = ""asdf "" +  (ss ?? string.Empty)  + ""kkk"";
    }
}
            ";

            var expected = @"
class Abc
{
    void f(Abcabc)
    {
        var ss = """";
        string s=""asdf""+(ss!=(string)null?ss:string.Empty)+""kkk"";
    }
}
            ";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestUncoalesceCoalescedNull7()
        {
            var original = @"
namespace Ns {
class NullCoalesce
{
    static void Main()
    {
        int? x = null;
        int y = x ??
                -1;
    }
}
}
";

            var expected = @"
namespace Ns {
class NullCoalesce
{
    static void Main()
    {
        int? x = null;
        int y = x!=null ? x.Value
             : -1;
    }
}
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestUncoalesceCoalescedNull8()
        {
            var original = @"
namespace Ns {
class NullCoalesce
{
    static void Main()
    {
        int? x = null;
        int y = x
            ?? -1;
    }
}
}
";

            var expected = @"
namespace Ns {
class NullCoalesce
{
    static void Main()
    {
        int? x = null;
        int y = x!=null
            ? x.Value : -1;
    }
}
}
";
            TestRewrite_LinePreserve(original, expected);
        }
    }
}
