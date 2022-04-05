using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using SharpSyntaxRewriter.Rewriters;

namespace Tests
{
    [TestClass]
    public class TestImplementAutoProperty : RewriterTester
    {
        protected override SyntaxTree ApplyRewrite(SyntaxTree tree, Compilation compilation)
        {
            ImplementAutoProperty rw = new();
            return rw.Apply(tree, compilation.GetSemanticModel(tree));
        }

        [TestMethod]
        public void TestImplementAutoPropertyAccessorAttribute()
        {
            var original = @"
using System;

public class MyAttribute : Attribute {}
public class OtherAttribute : Attribute {}

class Aaa
{
    [MyAttribute]
    public int Vvv
    {
        [OtherAttribute]
        get;

        set;
    }
}
";

            var expected = @"
using System;

public class MyAttribute : Attribute {}
public class OtherAttribute : Attribute {}

class Aaa
{
    [MyAttribute]
    public int Vvv
    {
        [OtherAttribute]
        get { return ____LT____Vvv____GT____k_BackingField; }

        set { ____LT____Vvv____GT____k_BackingField = value; }
    } public int ____LT____Vvv____GT____k_BackingField;
}
";
            TestRewrite_LinePreserve(original, expected);
        }


        [TestMethod]
        public void TestImplementAutoPropertyNonStatic()
        {
            var original = @"
class Test
{
    int Price { get; set; }
}
";

            var expected = @"
class Test
{
    int Price { get { return ____LT____Price____GT____k_BackingField; } set { ____LT____Price____GT____k_BackingField = value; } } int ____LT____Price____GT____k_BackingField;
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImplementAutoPropertyStaticProperty()
        {
            var original = @"
class Test
{
    static int Price { get; set; }
}
";

            var expected = @"
class Test
{
    static int Price { get { return ____LT____Price____GT____k_BackingField; } set { ____LT____Price____GT____k_BackingField = value; } } static int ____LT____Price____GT____k_BackingField;
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImplementAutoPropertyWithInitializer()
        {
            var original = @"
class Test
{
    int Price { get; set; } = 1;
}
";

            var expected = @"
class Test
{
    int Price { get { return ____LT____Price____GT____k_BackingField; } set { ____LT____Price____GT____k_BackingField = value; } } int ____LT____Price____GT____k_BackingField = 1;
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImplementAutoPropertyDuplicateNames()
        {
            var original = @"
class Test
{
    int Price { get; set; } = 1;
}
class Test2
{
    int Price { get; set; } = 1;
}
";

            var expected = @"
class Test
{
    int Price
    {
        get { return ____LT____Price____GT____k_BackingField; }
        set { ____LT____Price____GT____k_BackingField = value; }
    }
    int ____LT____Price____GT____k_BackingField = 1;
}
class Test2
{
    int Price
    {
        get { return ____LT____Price____GT____k_BackingField; }
        set { ____LT____Price____GT____k_BackingField = value; }
    }
    int ____LT____Price____GT____k_BackingField = 1;
}
";
            TestRewrite_LineIgnore(original, expected);
        }

        [TestMethod]
        public void TestImplementAutoPropertyNoSetButAssignmentInCtor()
        {
            var original = @"
public class Test
{
    public Test()
    {
        v = 42;
    }
    public int v { get; }
}
";

            var expected = @"
public class Test
{
    public Test()
    {
        ____LT____v____GT____k_BackingField = 42;
    }
    public int v { get { return ____LT____v____GT____k_BackingField; } } public int ____LT____v____GT____k_BackingField;
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImplementAutoPropertyVirtualProperty()
        {
            var original = @"
class Test
{
    public virtual int Price { get; set; }
}
";

            var expected = @"
class Test
{
    public virtual int Price { get { return ____LT____Price____GT____k_BackingField; } set { ____LT____Price____GT____k_BackingField = value; } } public int ____LT____Price____GT____k_BackingField;
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImplementAutoPropertyOverrideProperty()
        {
            var original = @"
class Test
{
    public virtual int Price { get; set; }
}
class D : Test
{
    public override int Price { get; set; }
}
";

            var expected = @"
class Test
{
    public virtual int Price
    {
        get { return ____LT____Price____GT____k_BackingField; }
        set { ____LT____Price____GT____k_BackingField = value; }
    }
    public int ____LT____Price____GT____k_BackingField;
}
class D : Test
{
    public override int Price
    {
        get { return ____LT____Price____GT____k_BackingField; }
        set { ____LT____Price____GT____k_BackingField = value; }
    }
    public int ____LT____Price____GT____k_BackingField;
}
";
            TestRewrite_LineIgnore(original, expected);
        }

        [TestMethod]
        public void TestImplementAutoPropertyPropertyInNestedClass()
        {
            var original = @"
namespace Ns
{
    class Test
    {
        class Nested
        {
            public int Val { get; set; }
        }
    }
}
";

            var expected = @"
namespace Ns
{
    class Test
    {
        class Nested 
        {
            public int Val { get { return ____LT____Val____GT____k_BackingField;} set { ____LT____Val____GT____k_BackingField = value;} } public int ____LT____Val____GT____k_BackingField;
        }
    }
}
";
            TestRewrite_LineIgnore(original, expected);
        }

        [TestMethod]
        public void TestImplementAutoPropertyInClassWithNestedClass()
        {
            var original = @"
namespace Ns
{
    class Test
    {
        public int Val { get; set; }
        class Nested {}
    }
}
";

            var expected = @"
namespace Ns
{
    class Test
    {
        public int Val {get { return ____LT____Val____GT____k_BackingField; }set { ____LT____Val____GT____k_BackingField = value; }} public int ____LT____Val____GT____k_BackingField;
        class Nested {}
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImplementAutoPropertyInStructWithConstructor()
        {
            var original = @"
public struct S
{
    public S(int iii) { TheIntProp = iii; }
    public int TheIntProp { get; set; }
}
";

            var expected = @"
public struct S
{
    public S(int iii) { ____LT____TheIntProp____GT____k_BackingField = default(int); TheIntProp = iii; }
    public int TheIntProp { get { return____LT____TheIntProp____GT____k_BackingField;} set{____LT____TheIntProp____GT____k_BackingField=value;} } public int ____LT____TheIntProp____GT____k_BackingField;
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImplementAutoPropertyInStructWithConstructor2()
        {
            var original = @"
public struct S
{
    public int TheIntProp { get; set; }
    public S(int iii) { TheIntProp = iii; }
}
";

            var expected = @"
public struct S
{
    public int TheIntProp { get { return____LT____TheIntProp____GT____k_BackingField;} set{____LT____TheIntProp____GT____k_BackingField=value;} }    public int ____LT____TheIntProp____GT____k_BackingField;
    public S(int iii){____LT____TheIntProp____GT____k_BackingField = default(int);TheIntProp = iii;}
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImplementAutoPropertyInRecordStructWithConstructor()
        {
            var original = @"
public record struct S
{
    public S(int iii) { TheIntProp = iii; }
    public int TheIntProp { get; set; }
}
";

            var expected = @"
public record struct S
{
    public S(int iii) { ____LT____TheIntProp____GT____k_BackingField = default(int); TheIntProp = iii; }
    public int TheIntProp { get { return____LT____TheIntProp____GT____k_BackingField;} set{____LT____TheIntProp____GT____k_BackingField=value;} } public int ____LT____TheIntProp____GT____k_BackingField;
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImplementAutoPropertyInStructNoConstructor()
        {
            var original = @"
public struct S
{
    public int TheIntProp { get; set; }
}
";

            var expected = @"
public struct S
{
    public int TheIntProp{get { return____LT____TheIntProp____GT____k_BackingField;}set{____LT____TheIntProp____GT____k_BackingField=value;} } public int ____LT____TheIntProp____GT____k_BackingField;
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImplementAutoPropertyInStructNoConstructorOneField()
        {
            var original = @"
using System;

public struct Msg
{
    private string www;

    public int Vvv { get; private set; }
}
";

            var expected = @"
using System;

public struct Msg
{
    private string www;

    public int Vvv { get { return ____LT____Vvv____GT____k_BackingField; } private set {____LT____Vvv____GT____k_BackingField=value;} } public int ____LT____Vvv____GT____k_BackingField;
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        // TODO: Need "aggregate partial classes" rewriter.

        [Ignore]
        [TestMethod]
        public void TestImplementAutoPropertyInPartialStruct()
        {
            var original = @"
public partial struct S
{
    public int TheIntProp { get; set; }
}
public partial struct S
{
    public S(int iii) { TheIntProp = iii; }
}
";

            var expected = @"
public struct S
{
    public int TheIntProp
    {
        get { return____LT____TheIntProp____GT____k_BackingField; }
        set{ ____LT____TheIntProp____GT____k_BackingField=value;}
    }
    public int ____LT____TheIntProp____GT____k_BackingField;
}
";
            TestRewrite_LineIgnore(original, expected);
        }

        [TestMethod]
        public void TestImplementAutoPropertyAbstractProperty()
        {
            var original = @"
public abstract class Abc
{
    public abstract string BasePath { get; }
}
";

            var expected = @"
public abstract class Abc
{
    public abstract string BasePath { get; }
}
";

            TestRewrite_LineIgnore(original, expected);
        }

        [TestMethod]
        public void TestImplementAutoPropertyNoSetWithConstructorInitialization()
        {
            var original = @"
using System.IO;
using System.Text;

public class EncodingStringWriter : StringWriter
{
    public EncodingStringWriter(Encoding encoding) { Encoding = encoding; }

    public override Encoding Encoding { get; }
}
";

            var expected = @"
using System.IO;
using System.Text;

public class EncodingStringWriter : StringWriter
{
    public EncodingStringWriter(Encoding encoding) { ____LT____Encoding____GT____k_BackingField= encoding; }

    public override Encoding Encoding {get{return ____LT____Encoding____GT____k_BackingField;}}public Encoding ____LT____Encoding____GT____k_BackingField;
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImplementAutoPropertyNoSetWithReferenceInConstructor()
        {
            var original = @"
public class Abc
{
    public int ggg => 222;
}

public class Xxx
{
    public Abc aaa { get; }

    public Xxx()
    {
        int mmm = aaa.ggg;
    }
}
";

            var expected = @"
public class Abc
{
    public int ggg => 222;
}

public class Xxx
{
    public Abc aaa {get{return ____LT____aaa____GT____k_BackingField;}}     public Abc ____LT____aaa____GT____k_BackingField;

    public Xxx()
    {
        int mmm = aaa.ggg;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImplementAutoPropertyNoSetWithNonTrivialConstructorInitialization()
        {
            var original = @"
public class Iii
{
    public int vvv { get; }

    public Iii()
    {
        vvv = 44;
        vvv /= 999;
    }
}
";

            var expected = @"
public class Iii
{
    public int vvv { get { return ____LT____vvv____GT____k_BackingField;} } public int____LT____vvv____GT____k_BackingField;

    public Iii()
    {
        ____LT____vvv____GT____k_BackingField = 44;
        ____LT____vvv____GT____k_BackingField /= 999;
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImplementAutoPropertyPreserveAccessorModifier()
        {
            var original = @"
public class Abc
{
    public int Vvv { get; protected set; }
}
";

            var expected = @"
public class Abc
{
    public int Vvv
    {
        get { return ____LT____Vvv____GT____k_BackingField;}
        protected set{____LT____Vvv____GT____k_BackingField=value;}
    }
    public int ____LT____Vvv____GT____k_BackingField;
}
";

            TestRewrite_LineIgnore(original, expected);
        }

        [TestMethod]
        public void TestImplementAutoPropertyPreserveOverridenAccessorModifier()
        {
            var original = @"
using System;

public abstract class Base
{
    public abstract int Vvv { get; protected set; }
}

public class Derived : Base
{
    public override int Vvv { get; protected set; }
}
";

            var expected = @"
using System;

public abstract class Base
{
    public abstract int Vvv { get; protected set; }
}

public class Derived : Base
{
    public override int Vvv
    {
        get { return ____LT____Vvv____GT____k_BackingField;}
        protected set{____LT____Vvv____GT____k_BackingField=value;}
    }
    public int ____LT____Vvv____GT____k_BackingField;
}
";

            TestRewrite_LineIgnore(original, expected);
        }

        [TestMethod]
        public void TestImplementAutoPropertyInStructWithMultipleConstructors()
        {
            var original = @"
using System;

public struct CHEdgeData
{
    public CHEdgeData(uint tagsId, bool tagsForward)
            : this()
    {}

    public CHEdgeData(uint tagsId)
            : this()
    {}

    public float Weight { get; private set; }
}
";

            var expected = @"
using System;

public struct CHEdgeData
{
    public CHEdgeData(uint tagsId,bool tagsForward)
            : this()
    {____LT____Weight____GT____k_BackingField=default(float);}

    public CHEdgeData(uint tagsId)
            : this()
    {____LT____Weight____GT____k_BackingField=default(float);}

    public float Weight{get{return ____LT____Weight____GT____k_BackingField;} private set{____LT____Weight____GT____k_BackingField=value;}} public float ____LT____Weight____GT____k_BackingField;
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImplementAutoPropertyInStructWithMultipleConstructors2()
        {
            var original = @"
using System;

public struct CHEdgeData
{
    public float Weight { get; private set; }

    public CHEdgeData(uint tagsId, bool tagsForward)
            : this()
    {}

    public CHEdgeData(uint tagsId)
            : this()
    {}
}
";

            var expected = @"
using System;

public struct CHEdgeData
{
    public float Weight{get{return ____LT____Weight____GT____k_BackingField;} private set{____LT____Weight____GT____k_BackingField=value;}} public float ____LT____Weight____GT____k_BackingField;

    public CHEdgeData(uint tagsId,bool tagsForward)
            : this()
    {____LT____Weight____GT____k_BackingField=default(float);}

    public CHEdgeData(uint tagsId)
            : this()
    {____LT____Weight____GT____k_BackingField=default(float);}
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImplementAutoPropertyInterfaceOverride()
        {
            var original = @"
public interface IAAA
{
    int Vvv { get; }
}

public abstract class AAA : IAAA
{
    int IAAA.Vvv { get { return Vvv; } }
    public int Vvv { get; set; }
}
";

            var expected = @"
public interface IAAA
{
    int Vvv { get; }
}

public abstract class AAA : IAAA
{
    int IAAA.Vvv { get { return Vvv; } }public int ____LT____Vvv____GT____k_BackingField;
    public int Vvv {get{return ____LT____Vvv____GT____k_BackingField;} set{____LT____Vvv____GT____k_BackingField=value;} }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImplementAutoPropertyInitAccessor()
        {
            var original = @"
public struct Person
{
    public string FirstName { get; init; }
}
";

            var expected = @"
public struct Person
{
    public string FirstName { get { return ____LT____FirstName____GT____k_BackingField; } init { ____LT____FirstName____GT____k_BackingField=value; } } public string ____LT____FirstName____GT____k_BackingField;
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImplementAutoPropertyUseBetweenTrivial()
        {
            var original = @"
using System;

public class SmtpUri {
    public SmtpUri(Uri uri)
    {
        Host = uri.Host;
    }

    public string Host { get; }
}
";

            var expected = @"
using System;

public class SmtpUri {
    public SmtpUri(Uri uri)
    {
        ____LT____Host____GT____k_BackingField = uri.Host;
    }

    public string Host { get { return ____LT____Host____GT____k_BackingField; } } public string____LT____Host____GT____k_BackingField;
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImplementAutoPropertyReadonlyStruct()
        {
            var original = @"
public readonly struct ImageDimensions
{
    public int Height { get; }
}
";

            var expected = @"
public readonly struct ImageDimensions
{
    public int Height { get { return ____LT____Height____GT____k_BackingField; } } public readonly int ____LT____Height____GT____k_BackingField;
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImplementAutoPropertyReadonlyStructWithReadonlyPropertyGetAccessor()
        {
            var original = @"
public readonly struct ImageDimensions
{
    public readonly int Height { get; }
}
";

            var expected = @"
public readonly struct ImageDimensions
{
    public readonly int Height { get { return ____LT____Height____GT____k_BackingField; } } public readonly int ____LT____Height____GT____k_BackingField;
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestImplementAutoPropertyReadonlyStructWithReadonlyPropertyGetAndInitAccessors()
        {
            var original = @"
public readonly struct ImageDimensions
{
    public readonly int Height { get; init; }
}
";

            var expected = @"
public readonly struct ImageDimensions
{
    public readonly int Height { get { return ____LT____Height____GT____k_BackingField; } init{____LT____Height____GT____k_BackingField=value;} } public readonly int ____LT____Height____GT____k_BackingField;
}
";

            TestRewrite_LinePreserve(original, expected);
        }
    }
}
