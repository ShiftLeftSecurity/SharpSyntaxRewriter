using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using SharpSyntaxRewriter.Rewriters;

namespace Tests
{
    [TestClass]
    public class TestEnsureVisibleConstructor : RewriterTester
    {
        protected override SyntaxTree ApplyRewrite(SyntaxTree tree, Compilation compilation)
        {
            return new EnsureVisibleConstructor().Apply(tree);
        }

        [TestMethod]
        public void TestEnsureVisibleConstructorDeclareDefaultCtorSymmetricBlockTrivia()
        {
            var original = @"
class A
{
}
";

            var expected = @"
class A
{ public A() {} static A() {}
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestEnsureVisibleConstructorDeclareDefaultCtorSymmetricBlockCollapsedTrivia()
        {
            var original = @"
class A
{}
";

            var expected = @"
class A
{ public A() {} static A() {} }
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestEnsureVisibleConstructorSymmetricBlockCollapsedInlineTrivia()
        {
            var original = @"
class A {}
";

            var expected = @"
class A { public A() {} static A() {} }
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestEnsureVisibleConstructorDeclareDefaultCtorAsymmetricBlockTrivia()
        {
            var original = @"
class A {
}
";

            var expected = @"
class A { public A() {} static A() {}
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestEnsureVisibleConstructorDeclareDefaultCtor()
        {
            var original = @"
class A { public A() {} }
class B {}
class C { public C(int i) {} }
";

            var expected = @"
class A { static A() {} public A() {} }
class B { public B() {} static B() {} }
class C { static C() {} public C(int i) {}}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestEnsureVisibleConstructorDeclareDefaultCtorNonCtorMemberExists()
        {
            var original = @"
class A
{
    private int a;
}
";

            var expected = @"
class A
{ public A() {} static A() {}
    private int a;
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestEnsureVisibleConstructorDeclareDefaultCtorNestedClass()
        {
            var original = @"
class A { class B {} }
";

            var expected = @"
class A { public A() {} static A() {} class B { public B() {} static B() {} } }
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestEnsureVisibleConstructorDeclareDefaultCtorInheritance()
        {
            var original = @"
class A {}
class B : A {}
";

            var expected = @"
class A { public A() {} static A() {} }
class B : A { public B() {} static B() {} }
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestEnsureconstructorExistsDontDeclareCtorStruct()
        {
            var original = @"
struct A {}
";

            var expected = @"
struct A {}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestEnsureVisibleConstructorStaticClass()
        {
            var original = @"
static class A {}
";

            var expected = @"
static class A { static A() {} }
";
            TestRewrite_LinePreserve(original, expected);
        }
        
        [TestMethod]
        public void TestEnsureVisibleConstructorPartialClassNoDefaultCtor()
        {
            // We don't (currently) add constructors in partial classes.
            // See explanation in the rewriter.

            var original = @"
partial class MyClass
{
}

partial class MyClass
{
}
";

            var expected = @"
partial class MyClass
{
}

partial class MyClass
{
}
";
            TestRewrite_LinePreserve(original, expected);
        }
        
        [TestMethod]
        public void TestEnsureVisibleConstructorPartialClassOneDefaultCtor1()
        {
            // We don't (currently) add constructors in partial classes.
            // See explanation in the rewriter.

            var original = @"
partial class MyClass
{
    MyClass() {}
}

partial class MyClass
{
}
";

            var expected = @"
partial class MyClass
{
    MyClass() {}
}

partial class MyClass
{
}
";
            TestRewrite_LinePreserve(original, expected);
        }
        
        [TestMethod]
        public void TestEnsureVisibleConstructorPartialClassOneDefaultCtor2()
        {
            // We don't (currently) add constructors in partial classes.
            // See explanation in the rewriter.

            var original = @"
partial class MyClass
{
}

partial class MyClass
{
    MyClass(){}
}
";

            var expected = @"
partial class MyClass
{
}

partial class MyClass
{
    MyClass(){}
}
";

            TestRewrite_LinePreserve(original, expected);
        }
        
        [TestMethod]
        public void TestEnsureVisibleConstructorPartialClassOneDefaultCtor3()
        {
            // We don't (currently) add constructors in partial classes.
            // See explanation in the rewriter.

            var original = @"
partial class MyClass
{
    MyClass(int i, double k){}
}

partial class MyClass
{
    MyClass(int i){}
}

partial class MyClass
{
    MyClass(){}
}
";

            var expected = @"
partial class MyClass
{
    MyClass(int i, double k){}
}

partial class MyClass
{
    MyClass(int i){}
}

partial class MyClass
{
    MyClass(){}
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestEnsureVisibleConstructorNestedClassStaticNonStaticCtor()
        {
            var original = @"
public class Abc
{
    public class Xxx
    {
        static Xxx() {}
        internal Xxx() {}
    }
}
            ";

            var expected = @"
public class Abc
{ public Abc() {} static Abc() {}
    public class Xxx
    {
        static Xxx() {}
        internal Xxx() {}
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }
    }
}