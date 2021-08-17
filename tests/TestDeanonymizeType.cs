using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using SharpSyntaxRewriter.Rewriters;

namespace Tests
{
    [TestClass]
    public class TestDeanonymizeType : RewriterTester
    {
        protected override SyntaxTree ApplyRewrite(SyntaxTree tree, Compilation compilation)
        {
            var rw = new DeanonymizeType();
            rw.Reset(); // TODO: Broken API... needs re-design.
            return rw.Apply(tree, compilation.GetSemanticModel(tree));
        }

        [TestMethod]
        public void TestDeanonymizeTypeSingleBuiltinMember()
        {
            var original = @"
class A
{
    void f()
    {
        var y = new { abc = 10 };
    }
}
";

            var expected = @"
class A
{ internal class __AnonymousType1_A { internal int abc; internal __AnonymousType1_A(int abc) { this.abc=abc; } }
    void f()
    {
        var y = new __AnonymousType1_A(10);
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDeanonymizeTypeSingleBuiltinMemberSymmetricBlockTrivia()
        {
            var original = @"
class A
{
    void f()
    {
        var y = new
        {
            abc = 10
        };
    }
}
";

            var expected = @"
class A
{ internal class __AnonymousType1_A { internal int abc; internal __AnonymousType1_A(int abc) { this.abc=abc; } }
    void f()
    {
        var y = new __AnonymousType1_A
        (
            10
        );
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDeanonymizeTypeSingleBuiltinMemberSymmetricBlockManyInitializersTrivia()
        {
            var original = @"
class A
{
    void f()
    {
        var y = new
        {
            abc = 10, xyz = 99
        };
    }
}
";

            var expected = @"
class A
{ internal class __AnonymousType1_A { internal int abc; internal int xyz; internal __AnonymousType1_A(int abc, int xyz) { this.abc=abc; this.xyz=xyz; } }
    void f()
    {
        var y = new __AnonymousType1_A
        (
            10, 99
        );
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDeanonymizeTypeSingleBuiltinMemberSymmetricBlockManyInitializersBreakLineTrivia()
        {
            var original = @"
class A
{
    void f()
    {
        var y = new
        {
            abc = 10,
            xyz = 99,
            mno = 33
        };
    }
}
";

            var expected = @"
class A
{ internal class __AnonymousType1_A { internal int abc; internal int xyz; internal int mno; internal __AnonymousType1_A(int abc, int xyz, int mno) { this.abc=abc; this.xyz=xyz; this.mno = mno;} }
    void f()
    {
        var y = new __AnonymousType1_A
        (
            10,
            99,
            33
        );
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDeanonymizeTypeSingleBuiltinMemberSymmetricBlockManyInitializersTrailingComma()
        {
            var original = @"
class A
{
    void f()
    {
        var y = new
        {
            abc = 10,
            xyz = 99,
            mno = 33,
        };
    }
}
";

            var expected = @"
class A
{ internal class __AnonymousType1_A { internal int abc; internal int xyz; internal int mno; internal __AnonymousType1_A(int abc, int xyz, int mno) { this.abc=abc; this.xyz=xyz; this.mno = mno;} }
    void f()
    {
        var y = new __AnonymousType1_A
        (
            10,
            99,
            33
        );
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDeanonymizeTypeSingleBuiltinMemberSymmetricBlockManyInitializersBreakLineMixTrivia()
        {
            var original = @"
class A
{
    void f()
    {
        var y = new
        {
            abc = 10,
            xyz = 99, mno = 33
            , pqr = 44
        };
    }
}
";

            var expected = @"
class A
{ internal class __AnonymousType1_A { internal int abc; internal int xyz; internal int mno; internal int pqr; internal __AnonymousType1_A(int abc, int xyz, int mno, int pqr) { this.abc=abc; this.xyz=xyz; this.mno = mno; this.pqr = pqr;} }
    void f()
    {
        var y = new __AnonymousType1_A
        (
            10,
            99, 33
            , 44
        );
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDeanonymizeTypeSingleBuiltinMemberSymmetricBlockManyInitializersBreakLineNestedTrivia()
        {
            var original = @"
class A
{
    void f()
    {
        var y = new
        {
            abc = 10,
            z = new {
                 xyz = 4.4 }
        };
    }
}
";

            var expected = @"
class A
{ internal class __AnonymousType1_A{internal double xyz;internal __AnonymousType1_A(double xyz){this.xyz=xyz;}}internal class __AnonymousType2_A{internal int abc;internal __AnonymousType1_A z;internal __AnonymousType2_A(int abc,__AnonymousType1_A z){this.abc=abc;this.z=z;}}
    void f()
    {
        var y = new __AnonymousType2_A
        (
            10,
            new __AnonymousType1_A (
                4.4 )
        );
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDeanonymizeTypeSingleBuiltinMemberAsymmetricBlockTrivia()
        {
            var original = @"
class A
{
    void f()
    {
        var y = new {
            abc = 10
        };
    }
}
";

            var expected = @"
class A
{ internal class __AnonymousType1_A { internal int abc; internal __AnonymousType1_A(int abc) { this.abc=abc; } }
    void f()
    {
        var y = new __AnonymousType1_A (
            10
        );
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDeanonymizeTypeNoMemberDeclarators()
        {
            var original = @"
class A
{
    void f()
    {
        var y = new {};
    }
}
";

            var expected = @"
class A
{ internal class __AnonymousType1_A { internal __AnonymousType1_A(){ } }
    void f()
    {
        var y = new __AnonymousType1_A();
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDeanonymizeTypeNameWithoutInitializer()
        {
            var original = @"
class Test
{
    void f()
    {
        var xyz = 10;
        var a = new { abc = 1, xyz };
    }
}
";

            var expected = @"
class Test
{ internal class __AnonymousType1_Test { internal int abc; internal int xyz; internal __AnonymousType1_Test(int abc, int xyz) { this.abc = abc; this.xyz = xyz;} }
    void f()
    {
        var xyz = 10;
        var a = new __AnonymousType1_Test(1, xyz);
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDeanonymizeTypeNonSimpleNameWithoutInitializer()
        {
            var original = @"
class Test
{
    static int member = 1;
    void f()
    {
        var a = new { Test.member };
    }
}
";

            var expected = @"
class Test
{internal class __AnonymousType1_Test { internal int member; internal __AnonymousType1_Test(int member) { this.member = member; } }
    static int member = 1;
    void f()
    {
        var a = new __AnonymousType1_Test(Test.member);
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDeanonymizeTypeNameReferencedInInitializer()
        {
            var original = @"
public class Test
{
    public void f()
    {
        int bar = 3;
        var i = new { bar, foo = bar + 10 };
    }
}
";
            var expected = @"
public class Test
{ internal class __AnonymousType1_Test { internal int bar; internal int foo; internal __AnonymousType1_Test(int bar, int foo) { this.bar = bar; this.foo = foo; } }
    public void f()
    {
        int bar = 3;
        var i = new __AnonymousType1_Test(bar, bar + 10);
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }
        
        [TestMethod]
        public void TestDeanonymizeTypeInitializerWithCapture()
        {
            // https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-7#out-variables
            var original = @"
public class Album {
    public int id = 0;
}
    
public class C {
    public void f(Album a) {
        var x = new { id = a.id };
    }
}
";
            var expected = @"
public class Album {
    public int id = 0;
}

public class C {     internal class __AnonymousType1_C { internal int id; internal __AnonymousType1_C(int id) { this.id = id; } }
    public void f(Album a) {
        var x = new __AnonymousType1_C(a.id);
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }
        
        [TestMethod]
        public void TestDeanonymizeTypeExpressionInsideLambda()
        {
            var original = @"
using System.Collections.Generic;

public class Test
{
    public class OrderDetail
    {
        public int ProductId;
        public int OrderId;
    }
    
    protected void OnModelCreating(List<OrderDetail> l)
    {
        List<System.Object> test = new List<object>();
        l.ForEach(a => test.Add(new { a.ProductId, a.OrderId }));
    }
}
";
            var expected = @"
using System.Collections.Generic;

public class Test
{ internal class __AnonymousType1_Test { internal int ProductId; internal int OrderId; internal __AnonymousType1_Test(int ProductId, int OrderId) { this.ProductId = ProductId; this.OrderId = OrderId; } }
    public class OrderDetail
    {
        public int ProductId;
        public int OrderId;
    }

    protected void OnModelCreating(List<OrderDetail> l)
    {
        List<System.Object> test = new List<object>();
        l.ForEach(a => test.Add(new __AnonymousType1_Test(a.ProductId, a.OrderId)));
    }
}
";
            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDeanonymizeTypeNested()
        {
            var original = @"
class Test
{
    void f()
    {
        var obj = new { foo = new { bar = 2 } };
    }
}
            ";

            var expected = @"
class Test
{internal class __AnonymousType1_Test { internal int bar; internal __AnonymousType1_Test(int bar){this.bar=bar;} }     internal class __AnonymousType2_Test { internal __AnonymousType1_Test foo; internal __AnonymousType2_Test(__AnonymousType1_Test foo){this.foo=foo;} }
    void f()
    {
        var obj = new__AnonymousType2_Test(new__AnonymousType1_Test(2 ));
    }
}
            ";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDeanonymizeTypeNestedInArray()
        {
            var original = @"
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
            };
    }
}
";

            var expected = @"
class Test
{   internalclass__AnonymousType1_Test{internalintnnn;internal__AnonymousType1_Test(intnnn){this.nnn=nnn;}}internalclass__AnonymousType2_Test{internal__AnonymousType1_Test[]qwe;internal__AnonymousType2_Test(__AnonymousType1_Test[]qwe){this.qwe=qwe;}}
    void f()
    {
        var obj = new__AnonymousType2_Test
            (
                new []
                {
                    new__AnonymousType1_Test ( 333 )
                }
            );
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDeanonymizeTypeNestedAnonymousTypeParameter()
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
{   internalclass__AnonymousType1_Test{internalintnnn;internal__AnonymousType1_Test(intnnn){this.nnn=nnn;}}internalclass__AnonymousType2_Test{internal__AnonymousType1_Testhhh;internal__AnonymousType2_Test(__AnonymousType1_Testhhh){this.hhh=hhh;}}internalclass__AnonymousType3_Test{internalSystem.Collections.Generic.IEnumerable<__AnonymousType2_Test>qwe;internal__AnonymousType3_Test(System.Collections.Generic.IEnumerable<__AnonymousType2_Test>qwe){this.qwe=qwe;}}
    void f()
    {
        var obj = new__AnonymousType3_Test
            (
                new []
                {
new__AnonymousType1_Test ( 333 )
                }
                .Select(aaa => new__AnonymousType2_Test ( aaa) )
            );
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDeanonymizeTypeWithinDependentGenericCollection()
        {
            var original = @"
using System;
using System.Linq;
using System.Collections.Generic;

class App
{
    static void Main()
    {
        var array = new int[10];
        var www = array.Select(c => new { abc = c });
        var xxx = new { oij = www };
    }
}
";

            var expected = @"
using System;
using System.Linq;
using System.Collections.Generic;

class App
{internal class __AnonymousType1_App { internal int abc; internal __AnonymousType1_App(intabc){this.abc=abc;} } internal class __AnonymousType2_App { internal IEnumerable<__AnonymousType1_App>oij; internal __AnonymousType2_App(IEnumerable<__AnonymousType1_App>oij){this.oij=oij;}}
    static void Main()
    {
        var array = new int[10];
        var www = array.Select(c=>new __AnonymousType1_App(c ));
        var xxx = new __AnonymousType2_App(www );
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        // TODO
        /*
        [TestMethod]
        public void TestDeanonymizeTypeInPartialClass()
        {
            var originalA = @"
using System;
using System.Linq;
using System.Collections.Generic;

public partial class Abc
{
    public object iii()
    {
        return new { aaa  = 123 };
    }
}
";

            var originalB = @"
using System;
using System.Linq;
using System.Collections.Generic;

public partial class Abc
{
    public object fff()
    {
        return new { vvv = 123 };
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }
        */

        [TestMethod]
        public void TestDeanonymizeTypeInGenericClass()
        {
            var original = @"
using System;
using System.Linq;
using System.Collections.Generic;

public partial class Abc<T>
{
    public object fff(IEnumerable<T> aaa)
    {
        return new { vvv =  aaa };
    }
}
";

            var expected = @"
using System;
using System.Linq;
using System.Collections.Generic;

public partial class Abc<T>
{ internal class __AnonymousType1_Abc { internal IEnumerable<T> vvv; internal __AnonymousType1_Abc(IEnumerable<T>vvv){this.vvv=vvv;} }
    public object fff(IEnumerable<T> aaa)
    {
        return new __AnonymousType1_Abc(aaa);
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDeanonymizeTypeInGenericFunction()
        {
            var original = @"
using System;
using System.Linq;
using System.Collections.Generic;

public partial class Abc
{
    public object fff<T>(IEnumerable<T> aaa)
    {
        return new { vvv =  aaa };
    }
}
";

            var expected = @"
using System;
using System.Linq;
using System.Collections.Generic;

public partial class Abc
{internal class __AnonymousType1_Abc<T> { internal IEnumerable<T> vvv; internal __AnonymousType1_Abc(IEnumerable<T> vvv){this.vvv=vvv;} }
    public object fff<T>(IEnumerable<T> aaa)
    {
        return new __AnonymousType1_Abc<T>(aaa);
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDeanonymizeTypeInGenericFunctionMultipleTypeArgument()
        {
            var original = @"
using System;
using System.Linq;
using System.Collections.Generic;

public partial class Abc2
{
    public object fff<T, U>(IEnumerable<T> aaa, IEnumerable<U> uuu)
    {
        return new { vvv =  aaa, xxx = uuu };
    }
}
";

            var expected = @"
using System;
using System.Linq;
using System.Collections.Generic;

public partial class Abc2
{
    internal class __AnonymousType1_Abc2<T,U>
    {
        internal IEnumerable<T>vvv;
        internal IEnumerable<U>xxx;
        internal __AnonymousType1_Abc2(IEnumerable<T> vvv,
                                         IEnumerable<U>xxx)
        {this.vvv=vvv;this.xxx=xxx;}
    }

    public object fff<T, U>(IEnumerable<T> aaa, IEnumerable<U> uuu)
    {
        return new __AnonymousType1_Abc2<T,U>(aaa,uuu);
    }
}
";

            TestRewrite_LineIgnore(original, expected);
        }

        [TestMethod]
        public void TestDeanonymizeTypeWithSystemNullableProperty()
        {
            var original = @"
using System;

public class Rect
{
    public int Width { get; set; }
    public int Height { get; set; }
    public void fff()
    {
        Rect c = null;
        var oo = new {
           Width = c.Width > 0 ? (int?)c.Width : null,
           Height = c.Height  > 0 ? (int?)c.Height : null
        };
    }
}
";

            var expected = @"
using System;

public class Rect
{
    internal class __AnonymousType1_Rect
    {
        internal int? Width;
        internal int? Height;
        internal __AnonymousType1_Rect(int?Width,int?Height){this.Width=Width;this.Height=Height;}
    }

    public int Width { get; set; }
    public int Height { get; set; }
    public void fff()
    {
        Rect c = null;
        var oo = new __AnonymousType1_Rect(c.Width > 0 ? (int?)c.Width : null,
                                             c.Height  > 0 ? (int?)c.Height : null);
    }
}
";

            TestRewrite_LineIgnore(original, expected);
        }

        [TestMethod]
        public void TestDeanonymizeTypeWithLessAccessibleProperty()
        {
            var original = @"
class Test
{
    private class Private {}
    protected class Protected {}

    void f()
    {
        Private iii = null;
        Protected ppp = null;

        var obj = new { nnn = iii, mmm = ppp };
    }
}
";

            var expected = @"
class Test
{   private class __AnonymousType1_Test{internalPrivatennn;internalProtectedmmm;internal__AnonymousType1_Test(Privatennn,Protectedmmm){this.nnn=nnn;this.mmm=mmm;}}
    private class Private{}
    protected classProtected{}

    voidf()
    {
        Private iii=null;
        Protected ppp=null;

        var obj = new __AnonymousType1_Test(iii,ppp);
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDeanonymizeTypeWithInstantiatedGenericMembers()
        {
            var original = @"
using System;

public class Rect<TT>
{
    public TT Width { get; set; }
    public TT Height { get; set; }
}

public class Foo
{
    public void fff()
    {
        Rect<int> c = null;
        var oo = new {
           Width = c.Width,
           Height = c.Height
        };
    }
}
";

            var expected = @"
using System;

public class Rect<TT>
{
    public TT Width { get; set; }
    public TT Height { get; set; }
}

public class Foo
{
    internal class __AnonymousType1_Foo
    {
        internal int Width;
        internal int Height;
        internal __AnonymousType1_Foo(intWidth,intHeight){this.Width=Width;this.Height=Height;}
    }

    public void fff()
    {
        Rect<int> c = null;
        var oo = new__AnonymousType1_Foo(c.Width,c.Height);
    }
}
";

            TestRewrite_LineIgnore(original, expected);
        }

        [TestMethod]
        public void TestDeanonymizeTypeWithTypeExternalParameter()
        {
            var original = @"
using System;

public class Rect<TT>
{
    public TT Width { get; set; }
    public TT Height { get; set; }
}

public class Foo
{
    public void fff<YY>()
    {
        Rect<YY> c = null;
        var oo = new {
           Width = c.Width,
           Height = c.Height
        };
    }
}
";

            var expected = @"
using System;

public class Rect<TT>
{
    public TT Width { get; set; }
    public TT Height { get; set; }
}

public class Foo
{
    internal class __AnonymousType1_Foo<YY>
    {
        internal YY Width;
        internal YY Height;
        internal __AnonymousType1_Foo(YYWidth,YYHeight){this.Width=Width;this.Height=Height;}
    }

    public void fff<YY>()
    {
        Rect<YY> c = null;
        var oo = new__AnonymousType1_Foo<YY>(c.Width,c.Height);
    }
}
";

            TestRewrite_LineIgnore(original, expected);
        }

        [TestMethod]
        public void TestDeanonymizeTypeWithForwardedTypeParameter()
        {
            var original = @"
using System;

public class Abc<QQ>
{
    public QQ Whatever { get; set; }
}

public class Rect<TT>
{
    public Abc<TT> Nest { get; set; }
}

public class Foo
{
    public void fff<YY>()
    {
        Rect<YY> c = null;
        var oo = new
            {
                Nest = c.Nest
            };
    }
}
";

            var expected = @"
using System;

public class Abc<QQ>
{
    public QQ Whatever { get; set; }
}

public class Rect<TT>
{
    public Abc<TT> Nest { get; set; }
}

public class Foo
{internal class __AnonymousType1_Foo<YY> { internal Abc<YY> Nest; internal __AnonymousType1_Foo(Abc<YY>Nest){this.Nest=Nest;} }
    public void fff<YY>()
    {
        Rect<YY> c = null;
        var oo = new __AnonymousType1_Foo<YY>
            (
                c.Nest
            );
    }
}
";

            TestRewrite_LinePreserve(original, expected);
        }

        [TestMethod]
        public void TestDeanonymizeTypeWithTypeParameterMultipleFunctions()
        {
            // This is to ensure that we don't incorrectly accumulate each type parameter.

            var original = @"
using System;

public class Abc<QQ>
{
    public QQ Whatever { get; set; }
}

public class Rect<TT>
{
    public Abc<TT> Nest { get; set; }
}

public class Foo
{
    public void fff<YY>()
    {
        Rect<YY> c = null;
        var oo = new
            {
                Nest = c.Nest
            };
    }

    public void ggg<YY>()
    {
        Rect<YY> c = null;
        var oo = new
            {
                Nest = c.Nest
            };
    }
}
";

            var expected = @"
using System;

public class Abc<QQ>
{
    public QQ Whatever { get; set; }
}

public class Rect<TT>
{
    public Abc<TT> Nest { get; set; }
}

public class Foo
{
    internal class __AnonymousType1_Foo<YY>
    {
        internal Abc<YY> Nest;
        internal __AnonymousType1_Foo(Abc<YY>Nest){this.Nest=Nest;}
    }
    internal class __AnonymousType2_Foo<YY>
    {
        internal Abc<YY> Nest;
        internal __AnonymousType2_Foo(Abc<YY>Nest){this.Nest=Nest;}
    }

    public void fff<YY>()
    {
        Rect<YY> c = null;
        var oo = new __AnonymousType1_Foo<YY>(c.Nest);
    }
    public void ggg<YY>()
    {
        Rect<YY> c=null;
        var oo =new __AnonymousType2_Foo<YY>(c.Nest);
    }
}
";

            TestRewrite_LineIgnore(original, expected);
        }

        [TestMethod]
        public void TestDeanonymizeTypeWithForwardedTypeParameterNestedCreation()
        {
            var original = @"
using System;

public class Abc<QQ>
{
    public QQ Whatever { get; set; }
}

public class Rect<TT>
{
    public Abc<TT> Nest { get; set; }
}

public class Foo
{
    public void fff<YY>()
    {
        Rect<YY> c = null;
        var oo = new
            {
                Nest = new { c.Nest }
            };
    }
}
";

            var expected = @"
using System;

public class Abc<QQ>
{
    public QQ Whatever { get; set; }
}

public class Rect<TT>
{
    public Abc<TT> Nest { get; set; }
}

public class Foo
{
    internal class __AnonymousType1_Foo<YY>
    {
        internal Abc<YY> Nest;
        internal __AnonymousType1_Foo(Abc<YY>Nest){this.Nest=Nest;}
    }
    internal class __AnonymousType2_Foo<YY>
    {
        internal __AnonymousType1_Foo<YY> Nest;
        internal __AnonymousType2_Foo(__AnonymousType1_Foo<YY>Nest){this.Nest=Nest;}
    }

    public void fff<YY>()
    {
        Rect<YY> c = null;
        var oo=new __AnonymousType2_Foo<YY>(new__AnonymousType1_Foo<YY>(c.Nest));
    }
}
";

            TestRewrite_LineIgnore(original, expected);
        }

        [TestMethod]
        public void TestDeanonymizeTypeWithForwardedTypeParameterNestedCreationMultipleMembers()
        {
            var original = @"
using System;

public class Abc<QQ>
{
    public QQ Whatever { get; set; }
}

public class Rect<TT>
{
    public Abc<TT> Nest { get; set; }
}

public class Foo
{
    public void fff<YY>()
    {
        Rect<YY> c = null;
        var oo = new
            {
                Nest = new { c.Nest },
                NestAgain = new { c.Nest }
            };
    }
}
";

            var expected = @"
using System;

public class Abc<QQ>
{
    public QQ Whatever { get; set; }
}

public class Rect<TT>
{
    public Abc<TT> Nest { get; set; }
}

public class Foo
{
    internal class __AnonymousType1_Foo<YY>
    {
        internal Abc<YY> Nest;
        internal __AnonymousType1_Foo(Abc<YY>Nest){this.Nest=Nest;}
    }
    internal class __AnonymousType2_Foo<YY>
    {
        internal __AnonymousType1_Foo<YY>Nest;
        internal __AnonymousType1_Foo<YY>NestAgain;
        internal __AnonymousType2_Foo(__AnonymousType1_Foo<YY>Nest,__AnonymousType1_Foo<YY>NestAgain){this.Nest=Nest;this.NestAgain=NestAgain;}
    }

    public void fff<YY>()
    {
        Rect<YY> c = null;
        var oo = new __AnonymousType2_Foo<YY>(new__AnonymousType1_Foo<YY>(c.Nest),new __AnonymousType1_Foo<YY>(c.Nest));
    }
}
";

            TestRewrite_LineIgnore(original, expected);
        }
    }
}
